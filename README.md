# Fable.React.WebComponent
This package provides a compile-time transformation to generate web components from react components.


## Installation:

1. Add this nuget package to your project.
    * https://www.nuget.org/packages/Fable.React.WebComponent
   
2. add following npm packages to you project
    * ```react-to-webcomponent```  
    * ```prop-types```  
      


    ```
    npm install react-to-webcomponent

    npm install prop-types

    ```

## Usage:

How to build a web component!

### 1. Build a react component function of your choice with Feliz or maybe other library. You have to return a ```ReactElement``` and add the ```[<ReactWebComponent>]```attribute!

The input props for you react component must be from the type ```record``` (it should be currently a anonymous record, in order to work with ```react-refresh```, see know issues) or a unit, if you don't have any parameters at all.

Also all properties of you input props must be from typ ```string```, because WebComponents only accept strings as attributes, which are mapped into the props.

So you have to transform the string props to the needed types yourself.


Example: Here a react component with effects
```fsharp
[<ReactWebComponent>]
let SimpleComponent (props:{| start:string |}) =
    let isOkay,start = System.Int32.TryParse props.start
    if isOkay then
        let counter,setCount = React.useState(start)

        React.useEffect(
            fun () ->
                setCount start
        , [| start :> obj |])

        Html.div [
            Html.h1 "The Awesome Counter"
            Html.p counter
            Html.button [
                prop.text "Add Stuff"
                prop.onClick (fun _ -> setCount (counter + 3))
            ]
        ]
    else
        Html.h1 "A counter can only use numbers"

```

Example: static react component (out of simple Fable.React elements)
```fsharp
[<ReactWebComponent>]
let StaticComponent (input:{| arg1:string |}) =
    (p [] [ str (input.arg1) ])

```


Example: a elmish react component with its own elmish loop. Thanks zaid! (using Feliz)
```fsharp
[<ReactWebComponent>]
let ElmishComponent (parms:{| start:string; arg2:string; arg3:string; arg4:string; arg5:string; arg6:string |}) =
    let state, dispatch = 
        React.useElmish(
            init parms.start parms.arg2 parms.arg3 parms.arg4 parms.arg5 parms.arg6, 
            update, 
            [| parms.start :> obj; parms.arg2:> obj; parms.arg3:> obj; parms.arg4:> obj; parms.arg5:> obj |])
    view state dispatch

```

### 2. You have to generate the web component with the ```[<CreateWebComponent>]``` Attribute


Example: all of our components from above
```fsharp
[<CreateReactWebComponent("static-component")>]
let staticWebComp = StaticComponent


[<CreateReactWebComponent("simple-component")>]
let simpleWebComp = SimpleComponent


[<CreateReactWebComponent("elmish-component")>]
let elmishWebComp = ElmishComponent
```

### 3. Add your web component to the your HTML file
```html

<static-component arg1="hello world!"></static-component>

<simple-component start="11"></simple-component>

<elmish-component start="11"
                arg2="hello and"
                arg3="welcome to the"
                arg4="awesome world of"
                arg5="fable 3"
                arg6="and feliz"></elmish-component>

```

## Options


The ```[<CreateReactWebComponent>]``` has a mandatory option, the name of the custom element you want to generate.

Also you have the option to use the ```shadow DOM```(default) or the ```Lite DOM```.

Example:
```fsharp
// for Lite DOM
[<CreateReactWebComponent("my-custom-element", false)>]
// ...
```

## Know Issues:

If you are building a web component out of an elmish react component, the update loop stops working after a ```hot-reload```.
So you should develop you react component fully as react component including render it react style until you are finished with the development!. After that, you can transform it to a web component.

