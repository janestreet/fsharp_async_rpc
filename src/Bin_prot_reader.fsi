module Async_rpc.Bin_prot_reader

open Bin_prot.Common

type 'a t = 'a Bin_prot.Type_class.reader

val read_and_verify_length :
  'a t ->
  add_len : ('a -> int) option ->
  buf ->
  pos ref ->
  len : int ->
  location : string ->
  Result<'a, Protocol.Rpc_error.t>
