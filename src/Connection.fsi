module Async_rpc.Connection

open Core_kernel
open System.Threading.Tasks

type t

module Concurrency =
  type t =
    | Parallel
    | Sequential

val dispatch :
  t ->
  Protocol.Response_handler.t option ->
  'a Bin_prot.Type_class.writer ->
  'a Protocol.Query_v1.t ->
    unit Or_error.t

val create :
  System.IO.Stream ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  {| max_message_size : int |} ->
  (t Or_error.t -> unit) ->
    unit

val create_async :
  System.IO.Stream ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  {| max_message_size : int |} ->
    Task<t Or_error.t>

// [Concurrency.t] determines how the implementations are exectuted.
// [Concurrency.Parallel] will be executed in parallel on the thread-pool and
// [Concurrency.Sequential] will run synchronously with respect to multiple dispatches
// from a single client.
val create_with_implementations :
  System.IO.Stream ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  {| max_message_size : int
     connection_state : t -> 'connection_state |} ->
  (t Or_error.t -> unit) ->
  'connection_state Implementation.t list ->
  Concurrency.t ->
    unit

val close : t -> unit
val open_state : t -> Transport.Open_state.t

val close_finished : t -> Task<unit>

module For_testing =
  val open_queries : t -> Protocol.Query_id.t list
  val send_heartbeat_every : System.TimeSpan
  val heartbeat_timeout : System.TimeSpan
