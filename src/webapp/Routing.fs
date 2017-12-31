module PropertyMapper.Routing

open Search
open Giraffe
open Microsoft.AspNetCore.Http
open PropertyMapper.Contracts

let searchProperties config (postCode:string, distance, page) next (ctx:HttpContext) = task {
    let! properties =
        findByPostcode config
            { Filter = ctx.BindQueryString<PropertyFilter>()
              Postcode = postCode.ToUpper()
              MaxDistance = distance
              Page = page }
    return! FableJson.serialize properties next ctx }

let genericSearch config (text, page) next (ctx:HttpContext) =
    let request =
        { Page = page
          Text = text
          Filter = ctx.BindQueryString<PropertyFilter>() }
    task {
        let! properties = request |> InMemorySearch.findGeneric
        return! FableJson.serialize properties next ctx }
let webApp config : HttpHandler =
    choose [
        GET >=>
            choose [
                routef "/property/find/%s/%i" (genericSearch config)
                routef "/property/%s/%i/%i" (searchProperties config)
                routef "/property/%s/%i" (fun (postcode, distance) -> searchProperties config (postcode, distance, 0))
            ]
        setStatusCode 404 >=> text "Not Found" ]

