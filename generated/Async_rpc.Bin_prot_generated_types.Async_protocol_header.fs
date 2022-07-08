namespace Async_rpc.Bin_prot_generated_types
open Bin_prot.Write
open Bin_prot.Read
open Bin_prot.Size

module Bounded_int_list =
  type t = int64 list
  let bin_size_t = bin_size_list bin_size_int64
  let bin_write_t = bin_write_list bin_write_int64

  let bin_writer_t =
    { Bin_prot.Type_class.size = bin_size_t
      Bin_prot.Type_class.write = bin_write_t }

  let max_len = 100

  let __bin_read_t__ = __bin_read_list__ bin_read_int64
  let bin_read_t = bin_read_list_with_max_len max_len bin_read_int64

  let bin_reader_t =
    { Bin_prot.Type_class.read = bin_read_t
      Bin_prot.Type_class.vtag_read = __bin_read_t__ }
