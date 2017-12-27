module Pages.SearchBox

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
    | SearchCompleted of SearchTerm * SearchResponse
    | SearchError of exn
    | FilterSearch of string * string

let init _ = { Text = SearchTerm.Empty; SearchingFor = SearchTerm.Empty; Status = SearchState.Displaying }

let view model dispatch =
    div [ ClassName "col border rounded m-3 p-3" ] [
        yield
            div [ ClassName "form-group" ] [
                label [ HtmlFor "searchValue" ] [ str "Search for" ]
                input [
                    ClassName "form-control"
                    Id "searchValue"
                    Placeholder "Enter Search"
                    OnChange (fun ev -> dispatch (SetSearch !!ev.target?value))
                    Client.Style.onEnter (DoSearch model.Text) dispatch ]
            ]
        yield button [ ClassName "btn btn-primary"; OnClick (fun _ -> dispatch (DoSearch model.Text)) ] [ str "Search!" ]
        match model.Status with
        | Searching ->
            let (SearchTerm text) = model.SearchingFor
            yield div [ ClassName "progress"; Style [ Height "25px" ] ] [
              br []
              div [ ClassName "progress-bar progress-bar-striped progress-bar-animated"; Role "progressbar"; Style [ Width "100%" ] ] [ str <| sprintf "Searching for '%s'..." text ]
              br []
            ]
        | Displaying -> ()
    ]

let findTransactions (text, filter) =
    let filter = filter |> Option.map(fun (facet, value) -> sprintf "?%s=%s" facet value) |> Option.defaultValue ""
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/find/%s%s" text filter) []

let update msg model : Model * Cmd<Msg> =
    match msg with
    | SetSearch text -> { model with Text = SearchTerm text }, Cmd.none
    | DoSearch (SearchTerm text) when
        System.String.IsNullOrWhiteSpace text ||
        text.Length <= 3 -> { model with Status = Displaying }, Cmd.none
    | SearchCompleted _ -> { model with Status = Displaying }, Cmd.none
    | SearchError _ -> { model with Status = Displaying }, Cmd.none
    | DoSearch (SearchTerm text as term) ->
        let cmd = Cmd.ofPromise findTransactions (text, None) (fun response -> SearchCompleted(term, response)) SearchError
        { model with Status = Searching; SearchingFor = term }, cmd
    | FilterSearch (facet, value) ->
        let cmd =
            let (SearchTerm text) = model.SearchingFor
            Cmd.ofPromise findTransactions (text, Some(facet, value)) (fun response -> SearchCompleted(model.SearchingFor, response)) SearchError
        { model with Status = Searching; SearchingFor = model.SearchingFor }, cmd