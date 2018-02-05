module App

open Elmish
open Elmish.React
open Elmish.Browser.Navigation
open Elmish.HMR
open Elmish.Debug
open Fable.Core
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Pages

type Model =
    { SearchModel : Search.Model }

type AppMsg =
| SearchMsg of Search.Msg

let update msg model =
    match msg with
    | SearchMsg msg ->
        let searchModel, searchCmd = Search.update msg model.SearchModel
        { model with SearchModel = searchModel }, Cmd.map SearchMsg searchCmd

let init _ =
    let model =
        { SearchModel = Search.init () }
    model, Cmd.none

let view model dispatch =
    div [ ClassName "container-fluid" ] [
        div [ ClassName "row" ] [ div [ ClassName "col" ] [ h1 [] [ str "Property Search" ] ] ]
        div [ ClassName "row" ] [ Search.view model.SearchModel (SearchMsg >> dispatch) ]
    ]

JsInterop.importSideEffects "whatwg-fetch"
JsInterop.importSideEffects "babel-polyfill"

// App
Program.mkProgram init update view
|> Program.toNavigable (fun _ -> "#home") (fun _ m -> m, Cmd.none)
|> Program.withConsoleTrace
|> Program.withHMR
|> Program.withReact "elmish-app"
|> Program.withDebugger
|> Program.run