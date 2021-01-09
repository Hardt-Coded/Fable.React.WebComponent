module EventHelpers

open Fable.Core
open Browser.Types

module Interop =
    [<Emit("Object.assign({},$0,$1)")>]
    let objectAssign (x:obj) (y:obj) = jsNative

let createCustomEvent (eventType:string) (data:obj) : CustomEvent =
    let baseObj =
        {| ``type``= eventType |}
    Interop.objectAssign baseObj data

