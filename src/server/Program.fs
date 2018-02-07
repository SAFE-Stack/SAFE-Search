module PropertyMapper.App

open System
open System.IO
open Microsoft.Extensions.Configuration
open Giraffe

module Config =
    let createSearchEngine() =
        let appConfig =
            let builder =
                ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional = true)
                    .AddEnvironmentVariables()
                    .Build()
            { AzureStorage = builder.GetConnectionString "AzureStorage" |> ConnectionString
              AzureSearch = builder.GetConnectionString "AzureSearch" |> ConnectionString
              AzureSearchServiceName = builder.["AzureSearchName"] }

        match appConfig with
        | appConfig when String.IsNullOrWhiteSpace appConfig.AzureSearchServiceName ->
            { new Search.ISearchEngine with
                member __.GenericSearch request = Search.InMemory.findGeneric request
                member __.PostcodeSearch request = Search.InMemory.findByPostcode request }
        | appConfig ->
            { new Search.ISearchEngine with
                member __.GenericSearch request = Search.Azure.findGeneric appConfig request
                member __.PostcodeSearch request = Search.Azure.findByPostcode appConfig AzureStorage.tryGetGeo request }        

open Saturn.Application
open Saturn.Router

let topRouter =
    let searcher = Config.createSearchEngine()
    scope {
        error_handler (text "404")
        forward "/property" (Routing.propertyRouter searcher) }

let app = application {
    url "http://localhost:5000"
    memory_cache
    use_static (Directory.GetCurrentDirectory())
    use_gzip
    use_cors "*" (fun builder -> builder.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader() |> ignore)
    router topRouter }

[<EntryPoint>]
let main _ =
    run app
    0