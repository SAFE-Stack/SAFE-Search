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

let searchProperties config (postCode:string, distance, page) next (ctx:HttpContext) = task {
    let! properties =
        findByPostcode config
            { Filter = ctx.BindQueryString<PropertyFilterRaw>() |> ofRawFilter
              Postcode = postCode.ToUpper()
              MaxDistance = distance
              Page = page }
    return! FableJson.serialize properties next ctx }

let genericSearch config (text, page) next (ctx:HttpContext) =
    let request =
        { Page = page
          Text = text
          Filter = ctx.BindQueryString<PropertyFilterRaw>() |> ofRawFilter }
    task {
        let! properties = request |> findGeneric config
        return! FableJson.serialize properties next ctx }
let webApp config : HttpHandler =
    choose [
        GET >=>
            choose [
                routef "/property/find/%s/%i" (genericSearch config)
                routef "/property/%s/%i/%i" (searchProperties config)
                routef "/property/%s/%i" (fun (postcode, distance) -> searchProperties config (postcode, distance, 0))
                route "/" >=> razorHtmlView "Index" { Text = "Hello world, from Giraffe!" }
            ]
        setStatusCode 404 >=> text "Not Found" ]

