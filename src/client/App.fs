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
    { SearchBoxModel : SearchBox.Model
      SearchResultsModel : SearchResults.Model option }

type Msg =
| SearchBoxMsg of SearchBox.Msg
| SearchResultsMsg of SearchResults.Msg

let update msg model =
    match msg with
    | SearchBoxMsg msg ->
        let searchBoxModel, cmd = SearchBox.update msg model.SearchBoxModel

        { model with
            SearchResultsModel =
                match msg with
                | SearchBox.SearchCompleted(term, results) ->
                    { SearchResults.SearchTerm = term
                      SearchResults.Response = results }
                    |> Some
                | SearchBox.SetSearch _
                | SearchBox.DoSearch _
                | SearchBox.SearchError _
                | SearchBox.FilterSearch _ ->
                    model.SearchResultsModel
            SearchBoxModel = searchBoxModel }, cmd |> Cmd.map SearchBoxMsg
    | SearchResultsMsg (SearchResults.Filter (facet, value)) ->
        let searchBoxModel, searchBoxCmd =
            let model, cmd = SearchBox.update (SearchBox.FilterSearch (facet, value)) model.SearchBoxModel
            model, cmd |> Cmd.map SearchBoxMsg
        { model with SearchBoxModel = searchBoxModel }, searchBoxCmd

let init _ =
    let model =
        { SearchBoxModel = SearchBox.init ()
          SearchResultsModel = SearchResults.init () }
    model, Cmd.none

let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]

    div [ ClassName "container-fluid" ] [
        div [ ClassName "row" ] [ div [ ClassName "col" ] [ h1 [] [ str "Property Search" ] ] ]               
        div [ ClassName "row" ] [ Pages.SearchBox.view model.SearchBoxModel (SearchBoxMsg >> dispatch) ]                
        div [ ClassName "row" ] [ Pages.SearchResults.view model.SearchResultsModel (SearchResultsMsg >> dispatch) ]
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