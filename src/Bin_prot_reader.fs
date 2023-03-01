module Async_rpc.Bin_prot_reader

type 'a t = 'a Bin_prot.Type_class.reader

let read_and_verify_length
  (bin_reader_t : _ t)
  add_len
  buf
  (pos_ref : Bin_prot.Common.pos ref)
  len
  location
  =
  try
    let init_pos = pos_ref.Value in
    let data = bin_reader_t.read buf pos_ref in

    let add_len =
      match add_len with
      | None -> 0
      | Some add_len -> add_len data

    if pos_ref.Value - init_pos + add_len <> len then
      failwithf
        "message length (%d) did not match expected length (%d)"
        (pos_ref.Value - init_pos)
        len

    Ok data
  with
  | e -> Error(Protocol.Rpc_error.bin_io_exn location e)
