module Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Test_mode_protocol
open Bin_prot.Write
open Bin_prot.Read
open Bin_prot.Size
module Ack =
  type t = Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Or_error.T.t<unit>
  let bin_size_t v =
    Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Or_error.T.bin_size_t
      bin_size_unit v
  let bin_write_t buf pos v =
    Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Or_error.T.bin_write_t
      bin_write_unit buf pos v
  let bin_writer_t =
    {
      Bin_prot.Type_class.size = bin_size_t;
      Bin_prot.Type_class.write = bin_write_t
    }
  let __bin_read_t__ buf pos_ref vint =
    (Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Or_error.T.__bin_read_t__
       bin_read_unit) buf pos_ref vint
  let bin_read_t buf pos_ref =
    (Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Or_error.T.bin_read_t
       bin_read_unit) buf pos_ref
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
module Syn =
  type t = Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.t
  let bin_size_t =
    Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.bin_size_t
  let bin_write_t =
    Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.bin_write_t
  let bin_writer_t =
    {
      Bin_prot.Type_class.size = bin_size_t;
      Bin_prot.Type_class.write = bin_write_t
    }
  let __bin_read_t__ =
    Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.__bin_read_t__
  let bin_read_t =
    Async_rpc.Bin_prot_generated_types.Lib.Krb.Public.Src.Principal.Stable.Name.V1.T.bin_read_t
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
