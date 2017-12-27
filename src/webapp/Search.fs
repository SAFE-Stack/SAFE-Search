module PropertyMapper.Search

open Giraffe.Tasks
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models
open Microsoft.Spatial
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

type PropertyFilter =
  { Towns : string list
    Localities : string list
    Districts : string list
    Counties : string list
    MaxPrice : int option
    MinPrice : int option }


type FindNearestRequest = { Postcode : string; MaxDistance : int; Page : int; Filter : PropertyFilter }
type FindGenericRequest = { Text : string; Page : int; Filter : PropertyFilter }

[<AutoOpen>]
module Management =
    let propertiesIndex config =
        let (ConnectionString c) = config.AzureSearch
        (new SearchServiceClient(config.AzureSearchServiceName, SearchCredentials c)).Indexes.GetClient "properties"

[<AutoOpen>]
module AzureSearch =
    open System.Collections.Generic
    let findByDistance (geo:GeographyPoint) maxDistance =
        SearchParameters(Filter=sprintf "geo.distance(Geo, geography'POINT(%f %f)') le %d" geo.Longitude geo.Latitude maxDistance)
    let withFilter field (values:string list) (parameters:SearchParameters) =
        parameters.Filter <-
            [ (match parameters.Filter with f when String.IsNullOrWhiteSpace f -> None | f -> Some f)
              (match values with
               | [] -> None
               | values ->
                   [ for value in values -> sprintf "(%s eq '%s')" field (value.ToUpper()) ]
                   |> String.concat " or "
                   |> sprintf "(%s)"
                   |> Some)
            ]
            |> List.choose id
            |> String.concat " and "
        parameters

    let doSearch config page searchText (parameters:SearchParameters) = task {
        parameters.Facets <- ResizeArray [ "TownCity"; "Locality"; "District"; "County"; "Price" ]
        parameters.Skip <- Nullable(page * 50)
        parameters.IncludeTotalResultCount <- true
        let! searchResult = (propertiesIndex config).Documents.SearchAsync<SearchableProperty>(searchText, parameters)
        let facets =
            searchResult.Facets
            |> Seq.map(fun x -> x.Key, x.Value |> Seq.map(fun r -> r.Value |> string) |> Seq.toList)
            |> Map.ofSeq
            |> fun x -> x.TryFind >> Option.defaultValue []
        return facets, searchResult.Results |> Seq.toArray |> Array.map(fun r -> r.Document), searchResult.Count |> Option.ofNullable |> Option.map int }

let private toFindPropertiesResponse findFacet count results =      
    { Results =
        results
        |> Array.map(fun result ->
             { BuildDetails =
                 { PropertyType = result.PropertyType |> function "D" -> "Detached" | "S" -> "Semi-Detached" | "T" -> "Terraced" | "F" -> "Flats/Maisonettes" | _ -> "Other"
                   OldNew = result.OldNew |> function "Y" -> "New Build" | _ -> "Old Build"
                   Duration = result.Duration |> function "F" -> "Freehold" | _ -> "Leasehold" }
               Address =
                 { Building = [ result.Paon; result.Saon ] |> List.choose Option.ofObj |> String.concat ", "
                   Street = result.Street
                   Locality = result.Locality
                   TownCity = result.TownCity
                   District = result.District
                   County = result.County
                   PostCode = result.PostCode }
               Price = result.Price
               DateOfTransfer = result.DateOfTransfer })
      TotalTransactions = count
      Facets = 
        { Towns = findFacet "TownCity"
          Localities = findFacet "Locality"
          Districts = findFacet "District"
          Counties = findFacet "County"
          Prices = findFacet "Price" } }

let findGeneric config request = task {
    let! findFacet, searchResults, count =
        SearchParameters()
        |> withFilter "TownCity" request.Filter.Towns
        |> withFilter "County" request.Filter.Counties
        |> withFilter "Locality" request.Filter.Localities
        |> withFilter "District" request.Filter.Districts
        |> doSearch config request.Page request.Text
    return searchResults |> toFindPropertiesResponse findFacet count }
        
let findByPostcode config request = task {
    let! geo =
        match request.Postcode.Split ' ' with
        | [| partA; partB |] -> AzureStorage.tryGetGeo config partA partB
        | _ -> Task.FromResult None
    let! findFacet, searchResults, count =
        match geo with
        | Some geo -> task {
            let geo = GeographyPoint.Create(geo.Lat, geo.Long)
            return!
                findByDistance geo request.MaxDistance
                |> withFilter "TownCity" request.Filter.Towns
                |> withFilter "County" request.Filter.Counties
                |> withFilter "Locality" request.Filter.Localities
                |> withFilter "District" request.Filter.Districts
                |> doSearch config request.Page "" }
        | None -> Task.FromResult ((fun _ -> []), [||], None)
    return searchResults |> toFindPropertiesResponse findFacet count }
