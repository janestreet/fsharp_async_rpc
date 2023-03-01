module Async_rpc.Transport

open Core_kernel

module Close_reason =
  type t =
    | By_user
    | Error of Error.t

  val errorf : Printf.StringFormat<'a, t> -> 'a

module Open_state =
  type t =
    | Open
    | Close_started of Close_reason.t

  val is_open : t -> bool

module Send_result =
  type message_too_big = { size : int; max_message_size : int }

  type 'a t =
    | Sent of 'a
    | Close_started of Close_reason.t
    | Message_too_big of message_too_big
    | Other_error of Error.t

  val to_or_error : 'a t -> 'a Or_error.t

module Handler_result =
  type 'a t =
    | Stop of 'a
    | Continue

/// This reader is not thread-safe, being a wrapper over [Stream.Read], but this is
/// expected in [async_rpc] to be only called from a single thread anyway.
module Reader =
  module Error =
    type t =
      | End_of_stream
      | Other of Error.t
      | Closed

    val to_error : t -> Error.t

  type t

  val read_forever :
    t ->
    on_message : (byte [] -> 'a Handler_result.t) ->
    on_end_of_batch : (unit -> unit) ->
      Result<'a, Error.t>

  val read_one_message_bin_prot :
    t -> 'a Bin_prot.Type_class.reader -> Result<'a, Error.t>

  module For_testing =
    // This function is like [Transport.read_one_message_bin_prot], but
    // takes multiple side-effecting 'consumers' since some transport messages
    // deserialize to multiple bin_prot items of different types.
    val consume_one_transport_message : t -> (unit Bin_prot.Read.reader list) -> unit

/// This writer is thread-safe. Note that closing the writer will close the entire stream,
/// including the reader end (like in OCaml).
module Writer =
  type t

  val close : t -> unit
  val is_close_started : t -> bool
  val set_close_finished_callback : t -> (Close_reason.t -> unit) -> unit

  val send_bin_prot : t -> 'a Bin_prot.Type_class.writer -> 'a -> unit Send_result.t
  val send_bin_prot_exn : t -> 'a Bin_prot.Type_class.writer -> 'a -> unit

  module For_testing =
    val wait_for_flushed : t -> unit

type t =
  { writer : Writer.t
    reader : Reader.t }

val create : System.IO.Stream -> {| max_message_size : int |} -> t Or_error.t

val create_with_default_max_message_size : System.IO.Stream -> t Or_error.t

val stream : t -> System.IO.Stream

val default_max_message_size : int

module For_testing =
  val bad_message_size_error : Error.t
