module Async_rpc.Writer_with_length

type 'a t = 'a Bin_prot.Type_class.writer

val of_writer : 'a t -> 'a t

val of_type_class : 'a Bin_prot.Type_class.t -> 'a t
