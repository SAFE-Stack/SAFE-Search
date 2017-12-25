module PropertyMapper.GeoMapping

open Giraffe.Tasks
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models
open Microsoft.Spatial
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open PropertyMapper.Contracts
open System
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks

type SearchableProperty =
    { [<Key; IsFilterable>] TransactionId : string
      [<IsFacetable; IsSortable>] Price : int
      [<IsFilterable; IsSortable>] DateOfTransfer : DateTime
      [<IsFilterable>] PostCode : string
      [<IsFacetable; IsFilterable>] PropertyType : string
      [<IsFacetable; IsFilterable>] OldNew : string
      [<IsFacetable; IsFilterable>] Duration : string
      Paon : string
      Saon : string
      [<IsSearchable>] Street : string
      [<IsFacetable; IsFilterable; IsSearchable>] Locality : string
      [<IsFacetable; IsFilterable; IsSearchable>] TownCity : string
      [<IsFacetable; IsFilterable; IsSearchable>] District : string
      [<IsFacetable; IsFilterable; IsSearchable>] County : string
      [<IsFilterable>] Geo : GeographyPoint }

[<AutoOpen>]
module Management =
    let credentials = SearchCredentials ""
    let searchClient = new SearchServiceClient("", credentials)
    let propertiesIndex = searchClient.Indexes.GetClient "properties"

module AzureStorage =
    type Geo = { Lat : float; Long : float }
    let table =
        let tableClient =
            let connection = CloudStorageAccount.Parse @"" 
            connection.CreateCloudTableClient()
        tableClient.GetTableReference "postcodes"
    
    let tryGetGeo postcodeA postcodeB = task {
        let! result = TableOperation.Retrieve<DynamicTableEntity>(postcodeA, postcodeB) |> table.ExecuteAsync
        let tryGetProp name (entity:DynamicTableEntity) = entity.Properties.[name].DoubleValue |> Option.ofNullable
        return
            result.Result
            |> Option.ofObj
            |> Option.bind(function
                | :? DynamicTableEntity as entity ->
                    match entity |> tryGetProp "Lat", entity |> tryGetProp "Long" with
                    | Some lat, Some long -> Some { Lat = lat; Long = long }
                    | _ -> None
                | _ -> None) }

[<AutoOpen>]
module Searching =
    open System.Collections.Generic
    let findByDistance (geo:GeographyPoint) maxDistance =
        SearchParameters(Filter=sprintf "geo.distance(Geo, geography'POINT(%f %f)') le %d" geo.Longitude geo.Latitude maxDistance)
    let withFilter field (value:string) (parameters:SearchParameters) =
        parameters.Filter <-
            [ parameters.Filter; sprintf "%s eq '%s'" field (value.ToUpper()) ]
            |> List.choose Option.ofObj
            |> String.concat " and "
        parameters

    let doSearch page searchText (parameters:SearchParameters) = task {
        parameters.Facets <- ResizeArray [ "TownCity"; "Locality"; "District"; "County"; "Price" ]
        parameters.Skip <- Nullable(page * 50)
        let! searchResult = propertiesIndex.Documents.SearchAsync<SearchableProperty>(searchText, parameters)
        let facets =
            searchResult.Facets
            |> Seq.map(fun x -> x.Key, x.Value |> Seq.map(fun r -> r.Value |> string) |> Seq.toList)
            |> Map.ofSeq
            |> fun x -> x.TryFind >> Option.defaultValue []
        return facets, searchResult.Results |> Seq.toArray |> Array.map(fun r -> r.Document) }

type ValidatedPostcode = { PartA : string; PartB : string }
let validate (postcode:string) = 
    match postcode.Split ' ' with
    | [| a; b |] -> Some { PartA = a; PartB = b }
    | _ -> None
let toSearchResult (r:SearchableProperty) =
    { BuildDetails =
        { PropertyType = r.PropertyType |> function "D" -> "Detached" | "S" -> "Semi-Detached" | "T" -> "Terraced" | "F" -> "Flats/Maisonettes" | _ -> "Other"
          OldNew = r.OldNew |> function "Y" -> "New Build" | _ -> "Old Build"
          Duration = r.Duration |> function "F" -> "Freehold" | _ -> "Leasehold" }
      Address =
        { Building = [ r.Paon; r.Saon ] |> List.choose Option.ofObj |> String.concat ", "
          Street = r.Street
          Locality = r.Locality
          TownCity = r.TownCity
          District = r.District
          County = r.County
          PostCode = r.PostCode }
      Price = r.Price
      DateOfTransfer = r.DateOfTransfer }

let toResponse findFacet results =      
    { Results = results |> Array.map toSearchResult
      Facets = 
        { Towns = findFacet "TownCity"
          Localities = findFacet "Locality"
          Districts = findFacet "District"
          Counties = findFacet "County"
          Prices = findFacet "Price" } }
let defaultParameters = SearchParameters()

let findGeneric text page = task {
    let! findFacet, searchResults = doSearch page text defaultParameters
    return searchResults |> toResponse findFacet }
        
let findProperties request = task {
    let! geo =
        match validate request.Postcode with
        | Some postcode -> AzureStorage.tryGetGeo postcode.PartA postcode.PartB
        | None -> Task.FromResult None
    let! findFacet, searchResults =
        match geo with
        | Some geo -> task {
            let tryFilter field value = match value with | Some f -> withFilter field f | None -> id
            let geo = GeographyPoint.Create(geo.Lat, geo.Long)
            return!
                findByDistance geo request.Distance
                |> tryFilter "TownCity" request.Filter.Town
                |> tryFilter "County" request.Filter.County
                |> tryFilter "Locality" request.Filter.Locality
                |> tryFilter "District" request.Filter.District
                |> doSearch request.Page "" }
        | None -> Task.FromResult ((fun _ -> []), [||])
    return searchResults |> toResponse findFacet }
