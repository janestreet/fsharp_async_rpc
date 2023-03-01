module Async_rpc.Implementation

open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
open Async_rpc
open Transport
open Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src

let execute_implementation
  (bin_query : 'query Bin_prot.Type_class.t)
  (f : 'connection_state -> 'query -> 'response)
  (bin_response : 'response Bin_prot.Type_class.t)
  connection_state
  (query : Bin_prot.Nat0.t Query_v1.t)
  read_buffer
  read_buffer_pos_ref
  transport_writer
  =
  // If [Bin_prot_reader.read_and_verify_length] fails then the [Connection] needs to
  // return a [Transport.Handler_result.Stop], which is indicated here by returning an
  // [Error], otherwise we [Continue] (indicated by an [Ok ()]) even if there is an error
  // executing the implementation because that is unrelated to the state of the transport.
  Result.let_syntax {
    let! typed_query =
      let len = query.data

      Bin_prot_reader.read_and_verify_length
        bin_query.reader
        None
        read_buffer
        read_buffer_pos_ref
        len
        "server-side rpc query un-bin-io'ing"

    return
      async {
        let response_data =
          Core_kernel.Or_error.try_with (fun () -> f connection_state typed_query)
          |> Result.mapError (fun error ->
            Protocol.Rpc_error.t.Uncaught_exn(Sexp.t.Atom(sprintf "%A" error)))

        let response : _ Response.t = { id = query.id; data = response_data }

        let result =
          Transport.Writer.send_bin_prot
            transport_writer
            (Message.bin_writer_needs_length (
              Writer_with_length.of_writer bin_response.writer
            ))
            (Message.t.Response response)
          |> Transport.Send_result.to_or_error

        Result.iter_error result (fun error ->
          // A sending error indicates an issue with the connection, in which case the
          // connection should close.
          Log.printfn
            "Write error: (query: %A) (response: %A) (error %A)"
            typed_query
            response_data
            error

          Writer.close transport_writer)
      }
  }

module With_connection_state =
  type t =
    { rpc_description : Rpc_description.t
      run : Bin_prot.Nat0.t Query_v1.t
        -> buf
        -> pos_ref
        -> Transport.Writer.t
        -> Result.t<unit Async, Rpc_error.t> }

  let run
    t
    (query : Bin_prot.Nat0.t Query_v1.t)
    read_buffer
    read_buffer_pos_ref
    transport_writer
    =
    t.run query read_buffer read_buffer_pos_ref transport_writer

  let rpc_description t = t.rpc_description

type 'connection_state t = T of ('connection_state -> With_connection_state.t)

let create query_reader f response_writer rpc_description : 'connection_state t =
  T (fun connection_state ->
    { rpc_description = rpc_description
      run = (execute_implementation query_reader f response_writer connection_state) })

let add_connection_state (T t) connection_state = t connection_state
