module Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Versioned_rpc
open Bin_prot.Write
open Bin_prot.Read
open Bin_prot.Size
module Menu = struct
  module V1 = struct
    module T = struct
      type query = unit
      let bin_size_query = bin_size_unit
      let bin_write_query = bin_write_unit
      let bin_writer_query =
        {
          Bin_prot.Type_class.size = bin_size_query;
          Bin_prot.Type_class.write = bin_write_query
        }
      let __bin_read_query__ = __bin_read_unit__
      let bin_read_query = bin_read_unit
      let bin_reader_query =
        {
          Bin_prot.Type_class.read = bin_read_query;
          Bin_prot.Type_class.vtag_read = __bin_read_query__
        }
      let bin_query =
        {
          Bin_prot.Type_class.writer = bin_writer_query;
          Bin_prot.Type_class.reader = bin_reader_query
        }
      type response = (string * int64) list
      let bin_size_response v =
        bin_size_list
          (function
           | (v1, v2) ->
               let size = 0 in
               let size = Bin_prot.Common.(+) size (bin_size_string v1) in
               Bin_prot.Common.(+) size (bin_size_int64 v2)) v
      let bin_write_response buf pos v =
        bin_write_list
          (fun buf ->
             fun pos ->
               function
               | (v1, v2) ->
                   let pos = bin_write_string buf pos v1 in
                   bin_write_int64 buf pos v2) buf pos v
      let bin_writer_response =
        {
          Bin_prot.Type_class.size = bin_size_response;
          Bin_prot.Type_class.write = bin_write_response
        }
      let __bin_read_response__ buf pos_ref vint =
        (__bin_read_list__
           (fun buf ->
              fun pos_ref ->
                let v1 = bin_read_string buf pos_ref in
                let v2 = bin_read_int64 buf pos_ref in (v1, v2))) buf pos_ref vint
      let bin_read_response buf pos_ref =
        (bin_read_list
           (fun buf ->
              fun pos_ref ->
                let v1 = bin_read_string buf pos_ref in
                let v2 = bin_read_int64 buf pos_ref in (v1, v2))) buf pos_ref
      let bin_reader_response =
        {
          Bin_prot.Type_class.read = bin_read_response;
          Bin_prot.Type_class.vtag_read = __bin_read_response__
        }
      let bin_response =
        {
          Bin_prot.Type_class.writer = bin_writer_response;
          Bin_prot.Type_class.reader = bin_reader_response
        }
    end
  end
end
