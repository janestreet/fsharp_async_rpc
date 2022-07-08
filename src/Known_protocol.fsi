module Async_rpc.Known_protocol

open Core_kernel

type 'a t =
  | Krb of 'a
  | Krb_test_mode
  | Rpc

module With_krb_support =
  type t = (Transport.t -> unit Or_error.t) t

val magic_number : 'a t -> int64

val by_magic_number : Map<int64, unit t>
