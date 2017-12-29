module Pages.Details

open Fable.Helpers.React
open Fable.Helpers.React.Props
open PropertyMapper.Contracts

let view txn =
    let makeField fieldId name value =
        value
        |> Option.ofObj
        |> Option.map(fun v ->
            div [ ClassName "form-group row" ] [
                label [ HtmlFor fieldId; ClassName "col-sm-4 col-form-label" ] [ str name ]
                div [ ClassName "col-sm-8" ] [ input [ Type "text"; ClassName "form-control-plaintext"; Id fieldId; Value v ] ]
            ])
        |> Option.defaultValue (div [] [])

    div [ ClassName "modal fade"; Id "exampleModal"; Role "dialog"; unbox ("aria-labelledby", "exampleModalLabel"); unbox ("aria-hidden", "true") ] [
        div [ ClassName "modal-dialog"; Role "document" ] [
            div [ ClassName "modal-content" ] [
                div [ ClassName "modal-header" ] [
                    h5 [ ClassName "modal-title"; Id "exampleModalLabel" ] [ str (sprintf "%s" (txn.Address.Building + " " + txn.Address.Street)) ]
                    button [ Type "button"; ClassName "close"; unbox ("data-dismiss", "modal"); unbox ("aria-label", "Close") ] [
                        span [ unbox ("aria-hidden", true) ] [ str "x" ]
                    ]
                ]
                div [ ClassName "modal-body" ] [
                    makeField "Address1" "Building / Street" (txn.Address.Building + " " + txn.Address.Street)
                    makeField "Town" "Town" txn.Address.TownCity
                    makeField "District" "District" txn.Address.District
                    makeField "County" "County" txn.Address.County
                    makeField "Locality" "Locality" txn.Address.Locality
                    makeField "PostCode" "Post Code" txn.Address.PostCode
                    makeField "Price" "Price" (sprintf "£%s" (txn.Price |> commaSeparate))
                    makeField "TransferDate" "Date of Transfer" (txn.DateOfTransfer.ToShortDateString())
                    makeField "PropertyType" "Property Type" (sprintf "%O" txn.BuildDetails.PropertyType)
                    makeField "OldNew" "Build Type" (sprintf "%O" txn.BuildDetails.OldNew)
                    makeField "Duration" "Contract" (sprintf "%O" txn.BuildDetails.Duration)
                ]
                div [ ClassName "modal-footer" ] [
                    button [ Type "button"; ClassName "btn btn-primary"; unbox ("data-dismiss", "modal") ] [ str "Close" ]
                ]
            ]
        ]
    ]