module Async_rpc.Test.Memory_stream_with_close_buffer

[<SealedAttribute>]
type t =
  class
    inherit System.IO.MemoryStream
    new : unit -> t
    member BufferAtClose : unit -> byte []
  end
