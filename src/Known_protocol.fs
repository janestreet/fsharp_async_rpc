module Async_rpc.Known_protocol

open Core_kernel

type 'a t =
  | Krb of 'a
  | Krb_test_mode
  | Rpc

module With_krb_support =
  type t = (Transport.t -> unit Or_error.t) t

let all = [ Krb(); Krb_test_mode; Rpc ]

let magic_word =
  function
  | Krb _ -> "KRB2"
  | Krb_test_mode -> "KBT"
  | Rpc -> "RPC"

let gen_magic_number word =
  Seq.toList word
  |> List.rev
  |> List.fold (fun acc c -> (acc * 256L) + int64 (c)) 0L

let magic_number t = int64 (gen_magic_number (magic_word t))

let by_magic_number = Map.ofList (List.map (fun p -> magic_number p, p) all)
