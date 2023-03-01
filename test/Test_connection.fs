module Async_rpc.Test.Test_connection

open Core_kernel
open Async_rpc
open Async_rpc.Protocol
open Async_rpc.Test
open System.Threading.Tasks

let expect_message_from_reader reader bin_reader expected =
  Transport.Reader.For_testing.consume_one_transport_message
    reader
    [ Bin_prot_reader.expect bin_reader expected ]

type t =
  { transport : Transport.t
    connection_stream : Fake_network_stream.t }

let writer t = t.transport.writer
let reader t = t.transport.reader
let connection_stream t = t.connection_stream

let create
  (time_sources : {| for_connection : Time_source.t
                     for_fake_network_stream : Time_source.t |})
  =
  let test_stream, connection_stream =
    Fake_network_stream.make_pair time_sources.for_fake_network_stream

  let test_transport =
    Transport.create_with_default_max_message_size test_stream
    |> Result.ok_exn

  let protocol = Known_protocol.Rpc
  let protocol_header = Protocol_version_header.v1 protocol

  // The following sends / reads are part of the standard preamble for an RPC connection;
  // they're done here so that when a [Env.t] is returned the test can immediately start
  // dispatching and receiving messages using the things in the [t].
  Transport.Writer.send_bin_prot_exn
    test_transport.writer
    Protocol_version_header.bin_writer_t
    protocol_header

  let connection =
    Connection.create_async
      connection_stream
      time_sources.for_connection
      protocol
      {| max_message_size = Transport.default_max_message_size |}

  let connection = connection.Result |> Result.ok_exn

  expect_message_from_reader
    test_transport.reader
    Protocol_version_header.bin_reader_t
    protocol_header

  expect_message_from_reader
    test_transport.reader
    Message.bin_reader_nat0_t
    Message.t.Heartbeat

  { transport = test_transport
    connection_stream = connection_stream },
  connection

let create_with_fixed_time_source () =
  create
    {| for_connection = Time_source.Constant.dotnet_epoch
       for_fake_network_stream = Time_source.Constant.dotnet_epoch |}

let expect_message t bin_reader expected =
  expect_message_from_reader t.transport.reader bin_reader expected

let send_response t bin_writer_response response =
  Transport.Writer.send_bin_prot_exn
    t.transport.writer
    (Message.bin_writer_needs_length (Writer_with_length.of_writer bin_writer_response))
    (Message.t.Response response)

let send_string_response t response =
  send_response t Bin_prot.Type_class.bin_writer_string response
