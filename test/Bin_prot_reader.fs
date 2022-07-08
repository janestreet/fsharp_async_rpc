module Async_rpc.Test.Bin_prot_reader

type 'a t = 'a Bin_prot.Type_class.reader

let expect (t : _ Bin_prot.Type_class.reader) expected array pos_ref =
  let value = t.read array pos_ref
  Assert.AreEqual(expected, value)
