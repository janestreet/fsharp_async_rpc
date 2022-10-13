namespace Async_rpc

open Core_kernel
open Async_rpc.Protocol

module Rpc =
  type ('query, 'response) t

  val create :
    Rpc_description.t ->
    bin_query : 'query Bin_prot.Type_class.t ->
    bin_response : 'response Bin_prot.Type_class.t ->
    t<'query, 'response>

  val dispatch :
    t<'query, 'response> ->
    Connection.t ->
    'query ->
    (Result<'response, Rpc_error.t> -> unit) ->
    unit Or_error.t

  val description : t<_, _> -> Rpc_description.t

  val bin_query : t<'query, _> -> 'query Bin_prot.Type_class.t
  val bin_response : t<_, 'response> -> 'response Bin_prot.Type_class.t

module Pipe_message =
  type 'a t =
    | Update of 'a
    | Closed_by_remote_side
    | Closed_from_error of Error.t

module Pipe_rpc =
  type ('query, 'response, 'error) t

  val create :
    Rpc_description.t ->
    bin_query : 'query Bin_prot.Type_class.t ->
    bin_response : 'response Bin_prot.Type_class.t ->
    bin_error : 'error Bin_prot.Type_class.t ->
    t<'query, 'response, 'error>

  val dispatch_iter :
    t<'query, 'response, 'error> ->
    Connection.t ->
    'query ->
    initial_handler : (Result<Result<unit, 'error>, Rpc_error.t> -> unit) ->
    update_handler : ('response Pipe_message.t -> unit) ->
    unit Or_error.t
