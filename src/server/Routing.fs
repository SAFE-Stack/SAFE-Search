module PropertyMapper.Routing

module Adapters =
    open Microsoft.AspNetCore.Http
    open Giraffe
    open PropertyMapper.Search
    open PropertyMapper.Contracts
    let searchProperties (searcher:Search.ISearchEngine) (postCode:string, distance, page) next (ctx:HttpContext) = task {
        let! properties =
            searcher.PostcodeSearch
                { Filter = ctx.BindQueryString<PropertyFilter>()
                  Postcode = postCode.ToUpper()
                  MaxDistance = distance
                  Page = page }
        return! FableJson.serialize properties next ctx }

    let genericSearch (searcher:Search.ISearchEngine) (text, page) next (ctx:HttpContext) = task {
        let! properties =
            let request =
                { Page = page
                  Text = if System.String.IsNullOrWhiteSpace text then None else Some text
                  Filter = ctx.BindQueryString<PropertyFilter>() }
            searcher.GenericSearch request
        return! FableJson.serialize properties next ctx }

open Saturn.Router
let propertyRouter searcher = scope {
    getf "/find/%s/%i" (Adapters.genericSearch searcher)
    getf "/%s/%i/%i" (Adapters.searchProperties searcher)
    getf "/%s/%i" (fun (postcode, distance) -> Adapters.searchProperties searcher (postcode, distance, 0)) }
