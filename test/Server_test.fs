module Async_rpc.Test.Server_test

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Bin_prot
open Core_kernel
open Async_rpc
open Async_rpc.Protocol
open NUnit.Framework

let ip_address = "127.0.0.1"

let string_rpc =
  Rpc.create
    { Rpc_description.name = "string-rpc"
      Rpc_description.version = 1L }
    Type_class.bin_string
    Type_class.bin_string

let int_rpc =
  Rpc.create
    { Rpc_description.name = "int-rpc"
      Rpc_description.version = 2L }
    Type_class.bin_int64
    Type_class.bin_int64

let start_client port connection_callback =
  let client = new TcpClient(ip_address, port)
  let stream = client.GetStream() :> System.IO.Stream
  let time_source = new Time_source.Wall_clock.t ()
  let on_connection = new TaskCompletionSource<_>()

  let connection_callback =
    function
    | Error error -> on_connection.SetResult(Error error)
    | Ok connection ->
      connection_callback connection client
      |> on_connection.SetResult

  Connection.create
    stream
    time_source
    Known_protocol.Rpc
    {| max_message_size = Transport.default_max_message_size |}
    connection_callback

  on_connection.Task

let start_client_and_dispatch_rpc port rpc (queries : 'query list) =
  let on_responses = List.map (fun (_ : 'query) -> new TaskCompletionSource<_>()) queries

  let connection_callback connection (_ : TcpClient) =
    List.zip queries on_responses
    |> List.map
      (fun (query, (on_response : TaskCompletionSource<Result<'response, Rpc_error.t>>)) ->
        Async_rpc.Rpc.dispatch rpc connection query (on_response.SetResult))
    |> Result.all_unit

  let on_dispatch = start_client port connection_callback

  let on_response_tasks =
    List.map
      (fun (on_response : TaskCompletionSource<Result<'response, Rpc_error.t>>) ->
        on_response.Task)
      on_responses

  on_dispatch, on_response_tasks

let setup_server implementations_list initial_connection_state concurrency =
  let time_source = new Time_source.Wall_clock.t ()
  let local_address = IPAddress.Parse ip_address

  Server.For_testing.create_on_free_port
    local_address
    time_source
    Known_protocol.Rpc
    implementations_list
    concurrency
    {| initial_connection_state = initial_connection_state |}

let ignore_connection_state (_ : Socket) (_ : Connection.t) = ()

let wait_for_all_responses_ok_exn on_response_tasks =
  List.map
    (fun (on_response_task : Task<Result<'response, Rpc_error.t>>) ->
      on_response_task.Result)
    on_response_tasks
  |> Result.all
  |> Result.ok_exn

[<Test>]
[<Category("Server")>]
let ``Hanging implementations do not cause the client thread to block`` () =
  let mutable reached_implementation = false
  let mutable hang_implementations = true

  let implementations_list =
    [ Rpc.implement int_rpc (fun () query ->
        reached_implementation <- true

        // the following loop guarantees thread consumption
        while hang_implementations do
          ()

        query) ]

  let server =
    setup_server
      implementations_list
      ignore_connection_state
      Connection.Concurrency.Parallel

  let port = Server.port server
  let large_number_of_clients = 10

  let queries = List.init large_number_of_clients int64

  let on_responses =
    List.map (fun query -> start_client_and_dispatch_rpc port int_rpc [ query ]) queries
    |> List.map (fun (on_dispatch, on_result) ->
      Result.ok_exn on_dispatch.Result
      on_result)
    |> List.concat

  while not reached_implementation do
    ()

  hang_implementations <- false

  let responses = wait_for_all_responses_ok_exn on_responses
  Assert.AreEqual(queries, responses)

[<Test>]
[<Category("Server")>]
let ``Multiple clients making different rpc calls`` () =
  let string_implementation = (fun () query -> "Received " + query)
  let int_implementation = (fun () query -> query + 1L)

  let implementation_list =
    [ Rpc.implement string_rpc string_implementation
      Rpc.implement int_rpc int_implementation ]

  let server =
    setup_server
      implementation_list
      ignore_connection_state
      Connection.Concurrency.Parallel

  let port = Server.port server

  let (on_dispatch_client_1, on_responses_client_1) =
    start_client_and_dispatch_rpc port string_rpc [ "query" ]

  let (on_dispatch_client_2, on_responses_client_2) =
    start_client_and_dispatch_rpc port int_rpc [ 0L; 1L; 2L ]

  Result.ok_exn on_dispatch_client_1.Result
  Result.ok_exn on_dispatch_client_2.Result

  let responses_client_1 = wait_for_all_responses_ok_exn on_responses_client_1
  let responses_client_2 = wait_for_all_responses_ok_exn on_responses_client_2

  Assert.AreEqual(responses_client_1, [ "Received query" ])
  Assert.AreEqual(responses_client_2, [ 1L; 2L; 3L ])

module Connection_state =
  type t = { mutable total : int64 }

[<Test>]
[<Category("Server")>]
let ``Custom connection state`` () =
  let initial_connection_state (_ : Socket) (_ : Connection.t) =
    { Connection_state.total = 0L }

  // Implementations are executed in the thread pool so we cannot assume queries are
  // handled in order.
  let add_last_query (connection_state : Connection_state.t) query =
    lock connection_state (fun () ->
      connection_state.total <- connection_state.total + query
      connection_state.total)

  let implementation_list = [ Rpc.implement int_rpc add_last_query ]

  let queries = List.init 50 int64

  let setup_server_and_run_queries concurrency =
    let server = setup_server implementation_list initial_connection_state concurrency
    let port = Server.port server

    start_client_and_dispatch_rpc port int_rpc queries
    |> snd
    |> wait_for_all_responses_ok_exn

  let (expected_in_order_responses, _ : int64) =
    List.mapFold
      (fun acc query ->
        let total = acc + query
        total, total)
      0L
      queries

  let responses_parallel = setup_server_and_run_queries Connection.Concurrency.Parallel

  // It is basically impossible for the exact permutations to match but we expect that all
  // of them ran so the totals (max) must match.
  Assert.AreNotEqual(expected_in_order_responses, responses_parallel)
  Assert.AreEqual(List.max expected_in_order_responses, List.max responses_parallel)

  let responses_sequential =
    setup_server_and_run_queries Connection.Concurrency.Sequential

  Assert.AreEqual(expected_in_order_responses, responses_sequential)

let assert_connection_refused port =
  let exn = Assert.Catch(fun () -> new TcpClient(ip_address, port) |> ignore)
  Assert.IsInstanceOf<SocketException>(exn)
  Assert.That(exn.Message.Contains("Connection refused"))

[<Test>]
[<Category("Server")>]
let ``stop_accepting_new_connections stops accepting connections without closing existing``
  ()
  =
  let string_implementation = (fun () query -> "Received " + query)

  let implementation_list = [ Rpc.implement string_rpc string_implementation ]

  let server =
    setup_server
      implementation_list
      ignore_connection_state
      Connection.Concurrency.Parallel

  let port = Server.port server

  let conn, client =
    (start_client port (fun conn client -> Ok(conn, client)))
      .Result
    |> Result.ok_exn

  let first_dispatch =
    (Rpc.dispatch_async string_rpc conn "first")
      .Result
    |> Result.ok_exn

  Assert.AreEqual(first_dispatch, "Received first")

  Server.stop_accepting_new_connections server

  let second_dispatch =
    (Rpc.dispatch_async string_rpc conn "second")
      .Result
    |> Result.ok_exn

  Assert.AreEqual(second_dispatch, "Received second")

  Connection.close conn
  client.Close()

  assert_connection_refused port

[<Test>]
[<Category("Server")>]
let ``close stops accepting connections and closes existing`` () =
  let string_implementation = (fun () query -> "Received " + query)

  let implementation_list = [ Rpc.implement string_rpc string_implementation ]

  let server =
    setup_server
      implementation_list
      ignore_connection_state
      Connection.Concurrency.Parallel

  let port = Server.port server

  let conn_1, client_1 =
    (start_client port (fun conn client -> Ok(conn, client)))
      .Result
    |> Result.ok_exn

  let conn_2, client_2 =
    (start_client port (fun conn client -> Ok(conn, client)))
      .Result
    |> Result.ok_exn

  let close_task = (Server.close server)

  Assert.IsTrue(
    close_task.Wait(TimeSpan.FromSeconds(2.0)),
    "Timeout exceeded while waiting for the server to close."
  )

  let response_from_connection_1_after_closing =
    (Rpc.dispatch_async string_rpc conn_1 "conn_1")
      .Result

  let response_from_connection_2_after_closing =
    (Rpc.dispatch_async string_rpc conn_2 "conn_2")
      .Result

  Assert.That(
    sprintf "%A" response_from_connection_1_after_closing,
    Does.Match("Close_started")
  )

  Assert.That(
    sprintf "%A" response_from_connection_2_after_closing,
    Does.Match("Close_started")
  )

  Assert.False(client_1.Connected)
  Assert.False(client_2.Connected)

  assert_connection_refused port

[<Test>]
[<Category("Server")>]
let ``close stops accepting connections and determines immediately if there were no active connections``
  ()
  =
  let server = setup_server [] ignore_connection_state Connection.Concurrency.Parallel

  let close_task = (Server.close server)

  Assert.IsTrue(
    close_task.Wait(TimeSpan.FromSeconds(2.0)),
    "Timeout exceeded while waiting for the server to close."
  )

  let port = Server.port server
  assert_connection_refused port

[<Test>]
[<Category("Server")>]
let ``close stops the server when existing connections were already closed`` () =
  let server = setup_server [] ignore_connection_state Connection.Concurrency.Parallel

  let port = Server.port server

  let conn, client =
    (start_client port (fun conn client -> Ok(conn, client)))
      .Result
    |> Result.ok_exn

  Connection.close conn
  client.Close()

  Assert.IsTrue(
    (Connection.close_finished conn)
      .Wait(TimeSpan.FromSeconds(2.0)),
    "Timeout exceeded while waiting for the connection to close."
  )

  let close_task = (Server.close server)

  Assert.IsTrue(
    close_task.Wait(TimeSpan.FromSeconds(2.0)),
    "Timeout exceeded while waiting for the server to close."
  )

  assert_connection_refused port


[<Test>]
[<Category("Server")>]
let ``Client connection is closed when state initialization throws an exception`` () =

  let initial_connection_state (_ : Socket) (_ : Async_rpc.Connection.t) =
    failwith "test-error"

  let server = setup_server [] initial_connection_state Connection.Concurrency.Parallel

  let port = Server.port server
  use client = new TcpClient(ip_address, port)
  let stream = client.GetStream() :> System.IO.Stream
  let time_source = new Time_source.Wall_clock.t ()

  let conn =
    (Connection.create_async
      stream
      time_source
      Known_protocol.Rpc
      {| max_message_size = Transport.default_max_message_size |})
      .Result

  Assert.That(sprintf "%A" conn, Does.Match("End_of_stream"))
  Assert.IsFalse(client.Connected)

  let close_task = (Server.close server)

  Assert.IsTrue(
    close_task.Wait(TimeSpan.FromSeconds(2.0)),
    "Timeout exceeded while waiting for the server to close."
  )

  assert_connection_refused port
