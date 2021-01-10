module App

open Feliz
open Feliz.UseElmish
open Elmish
open Fable.React.WebComponent
open Browser.Types

type WebComponentEventHandling =
    abstract member dispatchEvent:  Browser.Types.Event -> unit
    abstract member addEventListener: string -> (Event->unit) -> unit


type Model = {
    Text:string
    DerAndereSagt:string
    HtmlEl:WebComponentEventHandling option
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

        
    [<Emit("new CustomEvent($0, $1)")>]
    let createCustomEvent (eventType:string) (data:obj) : CustomEvent = jsNative

module Commands =

    let sendEvent (eventHandlingStuff:WebComponentEventHandling) (customEvent:CustomEvent) =
        fun dispatch ->
            Browser.Dom.console.log "sending  event!"
            eventHandlingStuff.dispatchEvent customEvent |> ignore
        |> Cmd.ofSub

    let addEventListener (eventHandlingStuff:WebComponentEventHandling) eventType =
        fun dispatch ->
            
            eventHandlingStuff.addEventListener eventType (fun c -> 
                Browser.Dom.console.log "event received!"
                dispatch (EventReceived (c :?> CustomEvent))
                )
            
        |> Cmd.ofSub


let init defaultText he =
    match he with
    | None ->
        { Text = defaultText; HtmlEl = None; DerAndereSagt = "" }, Cmd.none
    | Some he ->
        Browser.Dom.console.log "he!"
        Browser.Dom.console.log he
        { Text = defaultText; HtmlEl = Some he; DerAndereSagt = "" }, Commands.addEventListener he "my-little-event"


let update msg state =
    match msg with
    | SetText str ->
        match state.HtmlEl with
        | None ->
            { state with Text = str }, Cmd.none
        | Some htmlEl ->
            let customEvent = Helpers.createCustomEvent "my-little-event" {| detail = str; bubbles = true |}
            { state with Text = str }, Commands.sendEvent htmlEl customEvent
    | EventReceived ev ->
        let entry = ev.detail :?> string
        if entry = state.Text then
            state, Cmd.none
        else
            { state with DerAndereSagt = entry }, Cmd.none

let view state dispatch =
    Html.div [
        Html.h1 state.Text
        Html.h1 $"Der andere sagt: {state.DerAndereSagt}"
        Html.input [
            prop.onChange (SetText >> dispatch) //(fun text -> dispatch (SetText text))
            prop.valueOrDefault state.Text
        ]    
    ]





[<ReactWebComponent>]
let HelloWorld (eventHandling:WebComponentEventHandling, parms:{| defaulttext:string |}) = 
    let state, dispatch = 
        React.useElmish(
            init parms.defaulttext (Some eventHandling),
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


