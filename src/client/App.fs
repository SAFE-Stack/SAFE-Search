module App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Import
open Fable.Import.Browser
open Fable.PowerPack
open Fable.Helpers.React

open Elmish
open Elmish.React
open Elmish.Browser.Navigation
open Elmish.HMR
open Elmish.React
open Elmish.Debug

open PropertyMapper.Contracts

type SearchState = Searching | Displaying

type Model =
    { Text : string
      Status : SearchState
      Response : FindPropertiesResponse }

type Msg =
| SetSearch of string
| DoSearch
| DisplayTransaction of FindPropertiesResponse
| Error of exn

let loadTransactions text =
    Fetch.fetchAs<FindPropertiesResponse> (sprintf "http://localhost:5000/property/find/%s" text) []

let update msg model : Model * Cmd<Msg> =
    match msg with
    | DoSearch -> { model with Status = Searching }, Cmd.ofPromise loadTransactions model.Text DisplayTransaction Error
    | SetSearch text -> { model with Text = text }, Cmd.none
    | DisplayTransaction data -> { model with Response = data; Status = Displaying }, Cmd.none
    | Error _ -> model, Cmd.none

let init _ =
    let model = { Text = "HENDON"; Status = Displaying; Response = { Results = [||]; Facets = { Towns = []; Localities = []; Districts = []; Counties = []; Prices = [] } } }
    model, Cmd.ofMsg DoSearch

open Fable.Helpers.React
open Fable.Helpers.React.Props

let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]

    div [ ClassName "container" ] [
        div [ ClassName "row" ] [
            div [ ClassName "col" ] [
                h1 [] [ str "Property Search"]
            ]
        ]        
        div [ ClassName "row" ] [
            div [ ClassName "col" ] [
                yield
                    div [ ClassName "form-group" ] [
                        label [ HtmlFor "searchValue" ] [ str "Search for" ]
                        input [
                            ClassName "form-control"
                            Id "searchValue"
                            Placeholder "Enter Search"
                            OnChange (fun ev -> dispatch (SetSearch !!ev.target?value))
                            Client.Style.onEnter DoSearch dispatch ]
                    ]
                yield button [ ClassName "btn btn-primary"; OnClick (fun _ -> dispatch DoSearch) ] [ str "Search!" ]
                match model.Status with
                | Searching ->
                    yield div [ ClassName "progress"; Style [ Height "25px" ] ] [
                      br []
                      div [ ClassName "progress-bar progress-bar-striped progress-bar-animated"; Role "progressbar"; Style [ Width "100%" ] ] [ str "Searching..." ]
                      br []
                    ]
                | Displaying -> ()
            ]
        ]

        div [ ClassName "row" ] [
            div [ ClassName "col" ] [
                table [ ClassName "table table-bordered table-hover" ] [
                    thead [] [
                        tr [] [ toTh "Street"
                                toTh "Town"
                                toTh "County"
                                toTh "Date"
                                toTh "Price" ]
                    ]
                    tbody [] [
                        for row in model.Response.Results ->
                            tr [] [ toTd row.Address.Street
                                    toTd row.Address.TownCity
                                    toTd row.Address.County
                                    toTd (string row.DateOfTransfer)
                                    toTd (string row.Price) ]
                    ]                
                ]
            ]
        ]
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