module client

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.PowerPack
open Fable.Import.Browser

let init() =
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

    let canvas = Browser.document.getElementsByTagName_canvas().[0]
    canvas.width <- 1000.
    canvas.height <- 800.
    let ctx = canvas.getContext_2d()
    // The (!^) operator checks and casts a value to an Erased Union type
    // See http://fable.io/docs/interacting.html#Erase-attribute
    ctx.fillStyle <- !^"rgb(200,0,0)"
    ctx.fillRect (10., 10., 55., 50.)
    ctx.fillStyle <- !^"rgba(0, 0, 200, 0.5)"
    ctx.fillRect (30., 30., 55., 50.)

init()