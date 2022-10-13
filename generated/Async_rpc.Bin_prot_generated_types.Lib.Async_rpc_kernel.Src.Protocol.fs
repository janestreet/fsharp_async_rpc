module Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol
open Bin_prot.Write
open Bin_prot.Read
open Bin_prot.Size
module Rpc_tag = struct
  type t = string
  let bin_size_t = bin_size_string
  let bin_write_t = bin_write_string
  let bin_writer_t =
    {
      Bin_prot.Type_class.size = bin_size_t;
      Bin_prot.Type_class.write = bin_write_t
    }
  let __bin_read_t__ = __bin_read_string__
  let bin_read_t = bin_read_string
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
end
module Query_id = struct
  type t = int64
  let bin_size_t = bin_size_int64
  let bin_write_t = bin_write_int64
  let bin_writer_t =
    {
      Bin_prot.Type_class.size = bin_size_t;
      Bin_prot.Type_class.write = bin_write_t
    }
  let __bin_read_t__ = __bin_read_int64__
  let bin_read_t = bin_read_int64
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
end
module Unused_query_id = struct
  type t = Query_id.t
  let bin_size_t = Query_id.bin_size_t
  let bin_write_t = Query_id.bin_write_t
  let bin_writer_t =
    {
      Bin_prot.Type_class.size = bin_size_t;
      Bin_prot.Type_class.write = bin_write_t
    }
  let __bin_read_t__ = Query_id.__bin_read_t__
  let bin_read_t = Query_id.bin_read_t
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
end
module Rpc_error = struct
  module T = struct
    module Generated_0 = struct
      type t =
        | Version of int64 
      let bin_size_t =
        function
        | Version args ->
            let size_args = bin_size_int64 args in Bin_prot.Common.(+) size_args 4
      let bin_write_t buf pos =
        function
        | Version args ->
            let pos = Bin_prot.Write.bin_write_variant_int buf pos (-901574920) in
            bin_write_int64 buf pos args
      let bin_writer_t =
        {
          Bin_prot.Type_class.size = bin_size_t;
          Bin_prot.Type_class.write = bin_write_t
        }
      let __bin_read_t__ buf pos_ref vint =
        match vint with
        | (-901574920) -> let arg_1 = bin_read_int64 buf pos_ref in Version arg_1
        | _ -> raise Bin_prot.Common.No_variant_match
      let bin_read_t buf pos_ref =
        let vint = Bin_prot.Read.bin_read_variant_int buf pos_ref in
        try __bin_read_t__ buf pos_ref vint
        with
        | Bin_prot.Common.No_variant_match ->
            let err =
              Bin_prot.Common.ReadError.Variant
                "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t" in
            Bin_prot.Common.raise_read_error err (!pos_ref)
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
    end
    type t =
      | Bin_io_exn of Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.t 
      | Connection_closed 
      | Write_error of Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.t 
      | Uncaught_exn of Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.t 
      | Unimplemented_rpc of Rpc_tag.t * Generated_0.t 
      | Unknown_query_id of Query_id.t 
    let bin_size_t =
      function
      | Bin_io_exn v1 ->
          let size = 1 in
          Bin_prot.Common.(+) size
            (Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_size_t
               v1)
      | Write_error v1 ->
          let size = 1 in
          Bin_prot.Common.(+) size
            (Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_size_t
               v1)
      | Uncaught_exn v1 ->
          let size = 1 in
          Bin_prot.Common.(+) size
            (Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_size_t
               v1)
      | Unimplemented_rpc (v1, v2) ->
          let size = 1 in
          let size = Bin_prot.Common.(+) size (Rpc_tag.bin_size_t v1) in
          Bin_prot.Common.(+) size (Generated_0.bin_size_t v2)
      | Unknown_query_id v1 ->
          let size = 1 in Bin_prot.Common.(+) size (Query_id.bin_size_t v1)
      | Connection_closed -> 1
    let bin_write_t buf pos =
      function
      | Bin_io_exn v1 ->
          let pos = Bin_prot.Write.bin_write_int_8bit buf pos 0 in
          Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_write_t
            buf pos v1
      | Connection_closed -> Bin_prot.Write.bin_write_int_8bit buf pos 1
      | Write_error v1 ->
          let pos = Bin_prot.Write.bin_write_int_8bit buf pos 2 in
          Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_write_t
            buf pos v1
      | Uncaught_exn v1 ->
          let pos = Bin_prot.Write.bin_write_int_8bit buf pos 3 in
          Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_write_t
            buf pos v1
      | Unimplemented_rpc (v1, v2) ->
          let pos = Bin_prot.Write.bin_write_int_8bit buf pos 4 in
          let pos = Rpc_tag.bin_write_t buf pos v1 in
          Generated_0.bin_write_t buf pos v2
      | Unknown_query_id v1 ->
          let pos = Bin_prot.Write.bin_write_int_8bit buf pos 5 in
          Query_id.bin_write_t buf pos v1
    let bin_writer_t =
      {
        Bin_prot.Type_class.size = bin_size_t;
        Bin_prot.Type_class.write = bin_write_t
      }
    let __bin_read_t__ _buf pos_ref _vint =
      Bin_prot.Common.raise_variant_wrong_type
        "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t"
        (!pos_ref)
    let bin_read_t buf pos_ref =
      match Bin_prot.Read.bin_read_int_8bit buf pos_ref with
      | 0 ->
          let arg_1 =
            Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_read_t
              buf pos_ref in
          Bin_io_exn arg_1
      | 1 -> Connection_closed
      | 2 ->
          let arg_1 =
            Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_read_t
              buf pos_ref in
          Write_error arg_1
      | 3 ->
          let arg_1 =
            Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src.Sexp.T.bin_read_t
              buf pos_ref in
          Uncaught_exn arg_1
      | 4 ->
          let arg_1 = Rpc_tag.bin_read_t buf pos_ref in
          let arg_2 = Generated_0.bin_read_t buf pos_ref in
          Unimplemented_rpc (arg_1, arg_2)
      | 5 ->
          let arg_1 = Query_id.bin_read_t buf pos_ref in Unknown_query_id arg_1
      | _ ->
          Bin_prot.Common.raise_read_error
            (Bin_prot.Common.ReadError.Sum_tag
               "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t")
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
  end
end
module Rpc_result = struct
  type 'a t = ('a, Rpc_error.T.t) Core_kernel.Bin_prot_generated_types.Result.t
  let bin_size_t _size_of_a v =
    Core_kernel.Bin_prot_generated_types.Result.bin_size_t _size_of_a
      Rpc_error.T.bin_size_t v
  let bin_write_t _write_a buf pos v =
    Core_kernel.Bin_prot_generated_types.Result.bin_write_t _write_a
      Rpc_error.T.bin_write_t buf pos v
  let bin_writer_t bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_t (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_t (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_t__ _of__a buf pos_ref vint =
    (Core_kernel.Bin_prot_generated_types.Result.__bin_read_t__ _of__a
       Rpc_error.T.bin_read_t) buf pos_ref vint
  let bin_read_t _of__a buf pos_ref =
    (Core_kernel.Bin_prot_generated_types.Result.bin_read_t _of__a
       Rpc_error.T.bin_read_t) buf pos_ref
  let bin_reader_t bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_t (bin_reader_a : _ Bin_prot.Type_class.reader).read)
               buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_t__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_t bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_t (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_t (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
module Query = struct
  type 'a needs_length = {
    tag: Rpc_tag.t ;
    version: int64 ;
    id: Query_id.t ;
    data: 'a }
  let bin_size_needs_length _size_of_a =
    function
    | { tag = v1; version = v2; id = v3; data = v4 } ->
        let size = 0 in
        let size = Bin_prot.Common.(+) size (Rpc_tag.bin_size_t v1) in
        let size = Bin_prot.Common.(+) size (bin_size_int64 v2) in
        let size = Bin_prot.Common.(+) size (Query_id.bin_size_t v3) in
        Bin_prot.Common.(+) size (_size_of_a v4)
  let bin_write_needs_length _write_a buf pos =
    function
    | { tag = v1; version = v2; id = v3; data = v4 } ->
        let pos = Rpc_tag.bin_write_t buf pos v1 in
        let pos = bin_write_int64 buf pos v2 in
        let pos = Query_id.bin_write_t buf pos v3 in _write_a buf pos v4
  let bin_writer_needs_length bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_needs_length__ _of__a _buf pos_ref _vint =
    Bin_prot.Common.raise_variant_wrong_type
      "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.needs_length"
      (!pos_ref)
  let bin_read_needs_length _of__a buf pos_ref =
    let v_tag = Rpc_tag.bin_read_t buf pos_ref in
    let v_version = bin_read_int64 buf pos_ref in
    let v_id = Query_id.bin_read_t buf pos_ref in
    let v_data = _of__a buf pos_ref in
    { tag = v_tag; version = v_version; id = v_id; data = v_data }
  let bin_reader_needs_length bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_needs_length
                (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_needs_length__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_needs_length bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_needs_length (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_needs_length (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
module Response = struct
  type 'a needs_length = {
    id: Query_id.t ;
    data: 'a Rpc_result.t }
  let bin_size_needs_length _size_of_a =
    function
    | { id = v1; data = v2 } ->
        let size = 0 in
        let size = Bin_prot.Common.(+) size (Query_id.bin_size_t v1) in
        Bin_prot.Common.(+) size (Rpc_result.bin_size_t _size_of_a v2)
  let bin_write_needs_length _write_a buf pos =
    function
    | { id = v1; data = v2 } ->
        let pos = Query_id.bin_write_t buf pos v1 in
        Rpc_result.bin_write_t _write_a buf pos v2
  let bin_writer_needs_length bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_needs_length__ _of__a _buf pos_ref _vint =
    Bin_prot.Common.raise_variant_wrong_type
      "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.needs_length"
      (!pos_ref)
  let bin_read_needs_length _of__a buf pos_ref =
    let v_id = Query_id.bin_read_t buf pos_ref in
    let v_data = (Rpc_result.bin_read_t _of__a) buf pos_ref in
    { id = v_id; data = v_data }
  let bin_reader_needs_length bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_needs_length
                (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_needs_length__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_needs_length bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_needs_length (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_needs_length (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
module Stream_query = struct
  module Needs_length_generated_0 = struct
    type 'a t =
      | Query of 'a 
      | Abort 
    let bin_size_t _size_of_a =
      function
      | Query args ->
          let size_args = _size_of_a args in Bin_prot.Common.(+) size_args 4
      | _ -> 4
    let bin_write_t _write_a buf pos =
      function
      | Query args ->
          let pos = Bin_prot.Write.bin_write_variant_int buf pos (-250086680) in
          _write_a buf pos args
      | Abort -> Bin_prot.Write.bin_write_variant_int buf pos 774323088
    let bin_writer_t bin_writer_a =
      {
        Bin_prot.Type_class.size =
          (fun v ->
             bin_size_t (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
        Bin_prot.Type_class.write =
          (fun v ->
             bin_write_t (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
      }
    let __bin_read_t__ _of__a buf pos_ref vint =
      match vint with
      | (-250086680) -> let arg_1 = _of__a buf pos_ref in Query arg_1
      | 774323088 -> Abort
      | _ -> raise Bin_prot.Common.No_variant_match
    let bin_read_t _of__a buf pos_ref =
      let vint = Bin_prot.Read.bin_read_variant_int buf pos_ref in
      try (__bin_read_t__ _of__a) buf pos_ref vint
      with
      | Bin_prot.Common.No_variant_match ->
          let err =
            Bin_prot.Common.ReadError.Variant
              "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t" in
          Bin_prot.Common.raise_read_error err (!pos_ref)
    let bin_reader_t bin_reader_a =
      {
        Bin_prot.Type_class.read =
          (fun buf ->
             fun pos_ref ->
               (bin_read_t (bin_reader_a : _ Bin_prot.Type_class.reader).read)
                 buf pos_ref);
        Bin_prot.Type_class.vtag_read =
          (fun buf ->
             fun pos_ref ->
               fun vtag ->
                 (__bin_read_t__
                    (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                   pos_ref vtag)
      }
    let bin_t bin_a =
      {
        Bin_prot.Type_class.writer =
          (bin_writer_t (bin_a : _ Bin_prot.Type_class.t).writer);
        Bin_prot.Type_class.reader =
          (bin_reader_t (bin_a : _ Bin_prot.Type_class.t).reader)
      }
  end
  type 'a needs_length = 'a Needs_length_generated_0.t
  let bin_size_needs_length _size_of_a v =
    Needs_length_generated_0.bin_size_t _size_of_a v
  let bin_write_needs_length _write_a buf pos v =
    Needs_length_generated_0.bin_write_t _write_a buf pos v
  let bin_writer_needs_length bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_needs_length__ _of__a buf pos_ref vint =
    (Needs_length_generated_0.__bin_read_t__ _of__a) buf pos_ref vint
  let bin_read_needs_length _of__a buf pos_ref =
    (Needs_length_generated_0.bin_read_t _of__a) buf pos_ref
  let bin_reader_needs_length bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_needs_length
                (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_needs_length__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_needs_length bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_needs_length (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_needs_length (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
module Stream_initial_message = struct
  type ('response, 'error) t = {
    unused_query_id: Unused_query_id.t ;
    initial: ('response, 'error) Core_kernel.Bin_prot_generated_types.Result.t }
  let bin_size_t _size_of_response _size_of_error =
    function
    | { unused_query_id = v1; initial = v2 } ->
        let size = 0 in
        let size = Bin_prot.Common.(+) size (Unused_query_id.bin_size_t v1) in
        Bin_prot.Common.(+) size
          (Core_kernel.Bin_prot_generated_types.Result.bin_size_t
             _size_of_response _size_of_error v2)
  let bin_write_t _write_response _write_error buf pos =
    function
    | { unused_query_id = v1; initial = v2 } ->
        let pos = Unused_query_id.bin_write_t buf pos v1 in
        Core_kernel.Bin_prot_generated_types.Result.bin_write_t _write_response
          _write_error buf pos v2
  let bin_writer_t bin_writer_response bin_writer_error =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_t (bin_writer_response : _ Bin_prot.Type_class.writer).size
             (bin_writer_error : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_t
             (bin_writer_response : _ Bin_prot.Type_class.writer).write
             (bin_writer_error : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_t__ _of__response _of__error _buf pos_ref _vint =
    Bin_prot.Common.raise_variant_wrong_type
      "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t"
      (!pos_ref)
  let bin_read_t _of__response _of__error buf pos_ref =
    let v_unused_query_id = Unused_query_id.bin_read_t buf pos_ref in
    let v_initial =
      (Core_kernel.Bin_prot_generated_types.Result.bin_read_t _of__response
         _of__error) buf pos_ref in
    { unused_query_id = v_unused_query_id; initial = v_initial }
  let bin_reader_t bin_reader_response bin_reader_error =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_t
                (bin_reader_response : _ Bin_prot.Type_class.reader).read
                (bin_reader_error : _ Bin_prot.Type_class.reader).read) buf
               pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_t__
                  (bin_reader_response : _ Bin_prot.Type_class.reader).read
                  (bin_reader_error : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_t bin_response bin_error =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_t (bin_response : _ Bin_prot.Type_class.t).writer
           (bin_error : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_t (bin_response : _ Bin_prot.Type_class.t).reader
           (bin_error : _ Bin_prot.Type_class.t).reader)
    }
end
module Stream_response_data = struct
  module Needs_length_generated_0 = struct
    type 'a t =
      | Ok of 'a 
      | Eof 
    let bin_size_t _size_of_a =
      function
      | Ok args ->
          let size_args = _size_of_a args in Bin_prot.Common.(+) size_args 4
      | _ -> 4
    let bin_write_t _write_a buf pos =
      function
      | Ok args ->
          let pos = Bin_prot.Write.bin_write_variant_int buf pos 17724 in
          _write_a buf pos args
      | Eof -> Bin_prot.Write.bin_write_variant_int buf pos 3456156
    let bin_writer_t bin_writer_a =
      {
        Bin_prot.Type_class.size =
          (fun v ->
             bin_size_t (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
        Bin_prot.Type_class.write =
          (fun v ->
             bin_write_t (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
      }
    let __bin_read_t__ _of__a buf pos_ref vint =
      match vint with
      | 17724 -> let arg_1 = _of__a buf pos_ref in Ok arg_1
      | 3456156 -> Eof
      | _ -> raise Bin_prot.Common.No_variant_match
    let bin_read_t _of__a buf pos_ref =
      let vint = Bin_prot.Read.bin_read_variant_int buf pos_ref in
      try (__bin_read_t__ _of__a) buf pos_ref vint
      with
      | Bin_prot.Common.No_variant_match ->
          let err =
            Bin_prot.Common.ReadError.Variant
              "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.t" in
          Bin_prot.Common.raise_read_error err (!pos_ref)
    let bin_reader_t bin_reader_a =
      {
        Bin_prot.Type_class.read =
          (fun buf ->
             fun pos_ref ->
               (bin_read_t (bin_reader_a : _ Bin_prot.Type_class.reader).read)
                 buf pos_ref);
        Bin_prot.Type_class.vtag_read =
          (fun buf ->
             fun pos_ref ->
               fun vtag ->
                 (__bin_read_t__
                    (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                   pos_ref vtag)
      }
    let bin_t bin_a =
      {
        Bin_prot.Type_class.writer =
          (bin_writer_t (bin_a : _ Bin_prot.Type_class.t).writer);
        Bin_prot.Type_class.reader =
          (bin_reader_t (bin_a : _ Bin_prot.Type_class.t).reader)
      }
  end
  type 'a needs_length = 'a Needs_length_generated_0.t
  let bin_size_needs_length _size_of_a v =
    Needs_length_generated_0.bin_size_t _size_of_a v
  let bin_write_needs_length _write_a buf pos v =
    Needs_length_generated_0.bin_write_t _write_a buf pos v
  let bin_writer_needs_length bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_needs_length__ _of__a buf pos_ref vint =
    (Needs_length_generated_0.__bin_read_t__ _of__a) buf pos_ref vint
  let bin_read_needs_length _of__a buf pos_ref =
    (Needs_length_generated_0.bin_read_t _of__a) buf pos_ref
  let bin_reader_needs_length bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_needs_length
                (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_needs_length__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_needs_length bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_needs_length (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_needs_length (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
module Message = struct
  type 'a needs_length =
    | Heartbeat 
    | Query of 'a Query.needs_length 
    | Response of 'a Response.needs_length 
  let bin_size_needs_length _size_of_a =
    function
    | Query v1 ->
        let size = 1 in
        Bin_prot.Common.(+) size (Query.bin_size_needs_length _size_of_a v1)
    | Response v1 ->
        let size = 1 in
        Bin_prot.Common.(+) size (Response.bin_size_needs_length _size_of_a v1)
    | Heartbeat -> 1
  let bin_write_needs_length _write_a buf pos =
    function
    | Heartbeat -> Bin_prot.Write.bin_write_int_8bit buf pos 0
    | Query v1 ->
        let pos = Bin_prot.Write.bin_write_int_8bit buf pos 1 in
        Query.bin_write_needs_length _write_a buf pos v1
    | Response v1 ->
        let pos = Bin_prot.Write.bin_write_int_8bit buf pos 2 in
        Response.bin_write_needs_length _write_a buf pos v1
  let bin_writer_needs_length bin_writer_a =
    {
      Bin_prot.Type_class.size =
        (fun v ->
           bin_size_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).size v);
      Bin_prot.Type_class.write =
        (fun v ->
           bin_write_needs_length
             (bin_writer_a : _ Bin_prot.Type_class.writer).write v)
    }
  let __bin_read_needs_length__ _of__a _buf pos_ref _vint =
    Bin_prot.Common.raise_variant_wrong_type
      "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.needs_length"
      (!pos_ref)
  let bin_read_needs_length _of__a buf pos_ref =
    match Bin_prot.Read.bin_read_int_8bit buf pos_ref with
    | 0 -> Heartbeat
    | 1 ->
        let arg_1 = (Query.bin_read_needs_length _of__a) buf pos_ref in
        Query arg_1
    | 2 ->
        let arg_1 = (Response.bin_read_needs_length _of__a) buf pos_ref in
        Response arg_1
    | _ ->
        Bin_prot.Common.raise_read_error
          (Bin_prot.Common.ReadError.Sum_tag
             "Async_rpc.Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Protocol.fs.needs_length")
          (!pos_ref)
  let bin_reader_needs_length bin_reader_a =
    {
      Bin_prot.Type_class.read =
        (fun buf ->
           fun pos_ref ->
             (bin_read_needs_length
                (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf pos_ref);
      Bin_prot.Type_class.vtag_read =
        (fun buf ->
           fun pos_ref ->
             fun vtag ->
               (__bin_read_needs_length__
                  (bin_reader_a : _ Bin_prot.Type_class.reader).read) buf
                 pos_ref vtag)
    }
  let bin_needs_length bin_a =
    {
      Bin_prot.Type_class.writer =
        (bin_writer_needs_length (bin_a : _ Bin_prot.Type_class.t).writer);
      Bin_prot.Type_class.reader =
        (bin_reader_needs_length (bin_a : _ Bin_prot.Type_class.t).reader)
    }
end
