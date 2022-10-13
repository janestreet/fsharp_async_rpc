module Async_rpc.Test.Rpc_test

open Core_kernel
open Async_rpc
open Async_rpc.Protocol

open NUnit.Framework
open System.Threading

let parse_query_message reader bin_read_payload bin_size_payload =
  let query = ref None
  let payload = ref None

  Transport.Reader.For_testing.consume_one_transport_message
    reader
    [ (fun buf pos_ref ->
        query
        := Some(Message.bin_reader_nat0_t.read buf pos_ref))
      (fun buf pos_ref -> payload := Some(bin_read_payload buf pos_ref)) ]

  match !query, !payload with
  | Some (Message.t.Query q), Some payload ->
    Assert.AreEqual(q.data, bin_size_payload payload)
    Query.map_data q (fun (_ : int) -> payload)
  | (Some (_ : Bin_prot.Nat0.t Message.t)
    | None),
    (Some (_ : 'a)
    | None) -> failwith "Expected query message and payload"

let parse_string_query reader =
  parse_query_message reader Bin_prot.Read.bin_read_string Bin_prot.Size.bin_size_string

let parse_pipe_query reader =
  parse_query_message
    reader
    (Stream_query.bin_reader_needs_length Bin_prot.Type_class.bin_reader_string)
      .read
    (Stream_query.bin_size_needs_length Bin_prot.Size.bin_size_string)

[<Test>]
let ``Can send RPCs and pass responses to handler`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let rpc =
    Rpc.create
      { Rpc_description.name = "test-rpc"
        Rpc_description.version = 1 }
      Bin_prot.Type_class.bin_string
      Bin_prot.Type_class.bin_string

  let query_data i = sprintf "test-query-%d" i
  let response_data i = sprintf "test-response-%d" i
  let num_dispatches = 10
  let pending_received_responses = new CountdownEvent(num_dispatches)

  let dispatch_and_expect i =
    Rpc.dispatch
      rpc
      connection
      (query_data i)
      (fun result ->
        Assert.AreEqual(Ok(response_data i), result)
        ignore (pending_received_responses.Signal() : bool))
    |> Result.ok_exn

  let dispatch_indices = List.init num_dispatches id

  List.iter dispatch_and_expect dispatch_indices

  let received_queries =
    List.map
      (fun i -> i, parse_string_query (Test_connection.reader test_connection))
      dispatch_indices

  // Send responses in reverse order to test multiplexing
  List.rev received_queries
  |> List.iter
       (fun (i, query) ->
         Test_connection.send_string_response
           test_connection
           { id = query.id
             data = Ok(response_data i) })

  pending_received_responses.Wait()

let bin_error = Bin_prot.Type_class.bin_unit

let pipe_rpc =
  Pipe_rpc.create
    { Rpc_description.name = "test-pipe-rpc"
      Rpc_description.version = 1 }
    Bin_prot.Type_class.bin_string
    Bin_prot.Type_class.bin_string
    bin_error

[<Test>]
let ``Can send pipe RPCs and pass responses to handler`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let query_data query_i = sprintf "query-%d" query_i
  let response_data query_i response_i = sprintf "response-%d-%d" query_i response_i

  let responses_per_query = 10
  let num_dispatches = 10
  let pending_dispatches = new CountdownEvent(num_dispatches)

  let dispatch_and_expect query_i =
    let received_responses_this_query = ref 0

    Pipe_rpc.dispatch_iter
      pipe_rpc
      connection
      (query_data query_i)
      (fun initial -> initial |> Result.ok_exn |> Result.ok_exn)
      (fun result ->
        if !received_responses_this_query = responses_per_query then
          Assert.AreEqual(Pipe_message.Closed_by_remote_side, result)
          ignore (pending_dispatches.Signal() : bool)
        else
          Assert.AreEqual(
            (Pipe_message.Update(response_data query_i !received_responses_this_query)),
            result
          )

          incr received_responses_this_query)
    |> Result.ok_exn

  let dispatch_indices = List.init num_dispatches id

  List.iter dispatch_and_expect dispatch_indices

  let received_queries =
    List.map
      (fun i -> i, parse_pipe_query (Test_connection.reader test_connection))
      dispatch_indices

  let responses =
    List.map
      (fun (query_i, (query : _ Query.t)) ->
        [ Pipe_response.Initial
            { bin_writer_error = bin_error.writer
              query_id = query.id
              response = Ok() } ]
        @ List.init
            responses_per_query
            (fun response_i ->
              Pipe_response.Update
                { query_id = query.id
                  response = Stream_response_data.t.Ok(response_data query_i response_i) })
          @ [ Pipe_response.Update
                { query_id = query.id
                  response = Stream_response_data.t.Eof } ])
      received_queries

  // Transpose the responses so that they're sent interleaved.
  responses
  |> List.transpose
  |> Option.get
  |> List.iter (
    List.iter (fun pipe_response -> Pipe_response.send pipe_response test_connection)
  )

  pending_dispatches.Wait()

let dispatch_and_get_id test_connection connection initial_handler update_handler =
  Pipe_rpc.dispatch_iter pipe_rpc connection "dummy query" initial_handler update_handler
  |> Result.ok_exn

  (parse_pipe_query (Test_connection.reader test_connection))
    .id

[<Test>]
let ``Error in initial pipe response results in an inner error`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let initial_response = Tasks.TaskCompletionSource<_>()

  let query_id =
    dispatch_and_get_id
      test_connection
      connection
      initial_response.SetResult
      Assert.Unreached

  let pipe_response =
    Pipe_response.Initial
      { bin_writer_error = bin_error.writer
        query_id = query_id
        response = Error() }

  Pipe_response.send pipe_response test_connection

  Assert.AreEqual(initial_response.Task.Result, (Ok(Error())))

[<Test>]
let ``Invalid initial pipe response results in an outer error`` () =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let initial_response = Tasks.TaskCompletionSource<_>()

  let query_id =
    dispatch_and_get_id
      test_connection
      connection
      initial_response.SetResult
      Assert.Unreached

  Test_connection.send_string_response
    test_connection
    { id = query_id
      data = Ok("Invalid pipe initial response") }

  match initial_response.Task.Result with
  | Error e ->
    Assert.That(
      e.ToString(),
      Does.Match("Exn .* streaming_rpc initial_response un-bin-io'ing")
    )
  | e -> failwithf "Unexpected initial_response: %A" e

let dispatch_and_send_update response =
  let test_connection, connection = Test_connection.create_with_fixed_time_source ()

  let update_response = Tasks.TaskCompletionSource<_>()

  let query_id =
    dispatch_and_get_id
      test_connection
      connection
      (fun initial -> initial |> Result.ok_exn |> Result.ok_exn)
      update_response.SetResult

  let pipe_response =
    Pipe_response.Initial
      { bin_writer_error = bin_error.writer
        query_id = query_id
        response = Ok() }

  Pipe_response.send pipe_response test_connection

  Test_connection.send_string_response test_connection { id = query_id; data = response }

  update_response.Task.Result

[<Test>]
let ``Error pipe update response results in an update error`` () =
  match dispatch_and_send_update (Error Rpc_error.t.Connection_closed) with
  | Pipe_message.Closed_from_error e ->
    Assert.That(e.ToString(), Does.Match("Connection_closed"))
  | e -> failwithf "Unexpected update_response: %A" e

[<Test>]
let ``Invalid pipe update response results in an update error`` () =
  match dispatch_and_send_update (Ok "Invalid pipe update") with
  | Pipe_message.Closed_from_error e ->
    Assert.That(
      e.ToString(),
      Does.Match("Exn .* streaming_rpc response header un-bin-io'ing")
    )
  | e -> failwithf "Unexpected update_response: %A" e
