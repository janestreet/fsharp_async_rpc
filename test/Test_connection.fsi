module Async_rpc.Test.Test_connection

open Core_kernel
open Async_rpc

type t

/// [create], in addition to creating the test connection, hooks it up to the
/// [Connection.t] and establishes the standard handshake.
val create :
  {| for_connection : Time_source.t
     for_fake_network_stream : Time_source.t |} ->
    t * Connection.t

val create_with_fixed_time_source : unit -> t * Connection.t

val writer : t -> Transport.Writer.t
val reader : t -> Transport.Reader.t
val connection_stream : t -> Fake_network_stream.t

val expect_message : t -> 'a Bin_prot.Type_class.reader -> 'a -> unit
val send_response : t -> 'a Bin_prot.Type_class.writer -> 'a Protocol.Response.t -> unit
val send_string_response : t -> string Protocol.Response.t -> unit
