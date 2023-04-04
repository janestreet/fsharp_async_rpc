module Async_rpc.Implementation

open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
open Async_rpc
open Transport
open Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src
open System.Collections.Generic

module Kind =
  module Rpc =
    type t<'connection_state, 'query, 'response> =
      { bin_query : 'query Bin_prot.Type_class.t
        bin_response : 'response Bin_prot.Type_class.t
        impl : 'connection_state -> 'query -> 'response }

  module Streaming_rpc =
    type t<'connection_state, 'query, 'init, 'update> =
      { bin_query : 'query Bin_prot.Type_class.t
        bin_init_writer : 'init Bin_prot.Type_class.writer
        bin_update : 'update Bin_prot.Type_class.t
        impl : 'connection_state
          -> 'query
          -> Async<Result<('init * 'update Pipe.Reader.t), 'init>> }

  type t<'connection_state, 'query, 'response, 'init, 'update> =
    | Rpc of Rpc.t<'connection_state, 'query, 'response>
    | Streaming_rpc of Streaming_rpc.t<'connection_state, 'query, 'init, 'update>

let execute_implementation
  (rpc : Kind.Rpc.t<'connection_state, 'query, 'response>)
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
        rpc.bin_query.reader
        None
        read_buffer
        read_buffer_pos_ref
        len
        "server-side rpc query un-bin-io'ing"

    return
      async {
        let response_data =
          Core_kernel.Or_error.try_with (fun () -> rpc.impl connection_state typed_query)
          |> Result.mapError (fun error ->
            Protocol.Rpc_error.t.Uncaught_exn(Sexp.t.Atom(sprintf "%A" error)))

        let response : _ Response.t = { id = query.id; data = response_data }

        let result =
          Transport.Writer.send_bin_prot
            transport_writer
            (Message.bin_writer_needs_length (
              Writer_with_length.of_writer rpc.bin_response.writer
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

module For_streaming_rpcs =
  type t<'connection_state, 'update> =
    { transport_writer : Writer.t
      open_streaming_responses : Dictionary.t<Query_id.t, 'update Pipe.Reader.t>
      connection_state : 'connection_state }

  let send_write_error t id sexp =
    let data =
      Message.t.Response
        { id = id
          data = Error(Rpc_error.t.Write_error sexp) }

    match Writer.send_bin_prot t.transport_writer Message.bin_writer_nat0_t data with
    | Send_result.Sent ()
    | Send_result.Close_started (_ : Close_reason.t) -> ()
    | Send_result.Message_too_big (_ : Send_result.message_too_big)
    | Send_result.Other_error (_ : Error.t) as reason ->
      failwith
        $"Failed to send write error to client. (sexp %A{sexp}) (reason %A{reason})"

  let handle_send_result t id (result : unit Send_result.t) =
    match result with
    | Send_result.Sent ()
    | Send_result.Close_started (_ : Close_reason.t) -> ()
    | Send_result.Message_too_big (_ : Send_result.message_too_big)
    | Send_result.Other_error (_ : Error.t) as reason ->
      send_write_error t id (Sexp.t.Atom $"%A{reason}")

  let write_message t bin_writer x id =
    Writer.send_bin_prot t.transport_writer bin_writer x
    |> handle_send_result t id

  let write_response t id bin_writer_data data =
    let bin_writer =
      Message.bin_writer_needs_length (Writer_with_length.of_writer bin_writer_data)

    write_message t bin_writer (Message.t.Response { id = id; data = data }) id

  module With_header_bin_writer =
    // This module aims to emulate the Ocaml module
    // [async_rpc_kernel/src/implementations.ml:Cached_bin_writer], with one
    // key difference: Here, we create a new buffer for each [t] (i.e. for
    // each query), whereas in Ocaml we have a single global buffer.
    //
    // This is presumably fine in Ocaml b/c of it's single threaded async
    // scheduler; a global buffer doesn't work here in F# since there is no
    // guarantees on when async blocks are ran or how they are interleaved.
    type 'a t =
      { header_prefix : string
        mutable data_len : Bin_prot.Nat0.t
        bin_writer : 'a Bin_prot.Type_class.writer
        buffer : Bin_prot.Buffer.Buffer<byte> }

    type void_ = Void

    let bin_size_void Void = 0
    let bin_write_void (_ : buf) pos Void = pos

    let bin_writer_void : void_ Bin_prot.Type_class.writer =
      { size = bin_size_void
        write = bin_write_void }

    type void_message = void_ Protocol.Message.t

    let bin_writer_void_message : void_message Bin_prot.Type_class.writer =
      Protocol.Message.bin_writer_needs_length bin_writer_void

    let bin_protted buffer (bin_writer : 'a Bin_prot.Type_class.writer) x =
      let len = bin_writer.write buffer 0 x

      // Convert byte array directly into a String
      buffer.Slice(0, len).ToArray()
      |> Array.map char
      |> System.String

    let create id bin_writer =
      let buffer = new Bin_prot.Buffer.Buffer<byte>(32)

      let header_prefix =
        let response : void_message = Message.t.Response { id = id; data = Ok Void }
        bin_protted buffer bin_writer_void_message response

      { header_prefix = header_prefix
        data_len = 0
        bin_writer = bin_writer
        buffer = buffer }

    let stream_response_data_header_len = 4
    let stream_response_data_header_as_int32 : int32 = 0x8a79l

    let bin_write_string_no_length buf pos str =
      // This is [Bin_prot.Write.bin_write_string] except length is not
      // written
      let str_len = String.length str
      assert_pos pos
      let next = pos + str_len
      check_next buf next
      let byte_array = Array.map (fun char -> byte char) (str.ToCharArray())
      buf.CopyMemory(byte_array, pos, str_len)

      next

    let bin_size_nat0_header
      { header_prefix = header_prefix
        data_len = data_len
        bin_writer = _ }
      =
      let stream_response_data_nat0_len =
        stream_response_data_header_len
        + (Bin_prot.Size.bin_size_nat0 data_len)

      let stream_response_data_len = stream_response_data_nat0_len + data_len

      String.length header_prefix
      + (Bin_prot.Size.bin_size_nat0 stream_response_data_len)
      + stream_response_data_nat0_len

    let bin_write_nat0_header
      buf
      pos
      { header_prefix = header_prefix
        data_len = data_len
        bin_writer = _ }
      =
      let pos = bin_write_string_no_length buf pos header_prefix

      let stream_response_data_len =
        stream_response_data_header_len
        + Bin_prot.Size.bin_size_nat0 data_len
        + data_len

      let pos =
        Bin_prot.Write.bin_write_nat0
          buf
          pos
          (Bin_prot.Nat0.of_int stream_response_data_len)

      let next =
        Bin_prot.Write.bin_write_int_32bit buf pos stream_response_data_header_as_int32

      Bin_prot.Write.bin_write_nat0 buf next data_len

    let bin_size_message (t, _) = bin_size_nat0_header t + (t.data_len : int)

    let bin_write_message buf pos (t, data) : int =
      let pos = bin_write_nat0_header buf pos t
      t.bin_writer.write buf pos data

    let bin_writer_message : _ Bin_prot.Type_class.writer =
      { size = bin_size_message
        write = bin_write_message }

    let prep_write (t : 'a t) (data : 'a) : ('b t * 'b) Bin_prot.Type_class.writer =
      t.data_len <- t.bin_writer.size data |> Bin_prot.Nat0.of_int
      bin_writer_message

  let apply_streaming_implementation
    (t : t<'connection_state, 'update>)
    (rpc : Kind.Streaming_rpc.t<'connection_state, 'query, 'init, 'update>)
    len
    read_buffer
    read_buffer_pos_ref
    id
    : Rpc_result.t<unit Async> =
    let query : 'query Rpc_result.t =
      Bin_prot_reader.read_and_verify_length
        rpc.bin_query.reader
        None
        read_buffer
        read_buffer_pos_ref
        len
        "streaming_rpc server-side query un-bin-io'ing"

    let stream_writer : 'update With_header_bin_writer.t =
      With_header_bin_writer.create id rpc.bin_update.writer

    let handle_ok update_pipe =
      Dictionary.set t.open_streaming_responses id update_pipe

      async {
        do!
          Pipe.iter_async update_pipe (fun update ->
            async {
              let bin_writer_message =
                With_header_bin_writer.prep_write stream_writer update

              write_message t bin_writer_message (stream_writer, update) id
            })

        do! Pipe.closed_async update_pipe

        // After the update pipe has been closed and all values within the pipe
        // have been handled, write an Eof.
        write_response
          t
          id
          Stream_response_data.bin_writer_nat0_t
          (Ok Stream_response_data.t.Eof)

        Dictionary.remove t.open_streaming_responses id
        return ()
      }
      // don't wait for updates to be written!
      |> Async.Start

    Result.let_syntax {
      return
        async {
          let! (result : Result<('init * 'update Pipe.Reader.t), 'init> Rpc_result.t) =
            Rpc_result.try_with
              (fun () ->
                async {
                  match query with
                  | Error err -> return (Error err)
                  | Ok query ->
                    let! value = (rpc.impl t.connection_state query)
                    return (Ok value)
                })
              "server-side pipe_rpc computation"

          match result with
          | Error (Rpc_error.t.Uncaught_exn sexp as err) ->
            // An Uncaught_exn indicates we should close the connection
            Dictionary.remove t.open_streaming_responses id
            write_response t id rpc.bin_init_writer (Error err)
            Log.printfn "Uncaught_exn: (query: %A) (exn: %A)" query sexp
            Writer.close t.transport_writer
          | Error err ->
            Dictionary.remove t.open_streaming_responses id
            write_response t id rpc.bin_init_writer (Error err)
          | Ok (Error err) ->
            Dictionary.remove t.open_streaming_responses id
            write_response t id rpc.bin_init_writer (Ok err)
          | Ok (Ok (initial, (rest : 'update Pipe.Reader.t))) ->
            write_response t id rpc.bin_init_writer (Ok initial)
            handle_ok rest

          return ()
        }
    }

  let execute_pipe_implementation
    (t : t<'connection_state, 'update>)
    (rpc : Kind.Streaming_rpc.t<'connection_state, 'query, 'init, 'update>)
    (query : Bin_prot.Nat0.t Query_v1.t)
    read_buffer
    read_buffer_pos_ref
    =
    let len = query.data
    let id = query.id

    let stream_query =
      let add_len query =
        match (query : Bin_prot.Nat0.t Stream_query.t) with
        | Stream_query.t.Abort -> 0
        | Stream_query.t.Query len -> len

      Bin_prot_reader.read_and_verify_length
        Stream_query.bin_reader_nat0_t
        (Some add_len)
        read_buffer
        read_buffer_pos_ref
        len
        "server-side pipe_rpc stream_query un-bin-io'ing"

    (match stream_query with
     | Error (_ : Rpc_error.t) -> Ok(async { () })
     | Ok Stream_query.t.Abort ->
       Dictionary.find t.open_streaming_responses id
       |> Option.iter Pipe.close

       Ok(async { () })
     | Ok (Stream_query.t.Query len) ->
       apply_streaming_implementation t rpc len read_buffer read_buffer_pos_ref id)

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

let create rpc_kind rpc_description : 'connection_state t =
  match rpc_kind with
  | Kind.Rpc rpc ->
    T (fun connection_state ->
      { rpc_description = rpc_description
        run = execute_implementation rpc connection_state })
  | Kind.Streaming_rpc streaming ->
    let run connection_state query buf pos_ref writer =
      let t : For_streaming_rpcs.t<'connection_state, 'update> =
        { transport_writer = writer
          open_streaming_responses = new Dictionary<Query_id.t, 'update Pipe.Reader.t>()
          connection_state = connection_state }

      For_streaming_rpcs.execute_pipe_implementation t streaming query buf pos_ref

    T (fun connection_state ->
      { rpc_description = rpc_description
        run = run connection_state })

let add_connection_state (T t) connection_state = t connection_state
