module Pages.Search

open Elmish
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open PropertyMapper.Contracts

type SearchState = Searching | Displaying

type SearchParameters = { Facet : (string * string) option; Page : int; Sort : (PropertyTableColumn * SortDirection) option }
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
    | SetSort of (PropertyTableColumn * SortDirection) option
    | ChangePage of int
    | SearchCompleted of SearchTerm * SearchResponse
    | SearchError of exn
    | SelectTransaction of PropertyResult

let init _ =
    { Text = SearchTerm.Empty
      LastSearch = SearchTerm.Empty
      Status = SearchState.Displaying
      Parameters = { Facet = None; Page = 0; Sort = None }
      SearchResults = None
      Selected = None }

let viewResults searchResults currentSort dispatch =
    let toTh col =
        let nextSortDir, currentSortDisplay =
            match currentSort with
            | Some (sortCol, sortDirection) when sortCol = col ->
                match sortDirection with
                | Ascending -> Some Descending, " ▲"
                | Descending -> None, " ▼"
            | _ -> Some Ascending, ""
        let nextSort = nextSortDir |> Option.map (fun sortDir -> col, sortDir)
        th [ Scope "col"; OnClick(fun _ -> dispatch (SetSort nextSort)) ] [ str (string col + currentSortDisplay) ]
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
                | PostcodeSearch postcode -> sprintf "Showing properties within a 1km radius of '%s'%s." postcode hits

            yield div [ ClassName "row" ] [
                div [ ClassName "col-2" ] [ Filter.createFilters (SetFilter >> dispatch) response.Facets ]
                div [ ClassName "col-10" ] [
                    div [ ClassName "row" ] [ div [ ClassName "col" ] [ h4 [] [ str description ] ] ]
                    table [ ClassName "table table-bordered table-hover" ] [
                        thead [] [
                            tr [] [ toTh Street
                                    toTh Town
                                    toTh Postcode
                                    toTh Date
                                    toTh Price ]
                        ]
                        tbody [] [
                            for row in response.Results ->
                                let postcodeLink =
                                    a
                                        [ Href "#"; OnClick(fun _ -> row.Address.PostCode |> Option.iter(PostcodeSearch >> DoSearch >> dispatch)) ]
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
        yield viewResults model.SearchResults model.Parameters.Sort dispatch ]

let queryString parameters =
    [ match parameters.Facet with Some f -> yield f | None -> ()
      match parameters.Sort with
      | Some (col, dir) -> yield! [ ("SortColumn", (string col)); ("SortDirection", string dir) ]
      | None -> () ]
    |> List.map (fun (key:string, value) -> sprintf "%s=%s" key value)
    |> String.concat "&"
    |> function "" -> "" | s -> "?" + s

let findTransactions (text, parameters) =
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/find/%s/%d%s" text parameters.Page (queryString parameters)) []

let findByPostcode (postCode, page, parameters) =
    Fetch.fetchAs<SearchResponse> (sprintf "http://localhost:5000/property/%s/1/%d%s" postCode page (queryString parameters)) []

let update msg model : Model * Cmd<Msg> =
    let initiateSearch model term parameters =
        let cmd =
            match term with
            | Term text -> Cmd.ofPromise findTransactions (text, parameters)
            | PostcodeSearch postcode -> Cmd.ofPromise findByPostcode (postcode, parameters.Page, parameters)
        let cmd = cmd (fun response -> SearchCompleted(term, response)) SearchError
        { model with Status = Searching; LastSearch = term; Parameters = parameters }, cmd
    match msg with
    | SetSearch text -> { model with Text = Term text }, Cmd.none
    | DoSearch (Term text) when System.String.IsNullOrWhiteSpace text || text.Length <= 3 -> model, Cmd.none
    | DoSearch term -> initiateSearch model term { Facet = None; Page = 0; Sort = None }
    | SetFilter (facet, value) -> initiateSearch model model.LastSearch { model.Parameters with Facet = Some(facet, value) }
    | SetSort sort -> initiateSearch model model.LastSearch { model.Parameters with Sort = sort }
    | ChangePage page -> initiateSearch model model.LastSearch { model.Parameters with Page = page }
    | SearchCompleted (term, response) ->
        { model with
            Status = Displaying
            SearchResults = Some { SearchTerm = term; Response = response }
            Selected = None }, Cmd.none
    | SearchError _ -> model, Cmd.none
    | SelectTransaction transaction -> { model with Selected = Some transaction }, Cmd.none