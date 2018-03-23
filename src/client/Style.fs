module Client.Style

open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

module KeyCode =
    let enter = 13.
    let upArrow = 38.
    let downArrow =  40.

module R = Fable.Helpers.React

let buttonLink cssClass onClick elements = 
    R.a [ ClassName cssClass
          OnClick (fun _ -> onClick())
          OnTouchStart (fun _ -> onClick())
          Style [ !!("cursor", "pointer") ] ] elements

let onKeyDown keyCodeActions =
    OnKeyDown (fun (ev:React.KeyboardEvent) ->
        keyCodeActions
        |> List.tryFind (fst >> (=) ev.keyCode)
        |> Option.iter (fun (keyCode, action) ->
            ev.preventDefault()
            action ev))
