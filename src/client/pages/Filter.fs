module Pages.Filter

open Fable.Helpers.React
open Fable.Helpers.React.Props
open PropertyMapper.Contracts

let toFilterCard dispatch facet values =
    match values with
    | [] -> div [] []
    | values ->
        div [ ClassName "card" ] [
            div [ ClassName "card-header"; Role "tab"; Id (sprintf "heading-%s" facet) ] [
                h5 [ ClassName "mb-0" ] [
                    a [ DataToggle "collapse"; Href (sprintf "#collapse-%s" facet); AriaExpanded true ] [ str facet ]
                ]
            ]
            div [ Id (sprintf "collapse-%s" facet); Role "tabpanel"; ClassName "collapse show" ] [
                div [ ClassName "card-body" ] [
                    div [ ClassName "list-group" ]
                        [ for value in values ->
                            button [
                                ClassName ("list-group-item list-group-item-action")
                                OnClick (fun _ -> dispatch (facet, value)) ]
                              [ str value ] ]
                ]
            ]
        ]

let createFilters dispatch facets =
    div [ ClassName "container" ] [
        div [ ClassName "row" ] [ h4 [] [ str "Filters" ] ]
        div [ ClassName "row" ] [
            div [ Id "accordion"; Role "tablist" ] [
                toFilterCard dispatch "County" facets.Counties
                toFilterCard dispatch "District" facets.Districts
                toFilterCard dispatch "Town" facets.Towns
                toFilterCard dispatch "Locality" facets.Localities
                toFilterCard dispatch "Price Range" facets.PriceRanges
            ]
        ]
    ]