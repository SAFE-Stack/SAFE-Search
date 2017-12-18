module PropertyMapper.Routing

open Contracts
open GeoMapping
open Giraffe
open Giraffe.Razor
open PropertyMapper.Models

let searchProperties (pcodeA:string, pcodeB:string, distance, page) next ctx = task {
    let! properties =
        findProperties
            { Postcode = sprintf "%s %s" (pcodeA.ToUpper()) (pcodeB.ToUpper())
              Distance = distance
              Page = page }
    return! json properties next ctx }
let webApp : HttpHandler =
    choose [
        GET >=>
            choose [
                routef "/property/%s/%s/%i/" (fun (pcodeA, pcodeB, distance) -> searchProperties(pcodeA,pcodeB,distance,0))
                routef "/property/%s/%s/%i/%i" searchProperties
                route "/" >=> razorHtmlView "Index" { Text = "Hello world, from Giraffe!" }
            ]
        setStatusCode 404 >=> text "Not Found" ]

