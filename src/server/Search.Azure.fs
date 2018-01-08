module PropertyMapper.Search.Azure

open Giraffe.Tasks
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models
open Microsoft.Spatial
open PropertyMapper
open PropertyMapper.Contracts
open System
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks

type SearchableProperty =
    { [<Key; IsFilterable>] TransactionId : string
      [<IsFacetable; IsSortable>] Price : int
      [<IsFilterable; IsSortable>] DateOfTransfer : DateTime
      PostCode : string
      [<IsFacetable; IsFilterable>] PropertyType : string
      [<IsFacetable; IsFilterable>] Build : string
      [<IsFacetable; IsFilterable>] Contract : string
      Building : string
      [<IsSearchable>] Street : string
      [<IsFacetable; IsFilterable; IsSearchable>] Locality : string
      [<IsFacetable; IsFilterable; IsSearchable>] Town : string
      [<IsFacetable; IsFilterable; IsSearchable>] District : string
      [<IsFacetable; IsFilterable; IsSearchable>] County : string
      [<IsFilterable>] Geo : GeographyPoint }

[<AutoOpen>]
module Management =
    let propertiesIndex config =
        let (ConnectionString c) = config.AzureSearch
        (new SearchServiceClient(config.AzureSearchServiceName, SearchCredentials c)).Indexes.GetClient "properties"

[<AutoOpen>]
module QueryBuilder =
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

let insertProperties config (properties:PropertyResult list) =
    let index = propertiesIndex config
    properties
    |> List.map(fun r ->
        { TransactionId = string r.TransactionId
          Price = r.Price
          DateOfTransfer = r.DateOfTransfer
          PostCode = r.Address.PostCode |> Option.toObj
          PropertyType = r.BuildDetails.PropertyType.ToString()
          Build = r.BuildDetails.Build.ToString()
          Contract = r.BuildDetails.Contract.ToString()
          Building = r.Address.Building
          Street = r.Address.Street |> Option.toObj
          Locality = r.Address.Locality |> Option.toObj
          Town = r.Address.TownCity
          District = r.Address.District
          County = r.Address.County
          Geo = null })

let private toFindPropertiesResponse findFacet count page results =      
    { Results =
        results
        |> Array.map(fun result ->
             { TransactionId = Guid.Parse result.TransactionId
               BuildDetails =
                 { PropertyType = result.PropertyType |> PropertyType.Parse
                   Build = result.Build |> BuildType.Parse
                   Contract = result.Contract |> ContractType.Parse }
               Address =
                 { Building = result.Building
                   Street = result.Street |> Option.ofObj
                   Locality = result.Locality |> Option.ofObj
                   TownCity = result.Town
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

let applyFilters (filter:PropertyFilter) parameters = 
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
