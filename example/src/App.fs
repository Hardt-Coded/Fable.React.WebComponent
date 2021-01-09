module App

open Feliz
open Feliz.UseElmish
open Elmish
open Fable.React.WebComponent
open Browser.Types


type Model = {
    Text:string
    HtmlEl:HTMLElement option
}

type Msg =
    | SetText of string
    | EventReceived of CustomEvent

    
module Helpers =

    open Fable.Core
    open Browser.Types

    module Interop =
        [<Emit("Object.assign({},$0,$1)")>]
        let objectAssign (x:obj) (y:obj) = jsNative
    
    let createCustomEvent (eventType:string) (data:obj) : CustomEvent =
        let baseObj =
            {| ``type``= eventType |}
        Interop.objectAssign baseObj data

module Commands =

    let sendEvent (htmlElement:HTMLElement) (customEvent:CustomEvent) =
        fun dispatch ->
            htmlElement.dispatchEvent customEvent |> ignore
        |> Cmd.ofSub

    let addEventListener (htmlElement:HTMLElement) eventType =
        fun dispatch ->
            htmlElement.addEventListener(eventType, fun c -> dispatch (EventReceived (c :?> CustomEvent)))
        |> Cmd.ofSub


let init defaultText he =
    match he with
    | None ->
        { Text = defaultText; HtmlEl = None }, Cmd.none
    | Some he ->
        Browser.Dom.console.log "he!"
        Browser.Dom.console.log he
        { Text = defaultText; HtmlEl = Some he }, Commands.addEventListener he "my-little-event"


let update msg state =
    match msg with
    | SetText str ->
        match state.HtmlEl with
        | None ->
            { state with Text = str }, Cmd.none
        | Some htmlEl ->
            let customEvent = Helpers.createCustomEvent "my-little-event" {| details = str |}
            { state with Text = str }, Commands.sendEvent htmlEl customEvent
    | EventReceived ev ->
        let entry = ev.detail :?> string
        { state with Text = state.Text + entry }, Cmd.none

let view state dispatch =
    Html.div [
        Html.h1 state.Text
        Html.input [
            prop.onChange (SetText >> dispatch) //(fun text -> dispatch (SetText text))
            prop.valueOrDefault state.Text
        ]    
    ]



[<ReactWebComponent>]
let HelloWorld (thisElement:HTMLElement, parms:{| defaulttext:string |}) = 
    let state, dispatch = 
        React.useElmish(
            init parms.defaulttext (Some thisElement),
            update,
            [| parms.defaulttext |] |> Array.map (fun x -> x :> obj)
        )
    view state dispatch


[<ReactWebComponent>]
let HelloWorld2 (parms:{| defaulttext:string |}) = 
    let state, dispatch = 
        React.useElmish(
            init parms.defaulttext None,
            update,
            [| parms.defaulttext |] |> Array.map (fun x -> x :> obj)
        )
    view state dispatch


