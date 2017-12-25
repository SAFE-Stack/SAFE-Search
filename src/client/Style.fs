module Client.Style

open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

module R = Fable.Helpers.React

let buttonLink cssClass onClick elements = 
    R.a [ ClassName cssClass
          OnClick (fun _ -> onClick())
          OnTouchStart (fun _ -> onClick())
          Style [ !!("cursor", "pointer") ] ] elements

let onEnter msg dispatch =
    OnKeyDown (fun (ev:React.KeyboardEvent) ->
        match ev with 
        | _ when ev.keyCode = 13. ->
            ev.preventDefault()
            dispatch msg
        | _ -> ())
