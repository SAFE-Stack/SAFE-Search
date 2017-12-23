module client

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.PowerPack
open Fable.Import.Browser

promise {
    let! resp = Fetch.fetchAs<PropertyMapper.Contracts.FindPropertiesResponse> "http://localhost:5000/property/wd6/1fy/1" []
    let table = document.getElementById "results"
    for result in resp.Results do
        let tr = document.createElement "tr"
        let td = document.createElement "td"
        td.textContent <- result.Address.Street
        tr.appendChild td |> ignore
        table.appendChild tr |> ignore
} |> Promise.start