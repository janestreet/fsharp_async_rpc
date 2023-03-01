module Async_rpc.Transport

open Core_kernel
open System.Collections.Concurrent
open System.Threading.Tasks

module Close_reason =
  type t =
    | By_user
    | Error of Error.t

  let error s = Error(Error.Of.string s)

  let errorf format = Printf.ksprintf error format

module Open_state =
  type t =
    | Open
    | Close_started of Close_reason.t

  let is_open =
    function
    | Open -> true
    | Close_started (_ : Close_reason.t) -> false

module Send_result =
  type message_too_big = { size : int; max_message_size : int }

  type 'a t =
    | Sent of 'a
    | Close_started of Close_reason.t
    | Message_too_big of message_too_big
    // This constructor corresponds to some exceptions in the corresponding OCaml module,
    // as well as synchronous exceptions in F# that were backgrounded in OCaml. This is to
    // simplify the error handling story.
    | Other_error of Error.t

  let to_or_error =
    function
    | Sent v -> Ok v
    | (Close_started (_ : Close_reason.t)
    | Message_too_big (_ : message_too_big)
    | Other_error (_ : Error.t)) as e -> Or_error.Error.format "%A" e

module Handler_result =
  type 'a t =
    | Stop of 'a
    | Continue

module With_limit =
  let bad_message_size_error : Error.t =
    Error.Of.string "Rpc_transport: message too small or too big"

  type 'a t = { t : 'a; max_message_size : int }

  let create t (args : {| max_message_size : int |}) =
    if args.max_message_size < 0 then
      Or_error.Error.format
        "Rpc_transport.With_limit.create got negative max message size: %A"
        args
    else
      Ok
        { t = t
          max_message_size = args.max_message_size }

  let message_size_ok t payload_len =
    payload_len >= 0L
    && payload_len <= int64 (t.max_message_size)

  // Since bin_prot lengths are int64 but ints are in practice never beyond F#'s
  // int limit (int32), this check is a reasonable place to convert to int32.
  let check_message_size t payload_len : int Or_error.t =
    if not (message_size_ok t payload_len) then
      Error bad_message_size_error
    else
      Ok(int payload_len)

module Header =
  let header_length = Bin_prot.Utils.size_header_length

  // While there is a [Bin_prot.Utils.read_size_header], it just directly reads a LE int64
  // from a Buffer. The built-in function does the same without the intermediate Buffer
  // step.
  let read_payload_length (buffer : byte []) = System.BitConverter.ToInt64(buffer, 0)

  let write_payload_length (writer : System.IO.BinaryWriter) payload_len =
    writer.Write(int64 (payload_len))

module Reader =
  type t =
    { stream : System.IO.Stream With_limit.t
      length_buffer : byte [] }

  module Error =
    type t =
      | End_of_stream
      | Other of Error.t
      | Closed

    let to_error =
      function
      | Other error -> error
      | (End_of_stream
      | Closed) as error -> Error.Of.format "%A" error

    let other s = Other(Error.Of.string s)

  let try_read f =
    try
      f ()
    with
    // Exception list: https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.read?view=net-5.0
    // BinaryReader read functions exhibit a subset of these generally.
    | :? System.IO.EndOfStreamException -> Error Error.End_of_stream
    | :? System.IO.IOException as e -> Error(Error.other (e.ToString()))
    | :? System.ObjectDisposedException -> Error Error.Closed
    | (:? System.ArgumentNullException
    | :? System.ArgumentException
    | :? System.ArgumentOutOfRangeException) as e ->
      // The following should never occur unless our code has broken some precondition.
      failwithf "Bug - argument exception in [Reader.read_bytes]: %O" e

  let create stream max_message_size : t Or_error.t =
    Result.let_syntax {
      let! stream = With_limit.create stream max_message_size

      return
        { stream = stream
          length_buffer = Array.zeroCreate Header.header_length }
    }

  // The following code mimics the implementation of [BinaryReader.ReadBytes] except
  // 1. It calls [on_end_of_batch] after every partial read from the stream, like in OCaml.
  //    It doesn't call [on_end_of_batch] on the final read because in OCaml, a later
  //    callback [on_message] can abort the reading loop and cause the call to not happen.
  // 2. If the buffer isn't filled fully, it errors instead of trimming the buffer.
  let read_bytes (stream : System.IO.Stream) buffer len on_end_of_batch =
    let rec fill_buffer_and_get_length read_so_far =
      let read_this_loop = stream.Read(buffer, read_so_far, len - read_so_far)

      if read_this_loop > 0 then
        let read_so_far = read_so_far + read_this_loop

        if read_so_far < len then
          on_end_of_batch ()
          fill_buffer_and_get_length read_so_far
        else
          read_so_far
      else
        read_so_far

    try_read (fun () ->
      if len > 0 && fill_buffer_and_get_length 0 < len then
        Error Error.End_of_stream
      else
        Ok())

  let rec read_forever t on_message on_end_of_batch =
    let result =
      Result.let_syntax {
        let stream = t.stream.t

        let! () =
          read_bytes t.stream.t t.length_buffer Header.header_length on_end_of_batch

        on_end_of_batch ()
        let len = Header.read_payload_length t.length_buffer

        let! verified_len =
          With_limit.check_message_size t.stream len
          |> Result.mapError Error.Other

        let payload = Array.zeroCreate verified_len
        let! () = read_bytes stream payload verified_len on_end_of_batch

        return (on_message payload)
      }

    match result with
    | Error e -> Error e
    | Ok (Handler_result.Stop x) -> Ok x
    | Ok Handler_result.Continue ->
      on_end_of_batch ()
      read_forever t on_message on_end_of_batch


  let read_one_message_bin_prot t (bin_reader : _ Bin_prot.Type_class.reader) =
    read_forever
      t
      (fun buf ->
        let array = new Bin_prot.Buffer.Buffer<byte>(buf)
        let pos_ref = ref 0

        let x = bin_reader.read array pos_ref

        if pos_ref.Value <> buf.Length then
          sprintf
            "message length (%d) did not match expected length (%d)"
            (pos_ref.Value)
            buf.Length
          |> Error.other
          |> Error
          |> Handler_result.Stop
        else
          Handler_result.Stop(Ok x))
      ignore
    |> Result.join

  module For_testing =
    let consume_one_transport_message t consumers =
      read_forever
        t
        (fun buf ->
          let array = new Bin_prot.Buffer.Buffer<byte>(buf)
          let pos_ref = ref 0
          List.iter (fun f -> f array pos_ref) consumers

          if buf.Length <> pos_ref.Value then
            failwithf
              "Buffer (length %d) was not fully consumed (consumed %d)"
              buf.Length
              pos_ref.Value

          Handler_result.Stop())
        ignore
      |> Result.ok_exn

module Writer =
  module Message =
    type t =
      | Close of Close_reason.t
      | Flush of unit TaskCompletionSource
      | Message of byte []

  type t' =
    { to_send : Message.t Blocking_queue.Writer.t With_limit.t
      mutable close_finished_callback : (Close_reason.t -> unit)
      mutable open_state : Open_state.t }

  type t = T of t' Sequencer.t

  let close_and_get_reason (T t) reason =
    Sequencer.with_ t (fun t ->
      match t.open_state with
      | Open_state.Close_started reason -> reason
      | Open_state.Open ->
        t.open_state <- Open_state.Close_started reason

        // In case the close is not from the background thread, we need to signal
        // that thread to stop looping, else it may wait forever for a message.
        Blocking_queue.Writer.write t.to_send.t (Message.Close reason)
        reason)

  let close t =
    ignore (close_and_get_reason t Close_reason.By_user : Close_reason.t)

  let is_close_started (T t) =
    Sequencer.with_ t (fun t -> not (Open_state.is_open t.open_state))

  let try_write writer (buffer : byte []) =
    try
      let payload_length = buffer.Length
      Header.write_payload_length writer payload_length
      writer.Write buffer
      Ok()
    with
    | :? System.ObjectDisposedException ->
      Or_error.Error.format "Underlying stream was closed"
    | :? System.IO.IOException as e ->
      Or_error.Error.format "Got IO exception from writer: %A" e.Message

  let background_writer_loop (T t) queue_reader stream () =
    let writer = new System.IO.BinaryWriter(stream)

    let rec loop () =
      match Blocking_queue.Reader.read queue_reader with
      | Message.Close close_reason -> close_reason
      | Message.Flush signal ->
        signal.SetResult()
        loop ()
      | Message.Message buffer ->
        match try_write writer buffer with
        | Ok () -> loop ()
        | Error e -> close_and_get_reason (T t) (Close_reason.Error e)

    let close_reason = loop ()
    writer.Close()

    // Though this access causes [close_finished_callback] to escape the sequencer, it's
    // not like the callback itself has access to [t]'s internal data structures, so it
    // shouldn't get [t] into a bad state or anything.
    let close_finished_callback = Sequencer.with_ t (fun t -> t.close_finished_callback)
    close_finished_callback close_reason

  let create stream (args : {| max_message_size : int |}) : t Or_error.t =
    Result.let_syntax {
      let queue_reader, queue_writer = Blocking_queue.create ()
      let! to_send = With_limit.create queue_writer args

      let t =
        { to_send = to_send
          open_state = Open_state.Open
          close_finished_callback = (fun (_ : Close_reason.t) -> ()) }
        |> Sequencer.create
        |> T

      Thread.spawn_and_ignore
        "transport background writer"
        (background_writer_loop t queue_reader stream)

      return t
    }

  let send_bin_prot (T t) (bin_writer : 'a Bin_prot.Type_class.writer) (x : 'a) =
    Sequencer.with_ t (fun t ->
      match t.open_state with
      | Open_state.Close_started close_reason -> Send_result.Close_started close_reason
      | Open_state.Open ->
        let payload_len = bin_writer.size x in

        if With_limit.message_size_ok t.to_send (int64 payload_len) then

          let buffer = new Bin_prot.Buffer.Buffer<byte>(payload_len)
          let pos = bin_writer.write buffer 0 x

          if pos <> payload_len then
            Send_result.Other_error(
              Error.Of.format
                "Bug: Length of data written to payload (%d) differs from sizer output (%d)"
                pos
                payload_len
            )
          else
            Blocking_queue.Writer.write t.to_send.t (Message.Message buffer.Buffer)
            Send_result.Sent()
        else
          Send_result.Message_too_big
            { size = payload_len
              max_message_size = t.to_send.max_message_size })

  let send_bin_prot_exn t bin_writer value =
    send_bin_prot t bin_writer value
    |> Send_result.to_or_error
    |> Result.ok_exn


  // This is a separate mutable field because in practice the callback may need to access
  // values only accessible after the writer is created.
  let set_close_finished_callback (T t) callback =
    Sequencer.with_ t (fun t -> t.close_finished_callback <- callback)

  module For_testing =
    let wait_for_flushed (T t) =
      let signal = new TaskCompletionSource<unit>()

      Sequencer.with_ t (fun t ->
        match t.open_state with
        | Open_state.Open ->
          Blocking_queue.Writer.write t.to_send.t (Message.Flush signal)
        | Open_state.Close_started (_ : Close_reason.t) -> signal.SetResult())

      signal.Task.Result

// from [lib/async_rpc/src/rpc_transport.ml]
let default_max_message_size = 100 * 1024 * 1024

type t =
  { writer : Writer.t
    reader : Reader.t }

let create stream (args : {| max_message_size : int |}) =
  Result.let_syntax {
    let! writer = Writer.create stream args
    let! reader = Reader.create stream args

    return { writer = writer; reader = reader }
  }

let create_with_default_max_message_size stream =
  create stream {| max_message_size = default_max_message_size |}

let stream t = t.reader.stream.t

module For_testing =
  let bad_message_size_error = With_limit.bad_message_size_error
