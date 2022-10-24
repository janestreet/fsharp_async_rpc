module Async_rpc.Rpc_description

open Async_rpc.Protocol

type t = { name : Rpc_tag.t; version : int64 }
