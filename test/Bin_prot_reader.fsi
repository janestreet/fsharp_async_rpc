module Async_rpc.Test.Bin_prot_reader

open Bin_prot
open Bin_prot.Common

type 'a t = 'a Type_class.reader

val expect : t : 'a Type_class.reader -> expected : 'a -> buf -> pos_ref -> unit
