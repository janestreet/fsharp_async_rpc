namespace Async_rpc.Protocol

open Core_kernel
open Async_rpc.Bin_prot_generated_types
open Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol
open Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Test_mode_protocol
open Core_kernel.Bin_prot_generated_types

module Message =
  type 'a t = 'a Message.needs_length

  let bin_writer_needs_length = Message.bin_writer_needs_length
  let bin_reader_needs_length = Message.bin_reader_needs_length

  let bin_writer_nat0_t : Bin_prot.Nat0.t Message.needs_length Bin_prot.Type_class.writer =
    Message.bin_writer_needs_length Bin_prot.Type_class.bin_writer_nat0

  let bin_reader_nat0_t : Bin_prot.Nat0.t Message.needs_length Bin_prot.Type_class.reader =
    Message.bin_reader_needs_length Bin_prot.Type_class.bin_reader_nat0

module Query_id =
  type t = Query_id.t

  let create = let next = ref 0L in fun () -> System.Threading.Interlocked.Increment next

module Unused_query_id =
  type t = Unused_query_id.t

  let t () = Query_id.create ()

module Query_v1 =
  type 'a t = 'a Query_v1.needs_length

  let map_data
    ({ tag = tag
       version = version
       id = id
       data = data } : 'a t)
    f
    =
    ({ tag = tag
       version = version
       id = id
       data = f data } : 'b t)

module Query =
  type 'a t = 'a Query.needs_length

  let map_data
    ({ tag = tag
       version = version
       id = id
       metadata = metadata
       data = data } : 'a t)
    f
    =
    ({ tag = tag
       version = version
       id = id
       metadata = metadata
       data = f data } : 'b t)

module Response =
  type 'a t = 'a Response.needs_length

  let map_data ({ id = id; data = data } : 'a t) f =
    ({ id = id; data = Result.map f data } : 'b t)

module Stream_query =
  type 'a t = 'a Stream_query.needs_length

  let bin_reader_needs_length = Stream_query.bin_reader_needs_length
  let bin_writer_needs_length = Stream_query.bin_writer_needs_length
  let bin_size_needs_length = Stream_query.bin_size_needs_length

  let bin_writer_nat0_t : Bin_prot.Nat0.t Stream_query.needs_length Bin_prot.Type_class.writer =
    Stream_query.bin_writer_needs_length Bin_prot.Type_class.bin_writer_nat0

  let bin_reader_nat0_t : Bin_prot.Nat0.t Stream_query.needs_length Bin_prot.Type_class.reader =
    Stream_query.bin_reader_needs_length Bin_prot.Type_class.bin_reader_nat0

module Stream_initial_message =
  type ('response, 'error) t = Stream_initial_message.t<'response, 'error>

  let bin_reader_t = Stream_initial_message.bin_reader_t
  let bin_writer_t = Stream_initial_message.bin_writer_t

module Stream_response_data =
  type 'a t = 'a Stream_response_data.needs_length

  let bin_writer_needs_length = Stream_response_data.bin_writer_needs_length

  let bin_writer_nat0_t : Bin_prot.Nat0.t Stream_response_data.needs_length Bin_prot.Type_class.writer =
    Stream_response_data.bin_writer_needs_length Bin_prot.Type_class.bin_writer_nat0

  let bin_reader_nat0_t : Bin_prot.Nat0.t Stream_response_data.needs_length Bin_prot.Type_class.reader =
    Stream_response_data.bin_reader_needs_length Bin_prot.Type_class.bin_reader_nat0

module Sexp = Lib.Dotnet.Core_with_dotnet.Src.Sexp.T
module Rpc_error = Rpc_error.T

module Rpc_error =
  type t = Rpc_error.t

  module Unimplemented_rpc =
    type t = Rpc_error.Generated_0.t

  let sexp_of_located_exn location (exn : exn) =
    Sexp.t.Atom $"Exn during %s{location}: %O{exn}"

  let bin_io_exn location (exn : exn) =
    Rpc_error.Bin_io_exn(sexp_of_located_exn location exn)

  let uncaught_exn location (exn : exn) =
    Rpc_error.Uncaught_exn(sexp_of_located_exn location exn)

  let to_error (t : t) = Error.Of.format "%O" t

module Rpc_result =
  type 'a t = 'a Rpc_result.t

  let try_with (f : unit -> Async<'a t>) location : Async<'a t> =
    async {
      match! Async.Catch(f ()) with
      | Choice1Of2 value -> return value
      | Choice2Of2 exn -> return (Error(Rpc_error.uncaught_exn location exn))
    }

module Response_handler =
  module Result =
    type t =
      | Keep
      | Remove of unit Rpc_result.t

  type t =
    Bin_prot.Nat0.t Response.needs_length
      -> Bin_prot.Common.buf
      -> Bin_prot.Common.pos_ref
      -> Result.t

module Rpc_tag =
  type t = Rpc_tag.t

module Krb =
  module Test_mode_protocol =
    module Ack =
      type t = Ack.t

      let bin_reader_t = Ack.bin_reader_t
      let bin_writer_t = Ack.bin_writer_t

    module Syn =
      type t = Syn.t

      let bin_reader_t = Syn.bin_reader_t
      let bin_writer_t = Syn.bin_writer_t

  module Principal =
    module Name =
      module Stable =
        module V1 =
          type t = Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.t
