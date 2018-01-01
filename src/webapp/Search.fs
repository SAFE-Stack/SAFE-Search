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

type FindNearestRequest = { Postcode : string; MaxDistance : int; Page : int; Filter : PropertyFilter }
type FindGenericRequest = { Text : string option; Page : int; Filter : PropertyFilter }

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
    let withFilter (parameters:SearchParameters) (field, value:string option) =
        parameters.Filter <-
            [ (match parameters.Filter with f when String.IsNullOrWhiteSpace f -> None | f -> Some f)
              (value |> Option.map(fun value -> sprintf "(%s eq '%s')" field (value.ToUpper()))) ]
            |> List.choose id
            |> String.concat " and "
        parameters

    let doSearch config page searchText (parameters:SearchParameters) = task {
        parameters.Facets <- ResizeArray [ "TownCity"; "Locality"; "District"; "County"; "Price" ]
        parameters.Skip <- Nullable(page * 20)
        parameters.Top <- Nullable 20
        parameters.IncludeTotalResultCount <- true
        let! searchResult = (propertiesIndex config).Documents.SearchAsync<SearchableProperty>(searchText |> Option.defaultValue "", parameters)
        let facets =
            searchResult.Facets
            |> Seq.map(fun x -> x.Key, x.Value |> Seq.map(fun r -> r.Value |> string) |> Seq.toList)
            |> Map.ofSeq
            |> fun x -> x.TryFind >> Option.defaultValue []
        return facets, searchResult.Results |> Seq.toArray |> Array.map(fun r -> r.Document), searchResult.Count |> Option.ofNullable |> Option.map int }

let private toFindPropertiesResponse findFacet count page results =      
    { Results =
        results
        |> Array.map(fun result ->
             { BuildDetails =
                 { PropertyType = result.PropertyType |> PropertyType.Parse
                   Build = result.OldNew |> BuildType.Parse
                   Contract = result.Duration |> ContractType.Parse }
               Address =
                 { Building = [ result.Paon; result.Saon ] |> List.choose Option.ofObj |> String.concat ", "
                   Street = result.Street |> Option.ofObj
                   Locality = result.Locality |> Option.ofObj
                   TownCity = result.TownCity
                   District = result.District
                   County = result.County
                   PostCode = result.PostCode |> Option.ofObj }
               Price = result.Price
               DateOfTransfer = result.DateOfTransfer })
      TotalTransactions = count
      Facets = 
        { Towns = findFacet "TownCity"
          Localities = findFacet "Locality"
          Districts = findFacet "District"
          Counties = findFacet "County"
          Prices = findFacet "Price" }
      Page = page }

let applyFilters filter parameters = 
    [ "TownCity", filter.Town
      "County", filter.County
      "Locality", filter.Locality
      "District", filter.District ]
    |> List.fold withFilter parameters

let findGeneric config request = task {
    let! findFacet, searchResults, count =
        SearchParameters()
        |> applyFilters request.Filter
        |> doSearch config request.Page request.Text
    return searchResults |> toFindPropertiesResponse findFacet count request.Page }
        
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
                |> applyFilters request.Filter
                |> doSearch config request.Page None }
        | None -> Task.FromResult ((fun _ -> []), [||], None)
    return searchResults |> toFindPropertiesResponse findFacet count request.Page }
