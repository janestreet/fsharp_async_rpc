module Async_rpc.Test.Pipe_response

open Async_rpc.Protocol
open Async_rpc

let send_pipe_initial_response test_connection query_id response bin_writer_error =
  Test_connection.send_response
    test_connection
    (Stream_initial_message.bin_writer_t
      Bin_prot.Type_class.bin_writer_unit
      bin_writer_error)
    { id = query_id
      data =
        Ok(
          { unused_query_id = -1L
            initial = response }
        ) }

let send_pipe_update test_connection query_id response =
  Test_connection.send_response
    test_connection
    (Stream_response_data.bin_writer_needs_length (
      Writer_with_length.of_writer Bin_prot.Type_class.bin_writer_string
    ))
    { id = query_id; data = Ok response }

type 'a initial =
  { bin_writer_error : 'a Bin_prot.Type_class.writer
    query_id : Query_id.t
    response : Result<unit, 'a> }

type update =
  { query_id : Query_id.t
    response : string Stream_response_data.t }

type 'a t =
  | Initial of 'a initial
  | Update of update

let send t test_connection =
  match t with
  | Initial { bin_writer_error = bin_writer_error
              query_id = query_id
              response = response } ->
    send_pipe_initial_response test_connection query_id response bin_writer_error
  | Update { query_id = query_id
             response = response } -> send_pipe_update test_connection query_id response
