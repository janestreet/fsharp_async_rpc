namespace Async_rpc

open Core_kernel
open Async_rpc.Protocol

module Rpc =
  type ('query, 'response) t =
    { description : Rpc_description.t
      bin_query : 'query Bin_prot.Type_class.t
      bin_response : 'response Bin_prot.Type_class.t }

  let description t = t.description
  let bin_query t = t.bin_query
  let bin_response t = t.bin_response

  let create description bin_query bin_response =
    { description = description
      bin_query = bin_query
      bin_response = bin_response }

  let dispatch t conn query callback =
    let response_handler (response : _ Response.t) read_buffer read_buffer_pos_ref =
      let response =
        Result.let_syntax {
          let! len = response.data

          let data =
            Bin_prot_reader.read_and_verify_length
              t.bin_response.reader
              None
              read_buffer
              read_buffer_pos_ref
              len
              "client-side rpc response un-bin-io'ing"

          return! data
        }

      callback response

      Response_handler.Result.Remove(Ok())

    let query_id = Query.Id.create () in

    let query : _ Query.t =
      { tag = t.description.name
        version = int64 t.description.version
        id = query_id
        data = query } in

    Connection.dispatch conn (Some response_handler) t.bin_query.writer query

module Pipe_message =
  type 'a t =
    | Update of 'a
    | Closed_by_remote_side
    | Closed_from_error of Error.t

module Streaming_rpc =
  type ('query, 'initial_response, 'update_response, 'error_response) t =
    { description : Rpc_description.t
      bin_query : 'query Bin_prot.Type_class.t
      bin_initial_response : 'initial_response Bin_prot.Type_class.t
      bin_update_response : 'update_response Bin_prot.Type_class.t
      bin_error_response : 'error_response Bin_prot.Type_class.t }

  let create
    description
    bin_query
    bin_initial_response
    bin_update_response
    bin_error_response
    =
    { description = description
      bin_query = bin_query
      bin_initial_response = bin_initial_response
      bin_update_response = bin_update_response
      bin_error_response = bin_error_response }

  let abort t conn id =
    let query : _ Query.t =
      { tag = t.description.name
        version = int64 t.description.version
        id = id
        data = Stream_query.t.Abort } in

    Connection.dispatch conn None Stream_query.bin_writer_nat0_t query

  module Response_state =
    module Update_handler =
      type 'a t = 'a Pipe_message.t -> unit

    module Initial =
      type ('query, 'initial, 'update, 'error) rpc = t<'query, 'initial, 'update, 'error>

      type ('query, 'initial, 'update, 'error) t =
        { rpc : rpc<'query, 'initial, 'update, 'error>
          query_id : Query.Id.t
          make_update_handler : 'initial -> 'update Update_handler.t
          initial_result_handler : Result<Result<'initial, 'error>, Rpc_error.t> -> unit
          connection : Connection.t }

    module State =
      type ('query, 'initial, 'update, 'error) t =
        | Waiting_for_initial_response of Initial.t<'query, 'initial, 'update, 'error>
        | Writing_updates of 'update Bin_prot.Type_class.reader * 'update Update_handler.t

    type ('query, 'initial, 'update, 'error) t =
      { mutable state : State.t<'query, 'initial, 'update, 'error> }

  open Response_state

  let read_error (handler : _ Update_handler.t) err =
    handler (Pipe_message.Closed_from_error(Rpc_error.to_error err))
    Response_handler.Result.Remove(Error err)

  let eof (handler : _ Update_handler.t) =
    handler Pipe_message.Closed_by_remote_side
    Response_handler.Result.Remove(Ok())

  let response_handler initial_state : Response_handler.t =
    let state = { state = State.Waiting_for_initial_response initial_state } in

    fun response read_buffer read_buffer_pos_ref ->
      match state.state with
      | State.Writing_updates (bin_reader_update, handler) ->
        match response.data with
        | Error err -> read_error handler err
        | Ok len ->
          let data =
            Bin_prot_reader.read_and_verify_length
              Stream_response_data.bin_reader_nat0_t
              (Some
                (function
                | Stream_response_data.t.Eof -> 0
                | Stream_response_data.t.Ok len -> len))
              read_buffer
              read_buffer_pos_ref
              len
              "client-side streaming_rpc response header un-bin-io'ing"

          (match data with
           | Error err -> read_error handler err
           | Ok Stream_response_data.t.Eof -> eof handler
           | Ok (Stream_response_data.t.Ok len) ->

             let data =
               Bin_prot_reader.read_and_verify_length
                 bin_reader_update
                 None
                 read_buffer
                 read_buffer_pos_ref
                 len
                 "client-side streaming_rpc response payload un-bin-io'ing"

             match data with
             | Error err -> read_error handler err
             | Ok data ->
               handler (Pipe_message.Update data)
               Response_handler.Result.Keep)
      | State.Waiting_for_initial_response initial_handler ->
        (* We never use [Remove (Error _)] here, since that indicates that the
           connection should be closed, and these are "normal" errors. (In contrast, the
           errors we get in the [Writing_updates] case indicate more serious problems.)
           Instead, we just put errors in the initial response handler. *)
        let error err =
          initial_handler.initial_result_handler (Error err)
          Response_handler.Result.Remove(Ok())

        (match response.data with
         | Error err -> error err
         | Ok len ->
           let initial =
             Bin_prot_reader.read_and_verify_length
               (Stream_initial_message.bin_reader_t
                 initial_handler.rpc.bin_initial_response.reader
                 initial_handler.rpc.bin_error_response.reader)
               None
               read_buffer
               read_buffer_pos_ref
               len
               "client-side streaming_rpc initial_response un-bin-io'ing"

           (match initial with
            | Error err -> error err
            | Ok initial_msg ->
              (match initial_msg.initial with
               | Error err ->
                 initial_handler.initial_result_handler (Ok(Error err))
                 Response_handler.Result.Remove(Ok())
               | Ok initial ->
                 let handler = initial_handler.make_update_handler initial in
                 initial_handler.initial_result_handler (Ok(Ok initial))

                 state.state <-
                   State.Writing_updates(
                     initial_handler.rpc.bin_update_response.reader,
                     handler
                   )

                 Response_handler.Result.Keep)))

  let dispatch_gen t conn query initial_result_handler make_update_handler =
    let bin_writer_query =
      Stream_query.bin_writer_needs_length (Writer_with_length.of_type_class t.bin_query)

    let query = Stream_query.t.Query query in
    let query_id = Query.Id.create () in

    let query : _ Query.t =
      { tag = t.description.name
        version = int64 t.description.version
        id = query_id
        data = query }

    let initial_state : Initial.t<_, _, _, _> =
      { rpc = t
        query_id = query_id
        connection = conn
        make_update_handler = make_update_handler
        initial_result_handler = initial_result_handler }

    Connection.dispatch conn (Some(response_handler initial_state)) bin_writer_query query

  let dispatch_iter t conn query f_initial f_updates =
    dispatch_gen t conn query f_initial f_updates

module Pipe_rpc =
  type ('query, 'response, 'error) t =
    | T of Streaming_rpc.t<'query, unit, 'response, 'error>

  let create description bin_query bin_response bin_error =
    Streaming_rpc.create
      description
      bin_query
      Bin_prot.Type_class.bin_unit
      bin_response
      bin_error
    |> T

  let dispatch_iter (T t) conn query initial_handler update_handler =
    Streaming_rpc.dispatch_iter t conn query initial_handler (fun () -> update_handler)
