module Async_rpc.Connection

open Core_kernel

type t

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

val close : t -> unit
val open_state : t -> Transport.Open_state.t


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
