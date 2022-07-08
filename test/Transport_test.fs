module Async_rpc.Test.Transport_test

open Core_kernel
open NUnit.Framework
open Async_rpc
open Async_rpc.Transport

let create_stream_with_messages messages =
  let stream = new System.IO.MemoryStream()
  let binWriter = new System.IO.BinaryWriter(stream)

  List.iter
    (fun bytes ->
      binWriter.Write(int64 (Array.length bytes))
      binWriter.Write(bytes : byte []))
    messages

  stream.Position <- int64 (0)
  stream

[<Test>]
let ``Reader can parse manually constructed length-tagged incoming messages`` () =
  let messages =
    [ [| 1; 2 |]; [| 1; 2; 3 |]; [| 1; 2; 3; 4 |] ]
    |> List.map (Array.map byte)

  let stream = create_stream_with_messages messages

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let read_messages = ref []

  let read_result =
    Reader.read_forever
      transport.reader
      (fun message ->
        read_messages := message :: !read_messages
        Handler_result.Continue)
      (fun () -> ())

  Assert.AreEqual(read_result, Error Reader.Error.End_of_stream)

  let read_messages = List.rev !read_messages
  Assert.AreEqual(read_messages, messages)

let string_to_bin_prot_array str =
  let size = Bin_prot.Size.bin_size_string str
  let buffer = new Bin_prot.Buffer.Buffer<byte>(size)
  let pos = Bin_prot.Write.bin_write_string buffer 0 str
  Assert.AreEqual(size, pos)
  buffer.Buffer

let assert_stream_has_exactly stream messages =
  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let read () =
    Reader.read_one_message_bin_prot
      transport.reader
      Bin_prot.Type_class.bin_reader_string

  List.iter (fun message -> Assert.AreEqual(Ok message, read ())) messages

  Assert.AreEqual(Error Reader.Error.End_of_stream, read ())

[<Test>]
let ``Reader can parse [bin_prot]ed messages and use [read_one_message_bin_prot]`` () =
  let messages = [ "hello"; "world"; "test" ]
  let stream = create_stream_with_messages (List.map string_to_bin_prot_array messages)
  assert_stream_has_exactly stream messages

[<Test>]
let ``Reader errors if size too large`` () =
  let stream = new System.IO.MemoryStream()
  let binWriter = new System.IO.BinaryWriter(stream)

  let length_tag = 10
  let max_message_size = 5
  Assert.IsTrue(length_tag >= max_message_size)

  binWriter.Write(int64 (length_tag))
  stream.Position <- int64 (0)

  let transport =
    Transport.create stream {| max_message_size = max_message_size |}
    |> Result.ok_exn

  let result =
    Reader.read_forever
      transport.reader
      (fun (_ : byte []) -> failwith "Should not reach this code")
      (fun () -> ())

  Assert.AreEqual(Error(Reader.Error.Other For_testing.bad_message_size_error), result)

module Sexp = Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T

[<Test>]
let ``The reader can parse bin-io sent from a writer`` () =
  let stream = new System.IO.MemoryStream()

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let sexps =
    [ Sexp.List [ Sexp.Atom "hello"
                  Sexp.Atom "world" ]
      Sexp.Atom "lorem ipsum" ]

  sexps
  |> Seq.iter
       (fun sexp ->
         let result = Writer.send_bin_prot transport.writer Sexp.bin_writer_t sexp
         Assert.AreEqual(result, Send_result.Sent()))

  Writer.For_testing.wait_for_flushed transport.writer

  stream.Position <- int64 (0)

  let sexps_to_read = ref sexps

  let read_result =
    Reader.read_forever
      transport.reader
      (fun message ->
        match !sexps_to_read with
        | [] -> failwith "Should not reach this code path, no sexps read!"
        | expected_sexp :: tl ->
          let array = new Bin_prot.Buffer.Buffer<byte>(message)
          let sexp = Sexp.bin_read_t array (ref 0)
          Assert.AreEqual(sexp, expected_sexp)

          match tl with
          | [] -> Handler_result.Stop()
          | tl ->
            sexps_to_read := tl
            Handler_result.Continue)
      (fun () -> ())

  Assert.AreEqual(read_result, Ok())


[<Test>]
let ``Writer errors if size too large`` () =
  let transport =
    Transport.create (new System.IO.MemoryStream()) {| max_message_size = 0 |}
    |> Result.ok_exn

  match Writer.send_bin_prot transport.writer Sexp.bin_writer_t (Sexp.Atom "Hello") with
  | Send_result.Message_too_big { size = _; max_message_size = _ } -> ()
  | e -> failwithf "Unexpected writer output: %A" e

// Despite what length is requested, this memory stream will return 1 byte
// each read for the purposes of testing [on_end_of_batch] below.
type Slow_memory_stream () =
  inherit System.IO.MemoryStream ()
  override this.Read(buffer, pos, (_length : int)) = base.Read(buffer, pos, 1)

[<Test>]
let ``The reader calls 'on_end_of_batch' after any data from the stream`` () =
  let stream = new Slow_memory_stream()

  let binWriter = new System.IO.BinaryWriter(stream)
  let bytes = [| 1uy; 2uy; 3uy |]
  binWriter.Write(int64 (Array.length bytes))
  binWriter.Write(bytes)
  stream.Position <- int64 (0)

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let test_num_batches handler_result expected_num_batches expected_read_result =
    let batches = ref 0

    let read_result =
      Reader.read_forever
        transport.reader
        (fun (_ : byte []) -> handler_result)
        (fun () -> batches := !batches + 1)

    Assert.AreEqual(expected_read_result, read_result)
    Assert.AreEqual(expected_num_batches, !batches)

  let total_num_bytes =
    Bin_prot.Utils.size_header_length
    + Array.length bytes

  test_num_batches
    Handler_result.Continue
    total_num_bytes
    (Error Reader.Error.End_of_stream)

  stream.Position <- int64 (0)

  // In OCaml, if the [on_message] callback returns [Stop], then the final batch callback
  // is not called. We follow it for consistency, though calling it should also be safe since
  // it only resets the heartbeat timeout.
  test_num_batches (Handler_result.Stop()) (total_num_bytes - 1) (Ok())

let message = "Hello"

[<Test>]
let ``Writes don't work after the writer is closed`` () =
  let stream = new Memory_stream_with_close_buffer.t ()
  let close_finished = new System.Threading.Tasks.TaskCompletionSource<_>()

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let writer = transport.writer

  Writer.set_close_finished_callback writer close_finished.SetResult
  Writer.send_bin_prot_exn writer Bin_prot.Type_class.bin_writer_string message

  Assert.AreEqual(false, Writer.is_close_started writer)
  Writer.close writer
  Assert.AreEqual(true, Writer.is_close_started writer)

  let result = Writer.send_bin_prot writer Bin_prot.Type_class.bin_writer_string message
  Assert.AreEqual(Send_result.Close_started Close_reason.By_user, result)

  Assert.AreEqual(Close_reason.By_user, close_finished.Task.Result)

  // Only writes up to the close are observable
  assert_stream_has_exactly
    (new System.IO.MemoryStream(stream.BufferAtClose()))
    [ message ]

[<Test>]
let ``Writes don't work after the stream is invalidated`` () =
  let stream = new Memory_stream_with_close_buffer.t ()

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  let writer = transport.writer

  Writer.send_bin_prot_exn writer Bin_prot.Type_class.bin_writer_string message
  Writer.For_testing.wait_for_flushed writer

  Assert.AreEqual(false, Writer.is_close_started writer)
  stream.Close()
  Assert.AreEqual(false, Writer.is_close_started writer)

  // The writer doesn't notice until after the next write that the stream is closed.
  Writer.send_bin_prot_exn writer Bin_prot.Type_class.bin_writer_string message

  while not (Writer.is_close_started writer) do
    ()

  let result = Writer.send_bin_prot writer Bin_prot.Type_class.bin_writer_string message

  Assert.That(sprintf "%A" result, Does.Match("stream was closed"))

  assert_stream_has_exactly
    (new System.IO.MemoryStream(stream.BufferAtClose()))
    [ message ]

type Blockable_memory_stream () =
  inherit System.IO.MemoryStream ()

  let writes_go_through_event = new System.Threading.ManualResetEvent(true)

  member this.set_writes_go_through writes_go_through =
    if writes_go_through then
      ignore (writes_go_through_event.Set() : bool)
    else
      ignore (writes_go_through_event.Reset() : bool)

  override this.Write(buffer, offset, count) =
    ignore (writes_go_through_event.WaitOne() : bool)
    base.Write(buffer, offset, count)

[<Test>]
let ``Stream writing can be blocked without blocking [send_bin_prot]`` () =
  let stream = new Blockable_memory_stream()

  let transport =
    Transport.create_with_default_max_message_size stream
    |> Result.ok_exn

  stream.set_writes_go_through false
  Writer.send_bin_prot_exn transport.writer Bin_prot.Type_class.bin_writer_string message
  stream.set_writes_go_through true

  Writer.For_testing.wait_for_flushed transport.writer
  stream.Position <- 0L
  assert_stream_has_exactly stream [ message ]
