module Async_rpc.Versioned_rpc

open Async_rpc
open Async_rpc.Protocol
open Core_kernel
open Bin_prot_generated_types.Lib.Async_rpc_kernel.Src.Versioned_rpc
open System.Threading.Tasks

module Menu =
  let rpc =
    Rpc.create
      { Rpc_description.name = "__Versioned_rpc.Menu"
        Rpc_description.version = 1L }
      Menu.V1.T.bin_query
      Menu.V1.T.bin_response

  type t = Rpc_description.t list

  let of_alist =
    List.map (fun (name, version) ->
      { Rpc_description.name = name
        Rpc_description.version = version })

  let to_alist =
    List.map (fun (description : Rpc_description.t) ->
      (description.name, description.version))

  let add implementations =
    let menu =
      Implementation.create
        (Rpc.bin_query rpc)
        (fun connection_state () ->
          List.map
            (fun implementation ->
              (Implementation.add_connection_state implementation connection_state
               |> Implementation.With_connection_state.rpc_description))
            implementations
          |> to_alist)
        (Rpc.bin_response rpc)
        (Rpc.description rpc)

    menu :: implementations

  let request (connection : Connection.t) =
    task {
      let! response = Rpc.dispatch_async rpc connection ()
      return Result.map of_alist response
    }

  let supported_versions (t : t) (name : string) : int64 Set =
    List.choose
      (fun (description : Rpc_description.t) ->
        if name = description.name then
          Some description.version
        else
          None)
      t
    |> Set.ofList

  module For_testing =
    let create menu : t = of_alist menu

module Connection_with_menu =
  type t =
    { connection : Connection.t
      menu : Menu.t }

  let create (connection : Connection.t) =
    task {
      let! menu = Menu.request connection
      return Result.map (fun menu -> { connection = connection; menu = menu }) menu
    }

  let menu t = t.menu
  let connection t = t.connection
