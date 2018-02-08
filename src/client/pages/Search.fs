module Pages.Search

open Elmish
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open PropertyMapper.Contracts

type SearchState = Searching | Displaying

type SearchParameters = { Facet : (string * string) option; Page : int }
type SearchResults = { SearchTerm : SearchTerm; Response : SearchResponse }

type Model =
    { Text : SearchTerm
      LastSearch : SearchTerm
      Status : SearchState
      Parameters : SearchParameters
      SearchResults : SearchResults option
      Selected : PropertyResult option }

type Msg =
    | SetSearch of string
    | DoSearch of SearchTerm
    | SetFilter of string * string
    | ChangePage of int
    | SearchCompleted of SearchTerm * SearchResponse
    | SearchError of exn
    | SelectTransaction of PropertyResult

let init _ =
    { Text = SearchTerm.Empty
      LastSearch = SearchTerm.Empty
      Status = SearchState.Displaying
      Parameters = { Facet = None; Page = 0 }
      SearchResults = None
      Selected = None }

let viewResults searchResults dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toDetailsLink row c =
        td [ Scope "row" ] [
            a [ Href "#"
                DataToggle "modal"
                unbox ("data-target", "#exampleModal")
                OnClick(fun _ -> dispatch (SelectTransaction row)) ] [ str c ]
        ]
    let toTd c = td [ Scope "row" ] [ str c ]
    div [ ClassName "border rounded m-3 p-3 bg-light" ] [
        match searchResults with
        | None -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Please perform a search!" ] ] ]
        | Some { Response = { Results = [||] } } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Your search yielded no results." ] ] ]
        | Some { SearchTerm = term; Response = response } ->
            let hits = response.TotalTransactions |> Option.map (commaSeparate >> sprintf " (%s hits)") |> Option.defaultValue ""
            let description =
                match term with
                | Term term -> sprintf "Search results for '%s'%s." term hits
                | Postcode postcode -> sprintf "Showing properties within a 1km radius of '%s'%s." postcode hits

            yield div [ ClassName "row" ] [
                div [ ClassName "col-2" ] [ Filter.createFilters (SetFilter >> dispatch) response.Facets ]
                div [ ClassName "col-10" ] [
                    div [ ClassName "row" ] [ div [ ClassName "col" ] [ h4 [] [ str description ] ] ]
                    table [ ClassName "table table-bordered table-hover" ] [
                        thead [] [
                            tr [] [ toTh "Street"
                                    toTh "Town"
                                    toTh "Postcode"
                                    toTh "Date"
                                    toTh "Price" ]
                        ]
                        tbody [] [
                            for row in response.Results ->
                                let postcodeLink =
                                    a
                                        [ Href "#"; OnClick(fun _ -> row.Address.PostCode |> Option.iter(Postcode >> DoSearch >> dispatch)) ]
                                        [ row.Address.PostCode |> Option.defaultValue "" |> str ]
                                tr [] [ toDetailsLink row row.Address.FirstLine
                                        toTd row.Address.TownCity
                                        td [ Scope "row" ] [ postcodeLink ]
                                        toTd (row.DateOfTransfer.ToShortDateString())
                                        toTd (sprintf "£%s" (commaSeparate row.Price)) ]
                        ]
                    ]
                    nav [] [
                        ul [ ClassName "pagination" ] [
                            let buildPager enabled content current page =
                                li [ ClassName ("page-item" + (if enabled then "" else " disabled") + (if current then " active" else "")) ] [
                                    button [ ClassName "page-link"; Style [ Cursor "pointer" ]; OnClick (fun _ -> dispatch (ChangePage page)) ] [ str content ]
                                ]
                            let currentPage = response.Page
                            let totalPages = int ((response.TotalTransactions |> Option.defaultValue 0 |> float) / 20.)
                            yield buildPager (currentPage > 0) "Previous" false (currentPage - 1)
                            yield!
                                [ for page in 0 .. totalPages ->
                                    buildPager true (string (page + 1)) (page = currentPage) page ]
                            yield buildPager (currentPage < totalPages) "Next" false (currentPage + 1)
                        ]
                    ]
                ]
            ]
    ]

let view model dispatch =
    let progressBarVisibility = match model.Status with | Searching -> "visible" | Displaying -> "invisible"
    div [ ClassName "col" ] [
        yield Details.view model.Selected
        yield div [ ClassName "border rounded m-3 p-3 bg-light" ] [
            div [ ClassName "form-group" ] [
                label [ HtmlFor "searchValue" ] [ str "Search for" ]
                input [
                    ClassName "form-control"
                    Id "searchValue"
                    Placeholder "Enter Search"
                    OnChange (fun ev -> dispatch (SetSearch !!ev.target?value))
                    Client.Style.onEnter (DoSearch model.Text) dispatch
                ]
            ]
            div [ ClassName "form-group" ] [
                div [ ClassName "progress" ] [
                    div [ ClassName (sprintf "progress-bar progress-bar-striped progress-bar-animated %s" progressBarVisibility)
                          Role "progressbar"
                          Style [ Width "100%" ] ]
                        [ str <| sprintf "Searching for '%s'..." model.LastSearch.Description ]
                ]
            ]
            button [ ClassName "btn btn-primary"; OnClick (fun _ -> dispatch (DoSearch model.Text)) ] [ str "Search!" ] ]
        yield viewResults model.SearchResults dispatch ]

let findTransactions (text, filter, page) =
    let filter = filter |> Option.map(fun (facet:string, value) -> sprintf "?%s=%s" (facet.Replace(" ", "")) value) |> Option.defaultValue ""
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/find/%s/%d%s" text page filter) []

let findByPostcode (postCode, page) =
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/%s/1/%d" postCode page) []

let update msg model : Model * Cmd<Msg> =
    let initiateSearch model term facet page =
        let cmd =
            match term with
            | Term text -> Cmd.ofPromise findTransactions (text, facet, page)
            | Postcode postcode -> Cmd.ofPromise findByPostcode (postcode, page)
        let cmd = cmd (fun response -> SearchCompleted(term, response)) SearchError
        { model with
            Status = Searching
            LastSearch = term
            Parameters = { Facet = facet; Page = page } }, cmd
    match msg with
    | SetSearch text -> { model with Text = Term text }, Cmd.none
    | DoSearch (Term text) when System.String.IsNullOrWhiteSpace text || text.Length <= 3 -> model, Cmd.none
    | DoSearch term -> initiateSearch model term None 0
    | SetFilter (facet, value) -> initiateSearch model model.LastSearch (Some(facet, value)) model.Parameters.Page
    | ChangePage page -> initiateSearch model model.LastSearch model.Parameters.Facet page
    | SearchCompleted (term, response) ->
        { model with
            Status = Displaying
            SearchResults = Some { SearchTerm = term; Response = response }
            Selected = None }, Cmd.none
    | SearchError _ -> model, Cmd.none
    | SelectTransaction transaction -> { model with Selected = Some transaction }, Cmd.none