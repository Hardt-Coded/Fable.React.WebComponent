module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React.WebComponent
open App

importAll "./styles/global.scss"




[<CreateReactWebComponent("hello-world", "style.css")>]
let customeEl = HelloWorld 


[<CreateReactWebComponent("hello-world-two", "style.css")>]
let customeEl2 = HelloWorld



[<CreateReactWebComponent("simple-one", "style.css")>]
let simpleOne = SimpleOne


[<CreateReactWebComponent("simple-two", "style.css")>]
let simpleTwo = SimpleTwo


[<CreateReactWebComponent("simple-three", "style.css")>]
let simpleThree = SimpleThree


[<CreateReactWebComponent("simple-three-embedded", @"public\to-embedd-style.css", true)>]
let simpleThreeEmbedded = SimpleThree


