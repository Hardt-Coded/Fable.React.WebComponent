module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React.WebComponent
open App
open ReactToWebComponent



importAll "./styles/global.scss"

let customeElMeh = HelloWorld 


[<CreateReactWebComponent("hello-world", true)>]
let customeEl = HelloWorld 


[<CreateReactWebComponent("hello-world-two", true)>]
let customeEl2 = HelloWorld




