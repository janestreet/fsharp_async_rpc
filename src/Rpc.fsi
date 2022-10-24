namespace Async_rpc

open Core_kernel
open Async_rpc.Protocol
open System.Threading.Tasks

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

  /// Dispatch an rpc query using .NET Task instead of a callback. Any binds will still
  /// end up being ran on the [Async_rpc.Connection] reader thread so it is not too
  /// different from the normal [dispatch]. For convenience we merge the rpc response
  /// [Rpc_error.t] and the dispatch result [Error.t].
  val dispatch_async :
    t<'query, 'response> -> Connection.t -> 'query -> 'response Or_error.t Task

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
