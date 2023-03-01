module Async_rpc.Test.Connection_test

open Core_kernel
open Async_rpc
open Async_rpc.Protocol
open Async_rpc.Test
open System.Threading.Tasks

open NUnit.Framework

let spin_until f =
  while (not (f ())) do
    ()

[<Test>]
let ``Connection errors out on invalid handshake response`` () =
  let test_stream, connection_stream =
    Fake_network_stream.make_pair Time_source.Constant.dotnet_epoch

  let test_transport =
    Transport.create_with_default_max_message_size test_stream
    |> Result.ok_exn

  Transport.Writer.send_bin_prot_exn
    test_transport.writer
    Bin_prot.Type_class.bin_writer_string
    "garbage"

  let connection =
    Connection.create_async
      connection_stream
      Time_source.Constant.dotnet_epoch
      Known_protocol.Rpc
      {| max_message_size = Transport.default_max_message_size |}

  Assert.That(
    sprintf
      "%A"
      (match connection.Result with
       | Error e -> e
       | Ok x -> failwithf "got unexpected OK: %A" x),
    Does.Match("Handshake error: .*")
  )

[<Test>]
let ``Connection errors out on handshake timeout`` () =
  let time_source = new Time_source.Controllable.t (System.DateTime.UnixEpoch)

  let (_ : Fake_network_stream.t), connection_stream =
    Fake_network_stream.make_pair time_source

  let connection_task =
    let task = System.Threading.Tasks.TaskCompletionSource<_>()

    Thread.spawn_and_ignore "test connection creation thread" (fun () ->
      Connection.create
        connection_stream
        time_source
        Known_protocol.Rpc
        {| max_message_size = Transport.default_max_message_size |}
        task.SetResult)

    task.Task

  let small_tick = System.TimeSpan.FromSeconds 1.

  while not connection_task.IsCompleted do
    time_source.advance_immediately_by small_tick

  Assert.That(sprintf "%A" connection_task.Result, Does.Match("Read timed out"))

let query : string Query_v1.t =
  { tag = "test-rpc"
    version = 1L
    id = Query_id.create ()
    data = "hello world" }

let query_with_length_only = Query_v1.map_data query Bin_prot.Size.bin_size_string

let expect_query test_connection =
  Transport.Reader.For_testing.consume_one_transport_message
    (Test_connection.reader test_connection)
    [ Bin_prot_reader.expect
        Message.bin_reader_nat0_t
        (Message.t.Query_v1 query_with_length_only)
      Bin_prot_reader.expect Bin_prot.Type_class.bin_reader_string query.data ]

let dispatch_query connection response_handler =
  Connection.dispatch
    connection
    response_handler
    Bin_prot.Type_class.bin_writer_string
    query

let roundtrip_dispatch_and_response test_connection connection =
  let received_response = new System.Threading.ManualResetEvent(false)

  let response_handler
    (_ : Bin_prot.Nat0.t Response.t)
    (_ : Bin_prot.Common.buf)
    (_ : Bin_prot.Common.pos_ref)
    =
    ignore (received_response.Set() : bool)
    Response_handler.Result.Remove(Ok())

  dispatch_query connection (Some response_handler)
  |> Result.ok_exn

  expect_query test_connection

  Test_connection.send_string_response
    test_connection
    { id = query.id; data = Ok "arbitrary" }

  ignore (received_response.WaitOne() : bool)

let make_connections_for_heartbeat_test time_source =
  // The network stream time source is intentionally separate from the connection time source,
  // since the latter is used for read timeouts done in a background thread. For heartbeat
  // tests that are sensitive to sleep / advancement ordering using the same time source would
  // be racy.
  Test_connection.create
    {| for_connection = time_source
       for_fake_network_stream = Time_source.Constant.dotnet_epoch |}

[<Test>]
let ``Connection sends heartbeats after fixed period`` () =
  use time_source = new Time_source.Controllable.t (System.DateTime.UnixEpoch)
  let test_connection, connection = make_connections_for_heartbeat_test time_source

  let epsilon = System.TimeSpan.FromMilliseconds 10.

  let period_minus_epsilon =
    Connection.For_testing.send_heartbeat_every
    - epsilon

  for (_ : int) in 1..10 do
    time_source.advance_after_sleep_by period_minus_epsilon

    // Even though we almost hit the heartbeat period, no heartbeat has been sent yet.
    // Hence we roundtrip an RPC instead. (This also replenishes the heartbeat timeout)
    roundtrip_dispatch_and_response test_connection connection

    time_source.advance_immediately_by epsilon

    Test_connection.expect_message
      test_connection
      Message.bin_reader_nat0_t
      Message.t.Heartbeat

[<Test>]
let ``Connection dies if no data after fixed timeout period`` () =
  let start_time = System.DateTime.UnixEpoch
  use time_source = new Time_source.Controllable.t (start_time)

  let test_connection, connection = make_connections_for_heartbeat_test time_source

  let timeout_has_passed () =
    start_time
    + Connection.For_testing.heartbeat_timeout < (time_source :> Time_source.t).now ()

  time_source.advance_after_sleep_by Connection.For_testing.send_heartbeat_every

  // We don't advance straight to the [heartbeat_timeout] since in practice we would
  // observe more than one heartbeat before this happens.
  while not (timeout_has_passed ()) do
    Test_connection.expect_message
      test_connection
      Message.bin_reader_nat0_t
      Message.t.Heartbeat

    time_source.advance_after_sleep_by Connection.For_testing.send_heartbeat_every

  while (Transport.Open_state.is_open (Connection.open_state connection)) do
    ()

  Assert.MatchesIgnoringLines(
    "Close_started.*No heartbeats received for",
    Connection.open_state connection
  )

[<Test>]
let ``Connection can handshake, send queries, handle hard-coded responses, and close``
  ()
  =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let response_text = "lorem ipsum"

  let response : string Response.t =
    { id = query.id
      data = Ok response_text }

  let response_with_length_only = Response.map_data response Bin_prot.Size.bin_size_string
  let responses_to_send = 10

  let responses_received = ref 0

  let wait_for_responses n = spin_until (fun () -> !responses_received >= n)

  let response_handler received_response buf pos_ref =
    Assert.AreEqual(received_response, response_with_length_only)
    Assert.AreEqual(response_text, Bin_prot.Read.bin_read_string buf pos_ref)

    let received = System.Threading.Interlocked.Increment(responses_received)

    if received < responses_to_send then
      Response_handler.Result.Keep
    else if received = responses_to_send then
      Response_handler.Result.Remove(Ok())
    else
      failwith "Received more responses than number sent"

  Assert.AreEqual([], Connection.For_testing.open_queries connection)

  dispatch_query connection (Some response_handler)
  |> Result.ok_exn

  expect_query test_connection

  Assert.AreEqual([ query.id ], Connection.For_testing.open_queries connection)

  for (_ : int) in 1 .. (responses_to_send - 1) do
    Test_connection.send_string_response test_connection response

  wait_for_responses (responses_to_send - 1)

  Assert.AreEqual([ query.id ], Connection.For_testing.open_queries connection)

  Test_connection.send_string_response test_connection response
  wait_for_responses responses_to_send

  spin_until (fun () -> Connection.For_testing.open_queries connection = [])

  Connection.close connection
  (Connection.close_finished connection).Result

let dispatch_and_get_task connection handler_return =
  let response = TaskCompletionSource<_>()

  let response_handler r (_ : Bin_prot.Common.buf) (_ : Bin_prot.Common.pos_ref) =
    response.SetResult(r)
    handler_return

  dispatch_query connection (Some response_handler)
  |> Result.ok_exn

  response.Task

[<Test>]
let ``Closing the connection closes the stream and closes outstanding queries`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let response = dispatch_and_get_task connection Response_handler.Result.Keep
  Connection.close connection
  Assert.AreEqual(response.Result.data, Error Rpc_error.t.Connection_closed)

  Assert.True(
    (Test_connection.connection_stream test_connection)
      .is_closed ()
  )

  Assert.MatchesIgnoringLines(
    "Error.*Close_started.*By_user",
    dispatch_query connection None
  )

[<Test>]
let ``Closing the stream from the remote side causes connection close (via reader)`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let response = dispatch_and_get_task connection Response_handler.Result.Keep
  expect_query test_connection

  (Test_connection.connection_stream test_connection)
    .Close()

  Assert.AreEqual(response.Result.data, Error Rpc_error.t.Connection_closed)

  Assert.MatchesIgnoringLines(
    "Error.*Close_started.*reader loop finished",
    dispatch_query connection None
  )

[<Test>]
let ``Write errors also cause connection to close`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  (Test_connection.connection_stream test_connection)
    .set_exn_on_write (Some(System.IO.IOException "test error" :> System.Exception))

  let response = dispatch_and_get_task connection Response_handler.Result.Keep
  Assert.AreEqual(response.Result.data, Error Rpc_error.t.Connection_closed)

  Assert.MatchesIgnoringLines(
    "Error.*Close_started.*Writer stopped.*test error",
    dispatch_query connection None
  )
