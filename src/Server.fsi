module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot

type t

val port : t -> int

val create :
  IPAddress ->
  Time_source.t ->
  Known_protocol.With_krb_support.t ->
  'connection_state Implementation.t list ->
  Connection.Concurrency.t ->
  {| port : int
     initial_connection_state : Socket -> Connection.t -> 'connection_state |} ->
    t

val stop_accepting_new_connections : t -> unit

module For_testing =
  val create_on_free_port :
    IPAddress ->
    Time_source.t ->
    Known_protocol.With_krb_support.t ->
    'connection_state Implementation.t list ->
    Connection.Concurrency.t ->
    {| initial_connection_state : Socket -> Connection.t -> 'connection_state |} ->
      t
