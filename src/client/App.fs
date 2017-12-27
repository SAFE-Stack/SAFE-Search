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

let update msg model =
    match msg with
    | SearchBoxMsg msg ->
        let searchBoxModel, cmd = SearchBox.update msg model.SearchBoxModel
        //TODO: Is this the correct way to "promote" a message to global level
        // and transfer data between two views / components?
        { model with
            SearchResultsModel =
                match msg with
                | SearchBox.SearchCompleted(term, results) ->
                    { SearchResults.SearchTerm = term
                      SearchResults.TotalResults = results.TotalTransactions
                      SearchResults.Results = results.Results }
                    |> Some
                | _ -> model.SearchResultsModel
            SearchBoxModel = searchBoxModel }, cmd |> Cmd.map SearchBoxMsg                
let init _ =
    let model =
        { SearchBoxModel = SearchBox.init ()
          SearchResultsModel = SearchResults.init () }
    model, Cmd.none

let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]

    div [ ClassName "container" ] [
        div [ ClassName "row" ] [ div [ ClassName "col" ] [ h1 [] [ str "Property Search" ] ] ]               
        div [ ClassName "row" ] [ Pages.SearchBox.view model.SearchBoxModel (SearchBoxMsg >> dispatch) ]                
        div [ ClassName "row" ] [ Pages.SearchResults.view model.SearchResultsModel ]
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