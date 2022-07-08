module Async_rpc.Protocol_version_header

open Core_kernel

module Protocol_version_header = Async_rpc.Bin_prot_generated_types.Bounded_int_list

module Protocol_kind =
  type t = unit Known_protocol.t

type t = T of Protocol_version_header.t

let create_exn
  (protocol : 'a Known_protocol.t)
  (args : {| supported_versions : int64 list |})
  =
  let length = 1 + args.supported_versions.Length

  if length > Protocol_version_header.max_len then
    failwithf
      "List is too large. (length: %d) (max_len: %d)"
      length
      Protocol_version_header.max_len

  T(
    Known_protocol.magic_number protocol
    :: args.supported_versions
  )

let bin_reader_t =
  Bin_prot.Type_class.cnv_reader (fun t -> T t) Protocol_version_header.bin_reader_t

let bin_writer_t =
  Bin_prot.Type_class.cnv_writer (fun (T t) -> t) Protocol_version_header.bin_writer_t

let get_protocol (T t) =
  let protocols, versions =
    List.partition_map
      (fun v ->
        match Map.tryFind (int64 v) Known_protocol.by_magic_number with
        | Some p -> Either.First p
        | None -> Either.Second(int64 v))
      t

  match protocols with
  | [] -> Ok(None, Set.ofList versions)
  | [ p ] -> Ok(Some p, Set.ofList versions)
  | _ ->
    Or_error.Error.format
      "[Protocol_version_header.negotiate]: multiple magic numbers seen. (protocols: %A) (versions: %A)"
      (protocols : Protocol_kind.t list)
      (versions : int64 list)


// In OCaml, there is a [allow_legacy_peer] argument; it's removed here
// because in async_rpc it's fixed to true.
let negotiate_allowing_legacy_peer us peer =
  Result.let_syntax {
    let! us_protocol, us_versions = get_protocol us
    let! peer_protocol, peer_versions = get_protocol peer

    let! us_protocol =
      match us_protocol with
      | Some x -> Ok x
      | None -> Or_error.Error.format "No magic numbers seen (us_version: %A)" us_versions

    let peer_protocol =
      match peer_protocol with
      | Some x -> x
      | None ->
        (* we allow legacy peers by assuming peer is speaking our protocol *)
        us_protocol

    let! version =
      if not (us_protocol = peer_protocol) then
        Or_error.Error.format
          "[Protocol_version_header.negotiate]: conflicting magic protocol numbers. us: %A. them: %A"
          (us_protocol : Protocol_kind.t)
          (peer_protocol : Protocol_kind.t)

      else
        let protocol = us_protocol

        match Set.maxElement (Set.intersect us_versions peer_versions) with
        | Some version -> Ok version
        | None ->
          Or_error.Error.format
            "[Protocol_version_header.negotiate]: no shared version numbers. us: %A. peer: %A. protocol: %A"
            (us_versions : int64 Set)
            (peer_versions : int64 Set)
            (protocol : Protocol_kind.t)

    return version
  }

let v1 (protocol : 'a Known_protocol.t) =
  create_exn protocol {| supported_versions = [ 1L ] |}

let handshake_and_negotiate_version t (transport : Transport.t) =
  Result.let_syntax {
    do!
      Transport.Writer.send_bin_prot transport.writer bin_writer_t t
      |> Transport.Send_result.to_or_error

    let! peer =
      Transport.Reader.read_one_message_bin_prot transport.reader bin_reader_t
      |> Result.mapError Transport.Reader.Error.to_error

    // In OCaml some errors trigger cleanup (close) in this function, others cleanup in
    // the caller. There's no significant difference between one and the other during the
    // handshake, so for simplicity we explicitly stick to returning an error and let the
    // caller clean up.
    match negotiate_allowing_legacy_peer t peer with
    | Ok version -> return version
    | Error e -> return! (Or_error.Error.format "Protocol negotiation failed: %A" e)
  }
