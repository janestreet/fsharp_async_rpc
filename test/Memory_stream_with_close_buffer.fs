module Async_rpc.Test.Memory_stream_with_close_buffer

[<SealedAttribute>]
type t () =
  inherit System.IO.MemoryStream ()

  let mutable buffer_at_close = None

  member this.BufferAtClose() =
    match buffer_at_close with
    | Some buffer -> buffer
    | None -> failwithf "Stream is not yet closed"

  override this.Close() =
    buffer_at_close <- Some(base.GetBuffer().[.. (int (base.Position) - 1)])
    base.Close()
