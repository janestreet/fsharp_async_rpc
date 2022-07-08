module Async_rpc.Protocol_version_header

open Core_kernel

type t

/// [create_exn] raises if [List.length supported_versions >= 100].
val create_exn : 'a Known_protocol.t -> {| supported_versions : int64 list |} -> t

val v1 : 'a Known_protocol.t -> t

val bin_writer_t : t Bin_prot.Type_class.writer

val bin_reader_t : t Bin_prot.Type_class.reader

val handshake_and_negotiate_version : t -> Transport.t -> int64 Or_error.t
