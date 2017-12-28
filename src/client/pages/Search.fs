module Pages.Search

open Elmish
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open PropertyMapper.Contracts

type SearchState = Searching | Displaying

type SearchParameters = { Facet : (string * string) option; Page : int }

type Model =
    { Text : SearchTerm
      LastSearch : SearchTerm
      Status : SearchState
      Parameters : SearchParameters }

type Msg =
    | SetSearch of string
    | DoSearch of SearchTerm
    | ApplyFilter of string * string
    | ChangePage of int
    | SearchCompleted of SearchTerm * SearchResponse
    | SearchError of exn

let init _ = { Text = SearchTerm.Empty; LastSearch = SearchTerm.Empty; Status = SearchState.Displaying; Parameters = { Facet = None; Page = 0 } }

let view model dispatch =
    div [ ClassName "col border rounded m-3 p-3 bg-light" ] [
        let progressBarVisibility = match model.Status with | Searching -> "visible" | Displaying -> "invisible"
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
        let (SearchTerm text) = model.LastSearch
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

let findTransactions (text, filter, page) =
    let filter = filter |> Option.map(fun (facet, value) -> sprintf "?%s=%s" facet value) |> Option.defaultValue ""
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/find/%s/%d%s" text page filter) []

let update msg model : Model * Cmd<Msg> =
    let initiateSearch model (SearchTerm text as term) facet page =
        let cmd = Cmd.ofPromise findTransactions (text, facet, page) (fun response -> SearchCompleted(term, response)) SearchError
        { model with
            Status = Searching
            LastSearch = term
            Parameters = { Facet = facet; Page = page } }, cmd
    match msg with
    | SetSearch text -> { model with Text = SearchTerm text }, Cmd.none
    | DoSearch (SearchTerm text) when System.String.IsNullOrWhiteSpace text || text.Length <= 3 -> model, Cmd.none
    | DoSearch term -> initiateSearch model term None 0
    | ApplyFilter (facet, value) -> initiateSearch model model.LastSearch (Some(facet, value)) model.Parameters.Page
    | ChangePage page -> initiateSearch model model.LastSearch model.Parameters.Facet page
    | SearchCompleted _ -> { model with Status = Displaying }, Cmd.none
    | SearchError _ -> model, Cmd.none
