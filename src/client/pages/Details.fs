module Pages.Details

open Fable.Helpers.React
open Fable.Helpers.React.Props
open PropertyMapper.Contracts
open Fable

let view txn =
    let field name value =
        value
        |> Option.map(fun v ->
            div [ ClassName "form-group row" ] [
                label [ HtmlFor name; ClassName "col-sm-4 col-form-label" ] [ str name ]
                div [ ClassName "col-sm-8" ] [ input [ Type "text"; ClassName "form-control-plaintext"; Id name; Value v ] ]
            ])
        |> Option.defaultValue (div [] [])

    div [ ClassName "modal fade"; Id "exampleModal"; Role "dialog"; unbox ("aria-labelledby", "exampleModalLabel"); unbox ("aria-hidden", "true") ] [
        div [ ClassName "modal-dialog"; Role "document" ] [
            div [ ClassName "modal-content" ] [
                div [ ClassName "modal-header" ] [
                    h5 [ ClassName "modal-title"; Id "exampleModalLabel" ] [ str (sprintf "%s" (txn.Address.Building + " " + (txn.Address.Street |> Option.defaultValue ""))) ]
                    button [ Type "button"; ClassName "close"; unbox ("data-dismiss", "modal"); unbox ("aria-label", "Close") ] [
                        span [ unbox ("aria-hidden", true) ] [ str "x" ]
                    ]
                ]
                div [ ClassName "modal-body" ] [
                    field "Building / Street" (Some (txn.Address.Building + " " + (txn.Address.Street |> Option.defaultValue "")))
                    field "Town" (Some txn.Address.TownCity)
                    field "District" (Some txn.Address.District)
                    field "County" (Some txn.Address.County)
                    field "Locality" txn.Address.Locality
                    field "Post Code" txn.Address.PostCode
                    field "Price" (Some(sprintf "£%s" (txn.Price |> commaSeparate)))
                    field "Date of Transfer" (Some (txn.DateOfTransfer.ToShortDateString()))
                    field "Property Type" (txn.BuildDetails.PropertyType |> Option.map(fun pt -> pt.Description))
                    field "Build Type" (Some (string txn.BuildDetails.Build))
                    field "Contract" (Some (string txn.BuildDetails.Contract))
                ]
                div [ ClassName "modal-footer" ] [
                    button [ Type "button"; ClassName "btn btn-primary"; unbox ("data-dismiss", "modal") ] [ str "Close" ]
                ]
            ]
        ]
    ]