module Async_rpc.Implementation
open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common

type 'connection_state t

module Kind =
  module Rpc =
    type t<'connection_state, 'query, 'response> =
      { bin_query : 'query Bin_prot.Type_class.t
        bin_response : 'response Bin_prot.Type_class.t
        impl : 'connection_state -> 'query -> 'response }

  module Streaming_rpc =
    type t<'connection_state, 'query, 'init, 'update> =
      { bin_query : 'query Bin_prot.Type_class.t
        bin_init_writer : 'init Bin_prot.Type_class.writer
        bin_update : 'update Bin_prot.Type_class.t
        impl : 'connection_state
          -> 'query
          -> Async<Result<('init * 'update Pipe.Reader.t), 'init>> }

  type t<'connection_state, 'query, 'response, 'init, 'update> =
    | Rpc of Rpc.t<'connection_state, 'query, 'response>
    | Streaming_rpc of Streaming_rpc.t<'connection_state, 'query, 'init, 'update>

// When adding an [Async_rpc.Implementation] in F# users should note that function may be
// run in the thread pool, depending on the concurrency setting on the server; in this
// case, thread-safety must be considered.
val create :
  Kind.t<'connection_state, 'query, 'response, 'init, 'update> ->
  Rpc_description.t ->
    'connection_state t

// OCaml is able to store ['connection_state Implementation.t]s alongside the
// ['connection_state]s directly inside the [Connection], without [Connection] gaining a
// type variable, because it can pack them into a GADT, applying the state to the
// implementation when unpacking. Because we can't do that in F#, we need to eagerly apply
// the connection state before giving it to [Connection], hence [With_connection_state]
// and [run].
module With_connection_state =
  type t

  val run :
    t ->
    Bin_prot.Nat0.t Query_v1.t ->
    buf ->
    pos_ref ->
    Transport.Writer.t ->
      Result.t<unit Async, Rpc_error.t>

  val rpc_description : t -> Rpc_description.t

val add_connection_state :
  'connection_state t -> 'connection_state -> With_connection_state.t
