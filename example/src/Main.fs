module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React.WebComponent
open App

importAll "./styles/global.scss"

[<CreateReactWebComponent("hello-world", true)>]
let customeEl = HelloWorld // |> unbox<ReactToWebComponent.HTMLElement>


[<CreateReactWebComponent("hello-world-two", true)>]
let customeEl2 = HelloWorld2

