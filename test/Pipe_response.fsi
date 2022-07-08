module Async_rpc.Test.Pipe_response

open Async_rpc.Protocol

// This variant helper is used so that in the test, we can interleave initial messages
// with update messages in the same data structure.
type 'a initial =
  { bin_writer_error : 'a Bin_prot.Type_class.writer
    query_id : Query.Id.t
    response : Result<unit, 'a> }

type update =
  { query_id : Query.Id.t
    response : string Stream_response_data.t }

type 'a t =
  | Initial of 'a initial
  | Update of update

val send : _ t -> Test_connection.t -> unit
