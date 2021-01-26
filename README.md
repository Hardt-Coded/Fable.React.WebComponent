# Fable.React.WebComponent
This package provides a compile-time transformation to generate web components from react components.


## Installation:

1. Add this nuget package to your project and Feliz.
    * Feliz (or if you want Feliz.UseElmish)
    * https://www.nuget.org/packages/Fable.React.WebComponent
   
2. add following npm packages to you project
    * ```fable-react-to-webcomponent```  (please be aware, that with version 0.0.3 you have to use `fable-react-to-webcomponent` instead of `react-to-webcomponent`)
    * ```prop-types```  
      


    ```
    npm install fable-react-to-webcomponent

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

### New in Version 0.0.2
Also with the Version 0.0.2 it is possible to inject a helper for EventHandling.
Your EmlishComponent must have a tupled parameter with the helper and the args.
The Helper is not part of the package. (it's not possible to have a compiler plugin and fable code inside one package)

You can inject the eventHandling-Stuff into your init and you it there. See the new example in the repo.

```fsharp

type WebComponentEventHandling =
    abstract member dispatchEvent:  Browser.Types.Event -> unit
    abstract member addEventListener: string -> (Event->unit) -> unit
    abstract member removeEventListener: string -> (Event->unit) -> unit

let ElmishComponent ((eventHandling:WebComponentEventHandling),parms:{| start:string; arg2:string; arg3:string; arg4:string; arg5:string; arg6:string |}) =
    let state, dispatch = 
        React.useElmish(
            init eventHandling parms, 
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

### 3. Inject styling link (with version 0.0.3) into the webcomponent

because of the use of shadow dom, web components ignore the global stylings.
but now you can inject your style sheet into the webcomponent.

1. you have to configure webpack, that it's build an standalone css-file (you can only inject one currenty!). often it's configured already for production, but you also need to build this for development, if you develop the web components directly.  
`plugins:` make sure, that `MiniCssExtractPlugin` is also used in dev mode  
`module: { rules: [ :` Like in the example project, you see, the isProduction if expression is commented out and the MiniCssExtractPlugin.loader is always used.
    ```js

        ...


        plugins: isProduction ?
            commonPlugins.concat([
                new MiniCssExtractPlugin({ filename: 'style.css' }),
                new CopyWebpackPlugin({
                    patterns: [
                        { from: resolve(CONFIG.assetsDir) }
                    ]
                }),
            ])
            : commonPlugins.concat([
                new MiniCssExtractPlugin({ filename: 'style.css' }), // ADD THIS ONE TO THE DEV CONDITION
                new ReactRefreshWebpackPlugin()
            ]),
    
        ...


        {
                test: /\.(sass|scss|css)$/,
                exclude: /global.scss/,
                use: [
                    //isProduction
                    //    ? MiniCssExtractPlugin.loader
                    //    : 'style-loader',
                    MiniCssExtractPlugin.loader,
                    {
                        loader: 'css-loader',
                        options: {
                            modules: true
                        }
                    },
                    {
                        loader: 'sass-loader',
                        options: { implementation: require("sass") }
                    }
                ],
            },
            {
                test: /\.(sass|scss|css)$/,
                include: /global.scss/,
                use: [
                    //isProduction
                    //    ? MiniCssExtractPlugin.loader
                    //    : 'style-loader',
                    MiniCssExtractPlugin.loader,
                    {
                        loader: 'css-loader'
                    },
                    {
                        loader: 'sass-loader',
                        options: { implementation: require("sass") }
                    }
                ],
            },

        ...

    ```
2. There is a new flag on the `CreateReactWebComponent` Attibute. Here you can enter the css file name, that should be injected into the shadow DOM.
    ```fsharp
    [<CreateReactWebComponent("static-component", "style.css")>]
    let staticWebComp = StaticComponent


    [<CreateReactWebComponent("simple-component", "style.css")>]
    let simpleWebComp = SimpleComponent


    [<CreateReactWebComponent("elmish-component", "style.css")>]
    let elmishWebComp = ElmishComponent
    ``` 

Now you styles will be injected into your web component.


### 3a. Inject acutal css code into the generated js code for the web component (version 0.0.4)

All you need is the css before you start transpiling the code with Fable 3.  
So the fancy scss to css convertion, which webpack does for you doesn't work here. You may have to run a scss transpiler before.  
The functionality loads the script and puts it into a `style` tag and make it part of the web component itself. So no external dependency like in the previous approach.  
I helps, when you have you webcomponent hostet on another endpoint, like you do it in micro services.
The js file, which you generate, contains the actuall css code.

How do you do that?

make the css parameter must point to an actual file, which the fable compiler can reach. So it's releative to the path you running Fable 3 in.  
After that you set the flag "embeddCss" on true. Done.
```fsharp

    // for example here the css file is in the folder public
    
    [<CreateReactWebComponent("simple-three-embedded", @"public\to-embedd-style.css", true)>]
    let simpleThreeEmbedded = SimpleThree

    ``` 



### 5. Add your web component to the your HTML file
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


## Release Notes

### 0.0.6
```
- fixes around the event helper, which is injected. The add- and removeEventListener and the dispatchEvent function were the function form the js window object.  
  It's now changed to the shadow-dom element.

  if you want to catch bubbled events, you use document or window to adfd the eventListener. Like:

  // JS
  document.addEventListener("fancy-event", function(e) ... ) 


  // Fable
  Browser.Dom.document.addEventListener("fancy-event", (fun e -> ...))




```

