module Async_rpc.Test.Fake_network_stream

open Core_kernel
open System.IO

let not_supported () = raise (System.NotSupportedException())

let async_choose f1 f2 =
  let result = System.Threading.Tasks.TaskCompletionSource<_>()

  Thread.spawn_and_ignore
    "async_choose thread 1"
    (fun () -> ignore (f1 () |> result.TrySetResult : bool))

  Thread.spawn_and_ignore
    "async_choose thread 2"
    (fun () -> ignore (f2 () |> result.TrySetResult : bool))

  result.Task.Result

[<SealedAttribute>]
type t
  (
    writer_end : MemoryStream Sequencer.t,
    reader_end : MemoryStream Sequencer.t,
    time_source : Time_source.t
  ) =
  inherit Stream ()

  // We independently track positions along the 2 ends of our network stream because
  // writing / reading will affect the internal seek positions tracked by the
  // [MemoryStream]s.
  let mutable reader_pos = Sequencer.with_ reader_end (fun s -> s.Position)
  let mutable writer_pos = Sequencer.with_ writer_end (fun s -> s.Position)

  let mutable read_timeout_ms = System.Threading.Timeout.Infinite

  let mutable closed = false
  let mutable exception_on_write = None

  let read buffer pos length =
    // This lock is to prevent races between both reader / writer sides simultaneously
    // operating on the same 'direction' of the connection.
    Sequencer.with_
      reader_end
      (fun reader_end ->
        let rec spin_read_until_nonzero () =
          if closed then
            0
          else
            let num_read =
              reader_end.Position <- reader_pos

              let num_read = reader_end.Read(buffer, pos, length)

              reader_pos <- reader_end.Position
              num_read

            if num_read = 0 then
              while not (System.Threading.Monitor.Wait reader_end) do
                ()

              spin_read_until_nonzero ()
            else
              num_read

        spin_read_until_nonzero ())

  member this.is_closed() = closed

  member this.set_exn_on_write exn = exception_on_write <- exn

  override this.Read(buffer, pos, length) =
    let read_timeout_ms = read_timeout_ms

    if read_timeout_ms = System.Threading.Timeout.Infinite then
      read buffer pos length
    else
      let result =
        async_choose
          (fun () -> Ok(read buffer pos length))
          (fun () ->
            System.TimeSpan.FromMilliseconds(float read_timeout_ms)
            |> time_source.sleep_for

            Error(IOException "Read timed out"))

      match result with
      | Ok e -> e
      | Error exn -> raise exn

  override this.Write(buffer, pos, length) =
    Option.iter raise exception_on_write

    Sequencer.with_
      writer_end
      (fun writer_end ->
        writer_end.Position <- writer_pos
        writer_end.Write(buffer, pos, length)
        writer_pos <- writer_end.Position
        System.Threading.Monitor.Pulse writer_end)

  override this.ReadTimeout
    with get () = read_timeout_ms
    and set (value) = read_timeout_ms <- value

  override this.CanRead = true

  override this.CanSeek = false
  override this.CanWrite = true
  override this.CanTimeout = true

  override this.Length = not_supported ()

  override this.Position
    with get () = not_supported ()
    and set (_ : int64) = not_supported ()

  override this.Flush() = ()
  override this.Seek(_ : int64, _ : System.IO.SeekOrigin) = not_supported ()
  override this.SetLength(_ : int64) = not_supported ()

  override this.Close() =
    [ reader_end; writer_end ]
    |> List.iter
         (fun end_ ->
           Sequencer.with_
             end_
             (fun end_ ->
               end_.Close()
               System.Threading.Monitor.Pulse end_))

    closed <- true

let make_pair time_source =
  let host_a_buffer = Sequencer.create (new MemoryStream())
  let host_b_buffer = Sequencer.create (new MemoryStream())

  new t (host_a_buffer, host_b_buffer, time_source),
  new t (host_b_buffer, host_a_buffer, time_source)
