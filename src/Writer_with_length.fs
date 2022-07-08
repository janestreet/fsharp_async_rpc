module Async_rpc.Writer_with_length

module Nat0 = Bin_prot.Nat0

type 'a t = 'a Bin_prot.Type_class.writer

let of_writer ({ write = write; size = size } : 'a t) =
  let write buf pos a =
    let len = Nat0.of_int (size a) in
    let pos = Bin_prot.Write.bin_write_nat0 buf pos len in
    write buf pos a

  let size a =
    let len = Nat0.of_int (size a) in

    Bin_prot.Size.bin_size_nat0 len + len

  ({ write = write; size = size } : 'a t)

let of_type_class (bin_a : _ Bin_prot.Type_class.t) = of_writer bin_a.writer
