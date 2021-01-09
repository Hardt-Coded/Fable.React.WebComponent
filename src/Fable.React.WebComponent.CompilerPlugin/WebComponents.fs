namespace Fable.React.WebComponent

open Fable
open Fable.AST
open Fable.AST.Fable
open Utils
    

#if FABLE_COMPILER    

do()

#else
// Tell Fable to scan for plugins in this assembly
[<assembly:ScanForPlugins>]
do()



    
/// <summary>Transforms a function into a React function component. Make sure the function is defined at the module level</summary>
type ReactWebComponentAttribute(exportDefault: bool) =
    inherit MemberDeclarationPluginAttribute()
    override _.FableMinimumVersion = "3.0"
    
    new() = ReactWebComponentAttribute(exportDefault=false)
    
    /// <summary>Transforms call-site into createElement calls</summary>
    override _.TransformCall(compiler, memb, expr) =
        let membArgs = memb.CurriedParameterGroups |> List.concat
        match expr with
        //| Fable.Call(callee, info, typeInfo, range) when List.length membArgs = List.length info.Args ->
        //    // F# Component()
        //    // JSX <Component />
        //    // JS createElement(Component, inputAnonymousRecord)
        //    (AstUtils.makeCall (AstUtils.makeImport "createElement" "react") [callee; info.Args.[0] ])
        | Fable.Call(callee, info, typeInfo, range) when List.length info.Args = 1 ->
            // F# Component()
            // JSX <Component />
            // JS createElement(Component, inputAnonymousRecord)
            (AstUtils.makeCall (AstUtils.makeImport "createElement" "react") [callee; info.Args.[0] ])
        | Fable.Call(callee, info, typeInfo, range) when List.length info.Args = 2 ->
            // F# Component()
            // JSX <Component />
            // JS createElement(Component, inputAnonymousRecord)
            Fable.Sequential [
                AstUtils.makeImport "createElement" "react"
                AstUtils.emitJs "let myLittleComponent = function(arg) { return $0(theHtmlElementContainer.HtmlElement,arg); }" [callee]
                // Meh: the AST say tupled, but js has only the parms object here
                AstUtils.emitJs "createElement(myLittleComponent, tupledArg)" []
            ]
            
        | _ ->
            // return expression as is when it is not a call expression
            expr
    
    override this.Transform(compiler, file, decl) =
        match decl with
        | MemberNotFunction ->
            // Invalid attribute usage
            let errorMessage = sprintf "Expecting a function declation for %s when using [<ReactWebComponent>]" decl.Name
            compiler.LogWarning(errorMessage, ?range=decl.Body.Range)
            decl
        | MemberNotReturningReactElement ->
            // output of a React function component must be a ReactElement
            let errorMessage = sprintf "Expected function %s to return a ReactElement when using [<ReactWebComponent>]" decl.Name
            compiler.LogWarning(errorMessage, ?range=decl.Body.Range)
            decl
        | _ ->
            if (AstUtils.isCamelCase decl.Name) then
                compiler.LogWarning(sprintf "React function component '%s' is written in camelCase format. Please consider declaring it in PascalCase (i.e. '%s') to follow conventions of React applications and allow tools such as react-refresh to pick it up." decl.Name (AstUtils.capitalize decl.Name))
    
            // do not rewrite components accepting records as input
            if decl.Args.Length = 1 && AstUtils.isRecord compiler decl.Args.[0].Type then
                // check whether the record type is defined in this file
                // trigger warning if that is case
                let definedInThisFile =
                    file.Declarations
                    |> List.tryPick (fun declaration ->
                        match declaration with
                        | Declaration.ClassDeclaration classDecl ->
                            let classEntity = compiler.GetEntity(classDecl.Entity)
                            match decl.Args.[0].Type with
                            | Fable.Type.DeclaredType (entity, genericArgs) ->
                                let declaredEntity = compiler.GetEntity(entity)
                                if classEntity.IsFSharpRecord && declaredEntity.FullName = classEntity.FullName
                                then Some declaredEntity.FullName
                                else None
    
                            | _ -> None
    
                        | Declaration.ActionDeclaration action ->
                            None
                        | _ ->
                            None
                    )
    
                match definedInThisFile with
                | Some recordTypeName ->
                    let errorMsg = String.concat "" [
                        sprintf "Function component '%s' is using a record type '%s' as an input parameter. " decl.Name recordTypeName
                        "This happens to break React tooling like react-refresh and hot module reloading. "
                        "To fix this issue, consider using use an anonymous record instead or use multiple simpler values as input parameters"
                        "Future versions of [<ReactComponent>] might not emit this warning anymore, in which case you can assume that the issue if fixed. "
                        "To learn more about the issue, see https://github.com/pmmmwh/react-refresh-webpack-plugin/issues/258"
                    ]
    
                    compiler.LogWarning(errorMsg, ?range=decl.Body.Range)
    
                | None ->
                    // nothing to report
                    ignore()
                
                { decl with ExportDefault = exportDefault }
            else if decl.Args.Length = 1 && decl.Args.[0].Type = Fable.Type.Unit then
                // remove arguments from functions requiring unit as input
                { decl with Args = [ ]; ExportDefault = exportDefault }
            else if decl.Args.Length = 2  then
                //compiler.LogError (sprintf "%A" decl.Args.[0].Type)
                match decl.Args.[0].Type with
                | Fable.Type.DeclaredType({ EntityRef.FullName = fn; EntityRef.Path = _},[]) when fn.Contains("HTMLElement")->
                    { decl with ExportDefault = exportDefault }
                | _ ->
                    compiler.LogError "ReactWebComponents only accept one anonymous record, a unit or a tuple for HTML Element which is later injected and the parms."    
                    decl
                
                //compiler.LogError (sprintf "%A" decl.Args.[0].Type)
                //{ decl with ExportDefault = exportDefault }
            else
                compiler.LogError "ReactWebComponents only accept one anonymous record, a unit or a tuple with the event dispachter and a anonymous record as parameter."
                decl



type CreateReactWebComponentAttribute(customElementName:string, useShadowDom:bool) =
    inherit MemberDeclarationPluginAttribute()
    override _.FableMinimumVersion = "3.0"

    new(customElementName:string) = CreateReactWebComponentAttribute(customElementName, true)
        

    override _.TransformCall(compiler, memb, expr) =
        expr
    
    override this.Transform(compiler, file, decl) =
        //compiler.LogError (sprintf "%A" decl.Body)
        match decl.Body with
        | Fable.Lambda(arg, body, name) ->
            //compiler.LogError (sprintf "%A" arg)
            //compiler.LogError("arrived Lambda!")
            match arg.Type with
            | Fable.Tuple [Fable.DeclaredType({FullName = injectFn},_); Fable.AnonymousRecordType (fieldName,typList)] when injectFn.Contains("HTMLElement") -> // in case of a event dispatcher injection
                let allAreTypesStrings = typList |> List.forall (fun t -> t = Fable.String)
                if (not allAreTypesStrings) then
                    compiler.LogError "For Webcomponents all properties of the anonymous record must be from type string"
                    decl
                else
                    let oldBody = decl.Body
                    let propTypesRequiredStr =
                        System.String.Join(
                            ", ",
                            fieldName 
                            |> Array.map (fun e -> sprintf "%s: PropTypes.string.isRequired" e)
                        )

                    let webCompBody =
                        Fable.Sequential [
                
                            let reactFunctionWithPropsBody = 
                                AstUtils.makeCall
                                    (AstUtils.makeAnonFunction
                                        AstUtils.unitIdent
                                        (Fable.Sequential [
                                            AstUtils.emitJs "const elem = $0" [ oldBody ]
                                            
                                            AstUtils.makeImport "PropTypes" "prop-types"
                                            AstUtils.emitJs (sprintf "elem.propTypes = { %s }" propTypesRequiredStr) []
                                            AstUtils.emitJs "elem" []
                                        ])
                                    )
                                    []


                            let webComCall =
                                AstUtils.makeCall 
                                    (AstUtils.makeImport "default" "react-to-webcomponent") 
                                    [ 
                                        reactFunctionWithPropsBody; 
                                        AstUtils.makeImport "default" "react"
                                        AstUtils.makeImport "default" "react-dom"
                                        AstUtils.emitJs (sprintf "{ shadow: %s }" (if useShadowDom then "true" else "false")) []
                                    ]


                            
                            AstUtils.emitJs "let theHtmlElementContainer = { HtmlElement: {} }" []
                            //AstUtils.emitJs "" []
                            //AstUtils.emitJs "" []
                            AstUtils.emitJs "let myLittleWebComponent = $0" [ webComCall ]
                            AstUtils.emitJs "theHtmlElementContainer.HtmlElement = myLittleWebComponent.prototype[1]" []
                            AstUtils.emitJs "customElements.define($0,myLittleWebComponent)" [ AstUtils.makeStrConst customElementName]
                        ]
                

                    let func = Fable.Lambda(AstUtils.unitIdent,webCompBody,None)
                    let funcCall = AstUtils.makeCall func []
                        
                    
                    {
                        decl with
                            Body = funcCall
                    }
            | Fable.AnonymousRecordType(fieldName,typList) ->
                let allAreTypesStrings = typList |> List.forall (fun t -> t = Fable.String)
                if (not allAreTypesStrings) then
                    compiler.LogError "For Webcomponents all properties of the anonymous record must be from type string"
                    decl
                else
                    let oldBody = decl.Body
                    let propTypesRequiredStr =
                        System.String.Join(
                            ", ",
                            fieldName 
                            |> Array.map (fun e -> sprintf "%s: PropTypes.string.isRequired" e)
                        )

                    let webCompBody =
                        Fable.Sequential [
                
                            let reactFunctionWithPropsBody = 
                                AstUtils.makeCall
                                    (AstUtils.makeAnonFunction
                                        AstUtils.unitIdent
                                        (Fable.Sequential [
                                            AstUtils.emitJs "const elem = $0" [ oldBody ] 
                                            AstUtils.makeImport "PropTypes" "prop-types"
                                            AstUtils.emitJs (sprintf "elem.propTypes = { %s }" propTypesRequiredStr) []
                                            AstUtils.emitJs "elem" []
                                        ])
                                    )
                                    []


                            let webComCall =
                                AstUtils.makeCall 
                                    (AstUtils.makeImport "default" "react-to-webcomponent") 
                                    [ 
                                        reactFunctionWithPropsBody; 
                                        AstUtils.makeImport "default" "react"
                                        AstUtils.makeImport "default" "react-dom"
                                        AstUtils.emitJs (sprintf "{ shadow: %s }" (if useShadowDom then "true" else "false")) []
                                    ]
                
                
                            AstUtils.emitJs "customElements.define($0,$1)" [ AstUtils.makeStrConst customElementName ; webComCall ]
                        ]
                
                    {
                        decl with
                            Body = webCompBody
                    }
            | _ ->
                compiler.LogError "the react function is not declared with an anonymous record as paramater!"    
                decl
        | _ ->
            compiler.LogError "The imput for the web component must be a react element function generated from [<ReactWebComponents>]!"
            decl
#endif