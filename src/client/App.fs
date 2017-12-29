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
    { SearchModel : Search.Model
      ResultsModel : Results.Model }

type AppMsg =
| SearchMsg of Search.Msg
| ResultsMsg of Results.Msg

let update msg model =
    let updateSearch msg model =
        let searchModel, searchCmd = Search.update msg model.SearchModel
        { model with SearchModel = searchModel }, Cmd.map SearchMsg searchCmd
    match msg with
    | SearchMsg (Search.SearchCompleted(term, response) as msg) ->
        let model, cmd = model |> updateSearch msg
        { model with ResultsModel = Results.update (Results.DisplayResults(term, response)) model.ResultsModel }, cmd
    | SearchMsg msg -> model |> updateSearch msg
    | ResultsMsg (Results.FilterSet(facet, value)) -> model |> updateSearch (Search.ApplyFilter (facet, value))
    | ResultsMsg (Results.ChangePage page) -> model |> updateSearch (Search.ChangePage page)
    | ResultsMsg (Results.SetPostcode postcode) -> model |> updateSearch (Search.DoSearch (Postcode postcode))
    | ResultsMsg (Results.SelectTransaction _ as msg) -> { model with ResultsModel = Results.update msg model.ResultsModel }, Cmd.none
    | ResultsMsg (Results.DisplayResults _) -> model, Cmd.none

let init _ =
    let model =
        { SearchModel = Search.init ()
          ResultsModel = Results.init () }
    model, Cmd.none

let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]

    div [ ClassName "container-fluid" ] [
        div [ ClassName "row" ] [ div [ ClassName "col" ] [ h1 [] [ str "Property Search" ] ] ]               
        div [ ClassName "row" ] [ Pages.Search.view model.SearchModel (SearchMsg >> dispatch) ]
        div [ ClassName "row" ] [ Pages.Results.view model.ResultsModel (ResultsMsg >> dispatch) ]
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