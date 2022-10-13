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
  'a Protocol.Query.t ->
  unit Or_error.t

val create :
  System.IO.Stream ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  {| max_message_size : int |} ->
  (t Or_error.t -> unit) ->
  unit

// [Concurrency.t] determines how the implementations are exectuted.
// [Concurrency.Parallel] will be executed in parallel on the thread-pool and
// [Concurrency.Sequential] will run synchronously with respect to multiple dispatches
// from a single client.
val create_with_implementations :
  System.IO.Stream ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  {| max_message_size : int |} ->
  (t Or_error.t -> unit) ->
  Implementation.With_connection_state.t list ->
  Concurrency.t ->
  unit

val close : t -> unit
val open_state : t -> Transport.Open_state.t

val close_finished : t -> Task<unit>

module For_testing =
  val create_wait_for_connection :
    System.IO.Stream ->
    Time_source.t ->
    Known_protocol.With_krb_support.t ->
    {| max_message_size : int |} ->
    t Or_error.t

  val open_queries : t -> Protocol.Query.Id.t list
  val send_heartbeat_every : System.TimeSpan
  val heartbeat_timeout : System.TimeSpan
