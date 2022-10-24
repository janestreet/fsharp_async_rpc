module Async_rpc.Versioned_rpc

open Async_rpc
open Async_rpc.Protocol
open Core_kernel
open System.Threading.Tasks

module Menu =
  type t = Rpc_description.t list

  val add :
    'connection_state Implementation.t list -> 'connection_state Implementation.t list

  val request : Connection.t -> Task<Or_error.t<t>>
  val supported_versions : t -> name : string -> Set<int64>

  module For_testing =
    val create : (string * int64) list -> t

module Connection_with_menu =
  type t

  val create : Connection.t -> Task<Or_error.t<t>>
  val menu : t -> Menu.t
  val connection : t -> Connection.t
