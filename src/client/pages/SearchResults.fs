module Pages.SearchResults

open PropertyMapper.Contracts
open Fable.Helpers.React
open Fable.Helpers.React.Props

type Model = { SearchTerm : SearchTerm; TotalResults : int option; Results : PropertyResult array }

let init _ : Model option = None
let view model =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toTd c = td [ Scope "row" ] [ str c ]
    
    div [ ClassName "container" ] [
        match model with
        | Some { Model.Results = [||] } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Your search yielded no results." ] ] ]
        | Some model ->
            let (SearchTerm text) = model.SearchTerm
            yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str <| sprintf "Search Results for '%s' " text ] ] ]

            match model.TotalResults with
            | Some count -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h6 [] [ str <| sprintf "Found %d possible matches." count ] ] ]
            | None -> ()

            yield div [ ClassName "row" ] [            
                div [ ClassName "col" ] [
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
                            for row in model.Results ->
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
        | None -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Please perform a search!" ] ] ]
    ]
