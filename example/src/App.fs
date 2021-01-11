module App

open Feliz
open Feliz.UseElmish
open Elmish
open Fable.React.WebComponent
open Browser.Types
open Fable.Core.JsInterop

importAll "./styles/global.scss"

type WebComponentEventHandling =
    abstract member dispatchEvent:  Browser.Types.Event -> unit
    abstract member addEventListener: string -> (Event->unit) -> unit
    abstract member removeEventListener: string -> (Event->unit) -> unit


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


let init (args:{| defaulttext:string |}) eventHandling =
    Browser.Dom.console.log (eventHandling)
    { 
        Text = args.defaulttext; 
        HtmlEl = Some eventHandling; 
        DerAndereSagt = "" 
    }, Commands.addEventListener eventHandling "my-little-event"



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




// This form is currently under work. went into some issues with that!
//[<ReactWebComponent>]
//let HelloWorld (eventHandling:WebComponentEventHandling) (args:{| defaulttext:string |}) = 
//    let state, dispatch = 
//        React.useElmish(
//            init args eventHandling,
//            update,
//            [| args.defaulttext |] |> Array.map (fun x -> x :> obj)
//        )
//    view state dispatch


[<ReactWebComponent>]
let HelloWorld ((eventHandling:WebComponentEventHandling),(args:{| defaulttext:string |})) = 
    let state, dispatch = 
        React.useElmish(
            init args eventHandling,
            update,
            [| args.defaulttext |] |> Array.map (fun x -> x :> obj)
        )
    view state dispatch


[<ReactWebComponent>]
let SimpleOne () =
    Html.h1 "Simple One! "


[<ReactWebComponent>]
let SimpleTwo (args:{| input:string |}) =
    Html.h1 $"Simple Two! input: {args.input}"


[<ReactWebComponent>]
let SimpleThree (eventHandling:WebComponentEventHandling) =
    Browser.Dom.console.log(eventHandling)
    eventHandling.addEventListener
            "my-little-event"
            (fun (e:Browser.Types.Event) -> Browser.Dom.console.log($"event received: {e?detail}"))
        
    Html.h1 $"Simple Three! I can send events, but have no input stuff!"







