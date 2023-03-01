module Async_rpc.Test.Fake_network_stream

open Core_kernel

/// This type acts as a drop-in replacement for a [NetworkStream], where writing to one
/// end of a pair makes data available to read from the other end.
[<SealedAttribute>]
type t =
  class
    inherit System.IO.Stream

    member is_closed : unit -> bool
    member set_exn_on_write : System.Exception option -> unit
  end

val make_pair : Time_source.t -> t * t
