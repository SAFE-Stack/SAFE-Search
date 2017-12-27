module PropertyMapper.Routing

open Search
open Giraffe
open Giraffe.Razor
open Microsoft.AspNetCore.Http
open PropertyMapper.Contracts
open PropertyMapper.Models


let ofRawFilter (filter:PropertyFilterRaw) : PropertyFilter =
    let ofString = Option.ofObj >> Option.map(fun (s:string) -> s.Split ',' |> Array.toList)  >> Option.defaultValue []
    { Towns = filter.Towns |> ofString
      Localities = filter.Localities |> ofString
      Districts = filter.Districts |> ofString
      Counties = filter.Counties |> ofString
      MaxPrice = filter.MaxPrice
      MinPrice = filter.MinPrice }

let searchProperties config (pcodeA:string, pcodeB:string, distance, page) next (ctx:HttpContext) = task {
    let! properties =
        findByPostcode config
            { Filter = ctx.BindQueryString<PropertyFilterRaw>() |> ofRawFilter
              Postcode = sprintf "%s %s" (pcodeA.ToUpper()) (pcodeB.ToUpper())
              MaxDistance = distance
              Page = page }
    return! FableJson.serialize properties next ctx }

let genericSearch config text next (ctx:HttpContext) =
    let request =
        { Page = 0
          Text = text
          Filter = ctx.BindQueryString<PropertyFilterRaw>() |> ofRawFilter }
    task {
        let! properties = request |> findGeneric config
        return! FableJson.serialize properties next ctx }
let webApp config : HttpHandler =
    choose [
        GET >=>
            choose [
                routef "/property/%s/%s/%i" (fun (pcodeA, pcodeB, distance) -> searchProperties config (pcodeA,pcodeB,distance,0))
                routef "/property/%s/%s/%i/%i" (searchProperties config)
                routef "/property/find/%s" (genericSearch config)
                route "/" >=> razorHtmlView "Index" { Text = "Hello world, from Giraffe!" }
            ]
        setStatusCode 404 >=> text "Not Found" ]

