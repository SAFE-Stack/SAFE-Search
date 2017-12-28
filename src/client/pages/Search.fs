module Pages.Search

open Elmish
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open PropertyMapper.Contracts

type SearchState = Searching | Displaying

type Model =
    { Text : SearchTerm
      SearchingFor : SearchTerm
      Status : SearchState }

type Msg =
    | SetSearch of string
    | DoSearch of SearchTerm
    | ApplyFilter of string * string
    | SearchCompleted of SearchTerm * SearchResponse
    | SearchError of exn

let init _ = { Text = SearchTerm.Empty; SearchingFor = SearchTerm.Empty; Status = SearchState.Displaying }

let view model dispatch =
    div [ ClassName "col border rounded m-3 p-3 bg-light" ] [
        let progressBarVisibility = match model.Status with | Searching -> "visible" | Displaying -> "invisible"
        let (SearchTerm text) = model.SearchingFor
        yield div [ ClassName "form-group" ] [
            label [ HtmlFor "searchValue" ] [ str "Search for" ]
            input [
                ClassName "form-control"
                Id "searchValue"
                Placeholder "Enter Search"
                OnChange (fun ev -> dispatch (SetSearch !!ev.target?value))
                Client.Style.onEnter (DoSearch model.Text) dispatch
            ]
        ]
        yield div [ ClassName "form-group" ] [
            div [ ClassName "progress" ] [
                div [ ClassName (sprintf "progress-bar progress-bar-striped progress-bar-animated %s" progressBarVisibility)
                      Role "progressbar"
                      Style [ Width "100%" ] ]
                    [ str <| sprintf "Searching for '%s'..." text ]
            ]
        ]
        yield button [ ClassName "btn btn-primary"; OnClick (fun _ -> dispatch (DoSearch model.Text)) ] [ str "Search!" ]
    ]

let findTransactions (text, filter) =
    let filter = filter |> Option.map(fun (facet, value) -> sprintf "?%s=%s" facet value) |> Option.defaultValue ""
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/find/%s%s" text filter) []

let update msg model : Model * Cmd<Msg> =
    match msg with
    | SetSearch text -> { model with Text = SearchTerm text }, Cmd.none
    | DoSearch (SearchTerm text) when System.String.IsNullOrWhiteSpace text || text.Length <= 3 -> model, Cmd.none
    | DoSearch (SearchTerm text as term) ->
        let cmd = Cmd.ofPromise findTransactions (text, None) (fun response -> SearchCompleted(term, response)) SearchError
        { model with Status = Searching; SearchingFor = term }, cmd
    | ApplyFilter (facet, value) ->
        let cmd =
            let (SearchTerm text) = model.SearchingFor
            Cmd.ofPromise findTransactions (text, Some(facet, value)) (fun response -> SearchCompleted(model.SearchingFor, response)) SearchError
        { model with Status = Searching; SearchingFor = model.SearchingFor }, cmd
    | SearchCompleted _ -> { model with Status = Displaying }, Cmd.none
    | SearchError _ -> model, Cmd.none
