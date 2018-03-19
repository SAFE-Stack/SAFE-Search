module PropertyMapper.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.Razor
open PropertyMapper.Routing

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader() |> ignore

let createSearch config =
    match config with
    | _ when String.IsNullOrWhiteSpace config.AzureSearchServiceName ->
        { new Search.ISearch with
            member __.GenericSearch request = Search.InMemory.findGeneric request
            member __.PostcodeSearch request = Search.InMemory.findByPostcode request }
    | config ->
        { new Search.ISearch with
            member __.GenericSearch request = Search.Azure.findGeneric config request
            member __.PostcodeSearch request = Search.Azure.findByPostcode config AzureStorage.tryGetGeo request }

let configureApp searcher (app : IApplicationBuilder) =
    app.UseCors(configureCors)
       .UseGiraffeErrorHandler(errorHandler)
       .UseStaticFiles()
       .UseGiraffe(webApp searcher)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")
    services.AddRazorEngine viewsFolderPath |> ignore
    services.AddCors() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

let appConfig =
    lazy
        let builder =
            ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional = true)
                .AddEnvironmentVariables()
                .Build()

        { AzureStorage = builder.GetConnectionString "AzureStorage" |> ConnectionString
          AzureSearch = builder.GetConnectionString "AzureSearch" |> ConnectionString
          AzureSearchServiceName = builder.["AzureSearchName"] }

[<EntryPoint>]
let main _ =
    let contentRoot  = Directory.GetCurrentDirectory()
    let webRoot      = Path.Combine(contentRoot, "WebRoot")
    let configureApp = appConfig.Value |> createSearch |> configureApp

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0