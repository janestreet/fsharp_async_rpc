module Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal
open Bin_prot.Write
open Bin_prot.Read
open Bin_prot.Size
module Stable =
  module Name =
    module V1 =
      module T =
        module Generated_0 =
          type t = {
            service: string ;
            hostname: string }
          let bin_size_t =
            function
            | { service = v1; hostname = v2 } ->
                let size = 0 in
                let size = Bin_prot.Common.(+) size (bin_size_string v1) in
                Bin_prot.Common.(+) size (bin_size_string v2)
          let bin_write_t buf pos =
            function
            | { service = v1; hostname = v2 } ->
                let pos = bin_write_string buf pos v1 in bin_write_string buf pos v2
          let bin_writer_t =
            {
              Bin_prot.Type_class.size = bin_size_t;
              Bin_prot.Type_class.write = bin_write_t
            }
          let __bin_read_t__ _buf pos_ref _vint =
            Bin_prot.Common.raise_variant_wrong_type
              "Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.fs.t"
              (!pos_ref)
          let bin_read_t buf pos_ref =
            let v_service = bin_read_string buf pos_ref in
            let v_hostname = bin_read_string buf pos_ref in
            { service = v_service; hostname = v_hostname }
          let bin_reader_t =
            {
              Bin_prot.Type_class.read = bin_read_t;
              Bin_prot.Type_class.vtag_read = __bin_read_t__
            }
          let bin_t =
            {
              Bin_prot.Type_class.writer = bin_writer_t;
              Bin_prot.Type_class.reader = bin_reader_t
            }
        type t =
          | User of string 
          | Service of Generated_0.t 
        let bin_size_t =
          function
          | User v1 -> let size = 1 in Bin_prot.Common.(+) size (bin_size_string v1)
          | Service v1 ->
              let size = 1 in Bin_prot.Common.(+) size (Generated_0.bin_size_t v1)
        let bin_write_t buf pos =
          function
          | User v1 ->
              let pos = Bin_prot.Write.bin_write_int_8bit buf pos 0 in
              bin_write_string buf pos v1
          | Service v1 ->
              let pos = Bin_prot.Write.bin_write_int_8bit buf pos 1 in
              Generated_0.bin_write_t buf pos v1
        let bin_writer_t =
          {
            Bin_prot.Type_class.size = bin_size_t;
            Bin_prot.Type_class.write = bin_write_t
          }
        let __bin_read_t__ _buf pos_ref _vint =
          Bin_prot.Common.raise_variant_wrong_type
            "Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.fs.t"
            (!pos_ref)
        let bin_read_t buf pos_ref =
          match Bin_prot.Read.bin_read_int_8bit buf pos_ref with
          | 0 -> let arg_1 = bin_read_string buf pos_ref in User arg_1
          | 1 -> let arg_1 = Generated_0.bin_read_t buf pos_ref in Service arg_1
          | _ ->
              Bin_prot.Common.raise_read_error
                (Bin_prot.Common.ReadError.Sum_tag
                   "Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.fs.t")
                (!pos_ref)
        let bin_reader_t =
          {
            Bin_prot.Type_class.read = bin_read_t;
            Bin_prot.Type_class.vtag_read = __bin_read_t__
          }
        let bin_t =
          {
            Bin_prot.Type_class.writer = bin_writer_t;
            Bin_prot.Type_class.reader = bin_reader_t
          }
