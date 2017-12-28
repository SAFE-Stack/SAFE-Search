module Pages.Results

open PropertyMapper.Contracts
open Fable.Helpers.React
open Fable.Helpers.React.Props

type Model = { SearchTerm : SearchTerm option; Response : SearchResponse option }

type Msg =
    | FilterSet of facet:string * value: string
    | DisplayResults of SearchTerm * SearchResponse
let init _ : Model = { SearchTerm = None; Response = None }
let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]
    
    div [ ClassName "container-fluid border rounded m-3 p-3 bg-light" ] [
        match model with
        | { SearchTerm = None } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Please perform a search!" ] ] ]
        | { Model.Response = (Some { Results = [||] } | None) } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Your search yielded no results." ] ] ]
        | { SearchTerm = Some (SearchTerm text); Response = Some response } ->
            let hits = response.TotalTransactions |> Option.map (sprintf "(%d hits).") |> Option.defaultValue ""
            yield div [ ClassName "row" ] [
                div [ ClassName "col-2" ] [ Pages.Filter.createFilters (FilterSet >> dispatch) response.Facets ]
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
                            for row in response.Results ->
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

let update msg model : Model =
    match msg with
    | FilterSet _ -> model
    | DisplayResults (term, response) -> { SearchTerm = Some term; Response = Some response }