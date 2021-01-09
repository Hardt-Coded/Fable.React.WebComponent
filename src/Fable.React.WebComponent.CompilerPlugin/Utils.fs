module Utils


#if FABLE_COMPILER    

do()

#else
open Fable
open Fable.AST
open Fable.AST.Fable

let (|MemberNotFunction|_|) (decl:MemberDecl) = 
    if decl.Info.IsValue || decl.Info.IsGetter || decl.Info.IsSetter then Some () else None


let (|MemberNotReturningReactElement|_|) (decl:MemberDecl) = 
    if not (AstUtils.isReactElement decl.Body.Type) then Some () else None


let (|MemberNotReturningDelegateReactElement|_|) (decl:MemberDecl) = 
    if not (AstUtils.isFunctionWithReturnValueReactElement decl.Body.Type) then Some () else None
#endif
