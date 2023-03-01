module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot
open System.Threading

type t =
  { tcp_listener : TcpListener
    port : int }

let port t = t.port

let stop_accepting_new_connections t = t.tcp_listener.Stop()

let wait_for_connection t create_connection =
  async {
    while true do
      // Calling tcp_listener.Stop Throws a SocketException which ends the while loop
      // and starting with Async.Start ensures the exception doesn't propagate
      let! tcp_client =
        t.tcp_listener.AcceptTcpClientAsync()
        |> Async.AwaitTask

      create_connection tcp_client
  }
  |> Async.Start

let create_with_tcp_listener
  (tcp_listener : TcpListener)
  time_source
  protocol
  implementations_list
  initial_connection_state
  concurrency
  =
  tcp_listener.Start()
  let port = (tcp_listener.LocalEndpoint :?> IPEndPoint).Port

  let create_connection (tcp_client : TcpClient) =
    // Ownership of the stream is taken by [Async_rpc.Transport] so we don't need to
    // worry about closing.
    let stream = tcp_client.GetStream() :> System.IO.Stream

    let connection_callback =
      function
      | Ok connection ->
        task {
          do! Async_rpc.Connection.close_finished connection
          tcp_client.Close()
        }
        |> ignore
      | Error error ->
        Log.printfn "Connection creation failed %A" error
        tcp_client.Close()

    Async_rpc.Connection.create_with_implementations
      stream
      time_source
      protocol
      {| max_message_size = Async_rpc.Transport.default_max_message_size
         connection_state = initial_connection_state tcp_client.Client |}
      connection_callback
      implementations_list
      concurrency

  let t =
    { port = port
      tcp_listener = tcp_listener }

  wait_for_connection t create_connection

  t

let create
  address
  time_source
  protocol
  implementations_list
  concurrency
  (args : {| port : int
             initial_connection_state : Socket -> Connection.t -> 'connection_state |})
  =
  let tcp_listener = new TcpListener(localaddr = address, port = args.port)

  create_with_tcp_listener
    tcp_listener
    time_source
    protocol
    implementations_list
    args.initial_connection_state
    concurrency

module For_testing =
  let create_on_free_port
    address
    time_source
    protocol
    implementations_list
    concurrency
    (args : {| initial_connection_state : Socket -> Connection.t -> 'connection_state |})
    =
    let tcp_listener = new TcpListener(address, 0)

    create_with_tcp_listener
      tcp_listener
      time_source
      protocol
      implementations_list
      args.initial_connection_state
      concurrency
