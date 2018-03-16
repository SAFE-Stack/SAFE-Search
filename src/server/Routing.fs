module PropertyMapper.Routing

open PropertyMapper.Search
open PropertyMapper.Contracts
open Giraffe
open Microsoft.AspNetCore.Http

let searchProperties (searcher:Search.ISearch) (postCode:string, distance, page) next (ctx:HttpContext) = task {
    let! properties =
        searcher.PostcodeSearch
            { Filter = ctx.BindQueryString<PropertyFilter>()
              Postcode = postCode.ToUpper()
              MaxDistance = distance
              Page = page }
    return! FableJson.serialize properties next ctx }

let genericSearch (searcher:Search.ISearch) (text, page) next (ctx:HttpContext) =
    let request =
        { Page = page
          Text = if System.String.IsNullOrWhiteSpace text then None else Some text
          Filter = ctx.BindQueryString<PropertyFilter>()
          Sort =
            { SortColumn = ctx.TryGetQueryStringValue "SortColumn"
              SortDirection = ctx.TryGetQueryStringValue "SortDirection" |> Option.bind SortDirection.TryParse } }
    task {
        let! properties = searcher.GenericSearch request
        return! FableJson.serialize properties next ctx }
let webApp searcher : HttpHandler =
    choose [
        GET >=>
            choose [
                routef "/property/find/%s/%i" (genericSearch searcher)
                routef "/property/%s/%i/%i" (searchProperties searcher)
                routef "/property/%s/%i" (fun (postcode, distance) -> searchProperties searcher (postcode, distance, 0))
            ]
        setStatusCode 404 >=> text "Not Found" ]

