module Pages.SearchResults

open PropertyMapper.Contracts
open Fable.Helpers.React
open Fable.Helpers.React.Props

type Model = { SearchTerm : SearchTerm; Response : SearchResponse }
type Msg = Filter of (string * string)

let init _ : Model option = None
let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]

    let toFilterCard dispatch facet expanded values =
        div [ ClassName "card" ] [
            div [ ClassName "card-header"; Role "tab"; Id (sprintf "heading-%s" facet) ] [
                h5 [ ClassName "mb-0" ] [
                    a [ DataToggle "collapse"; Href (sprintf "#collapse-%s" facet); AriaExpanded expanded ] [ str facet ]
                ]
            ]
            div [ Id (sprintf "collapse-%s" facet); Role "tabpanel"; ClassName (if expanded then "collapse show" else "collapse collapsed") ] [
                div [ ClassName "card-body" ] [
                    div [ ClassName "list-group" ]
                        [ for value in values ->
                            a [ ClassName ("list-group-item list-group-item-action")
                                OnClick (fun _ -> dispatch (Filter(facet, value))) ]
                              [ str value ] ]
                ]
            ]
        ]
    
    div [ ClassName "container-fluid border rounded m-3 p-3 bg-light" ] [
        match model with
        | None -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Please perform a search!" ] ] ]
        | Some { Model.Response = { Results = [||] } } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Your search yielded no results." ] ] ]
        | Some model ->
            let (SearchTerm text) = model.SearchTerm
            let hits = model.Response.TotalTransactions |> Option.map (sprintf "(%d hits).") |> Option.defaultValue ""
            yield div [ ClassName "row" ] [
                div [ ClassName "col-2" ] [
                    div [ ClassName "container" ] [
                        div [ ClassName "row" ] [ h4 [] [ str "Filters" ] ]
                        div [ ClassName "row" ] [
                            div [ Id "accordion"; Role "tablist" ] [
                                toFilterCard dispatch "Counties" true model.Response.Facets.Counties
                                toFilterCard dispatch "Towns" false model.Response.Facets.Towns
                                toFilterCard dispatch "Districts" false model.Response.Facets.Districts
                                toFilterCard dispatch "Localities" false model.Response.Facets.Localities
                            ]
                        ]
                    ]
                ]
                div [ ClassName "col-10" ] [
                    div [ ClassName "row" ] [ div [ ClassName "col" ] [ h4 [] [ str <| sprintf "Search results for '%s' %s." text hits ] ] ]
                    table [ ClassName "table table-bordered table-hover" ] [
                        thead [] [
                            tr [] [ toTh "Street"
                                    toTh "Town"
                                    toTh "County"
                                    toTh "Postcode"
                                    toTh "Date"
                                    toTh "Price" ]
                        ]
                        tbody [] [
                            for row in model.Response.Results ->
                                tr [] [ toTd (row.Address.Building + " " + row.Address.Street)
                                        toTd row.Address.TownCity
                                        toTd row.Address.County
                                        toTd row.Address.PostCode
                                        toTd (row.DateOfTransfer.ToShortDateString())
                                        toTd (string row.Price) ]
                        ]                
                    ]
                ]
            ]
    ]