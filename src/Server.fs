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
    port : int
    // Tracks the number of active connections plus 1 for the server itself.
    outstanding_connections : CountdownEvent
    // Determined when the server close is initiated. Used to trigger existing
    // connections shutdown.
    close_initiated : TaskCompletionSource<unit>
    // Used to signal when the server finished closing. We keep it around to be
    // able to call [close] multiple times (subsequent calls simply return this
    // task source).
    close_finished : TaskCompletionSource<unit>
    // We use this to be able to execute the closing logic at most once.
    close_run_once : Thread_safe_run_once.t }

let port t = t.port

let stop_accepting_new_connections t = t.tcp_listener.Stop()

let close t =
  Thread_safe_run_once.run_if_first_time t.close_run_once (fun () ->
    stop_accepting_new_connections t

    // This will signal the existing connections to close
    t.close_initiated.SetResult(())

    if t.outstanding_connections.Signal() then
      // No outstanding connections (if there was any in the process of being
      // established, outstanding_connection.AddCount will throw an exception
      // there since the countdown already reached 0)
      t.close_finished.SetResult(())
    else
      async {
        // Delaying to make sure we don't block the thread and return from [close]
        // immediately.
        t.outstanding_connections.Wait()
        t.close_finished.SetResult(())
      }
      |> Async.Start)

  t.close_finished.Task

let wait_for_connection t create_connection =
  async {
    try
      while true do
        // Calling tcp_listener.Stop Throws a SocketException which ends the while loop
        // and starting with Async.Start ensures the exception doesn't propagate
        let! tcp_client =
          t.tcp_listener.AcceptTcpClientAsync()
          |> Async.AwaitTask

        create_connection tcp_client

    with
    | exc -> Log.printfn "Stopped waiting for new tcp clients: %A" exc
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

  // 1 represents the TCP listener itself
  let outstanding_connections = new CountdownEvent(1)

  let server_close_initiated = new TaskCompletionSource<_>()

  let create_connection (tcp_client : TcpClient) =
    // Ownership of the stream is taken by [Async_rpc.Transport] so we don't need to
    // worry about closing.
    let stream = tcp_client.GetStream() :> System.IO.Stream

    outstanding_connections.AddCount()

    let connection_callback =
      function
      | Ok connection ->
        task {
          let close_finished = Async_rpc.Connection.close_finished connection

          let! server_close_initiated_or_connection_closed =
            Task.WhenAny(server_close_initiated.Task, close_finished)

          if
            server_close_initiated_or_connection_closed.Equals
              (server_close_initiated.Task)
          then
            Async_rpc.Connection.close connection
            do! close_finished

          Log.printfn "Connection finished closing, closing the underlying TCP client.."
          tcp_client.Close()

          outstanding_connections.Signal()
          |> (ignore : bool -> unit)
        }
        |> ignore
      | Error error ->
        Log.printfn "Connection creation failed %A" error
        tcp_client.Close()

        outstanding_connections.Signal()
        |> (ignore : bool -> unit)

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
      tcp_listener = tcp_listener
      outstanding_connections = outstanding_connections
      close_initiated = server_close_initiated
      close_finished = new TaskCompletionSource<_>()
      close_run_once = Thread_safe_run_once.create () }

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
