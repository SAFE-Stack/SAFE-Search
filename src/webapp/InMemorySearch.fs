module PropertyMapper.InMemorySearch

open PropertyMapper.Search
open PropertyMapper.Contracts
open Giraffe.Tasks

let private data = lazy ("properties.json" |> System.IO.File.ReadAllText |> FableJson.ofJson)

let findGeneric (request:FindGenericRequest) = task {
    let text = request.Text.ToUpper()    
    let matches =
        data.Value
        |> Array.filter(fun r ->
            r.Address.County.ToUpper().Contains text ||
            r.Address.District.ToUpper().Contains text ||
            r.Address.Locality |> Option.map(fun l -> l.ToUpper().Contains text) |> Option.defaultValue false ||
            r.Address.TownCity.ToUpper().Contains text ||
            r.Address.Street |> Option.map (fun s -> s.ToUpper().Contains text)  |> Option.defaultValue false)
    let findFacet mapper = Array.choose mapper >> Array.distinct >> Array.truncate 10 >> Array.toList
    return
        { Results = matches |> Array.skip (request.Page * 20) |> Array.truncate 20
          TotalTransactions = Some matches.Length
          Page = request.Page
          Facets =
            { Towns = matches |> findFacet (fun m -> Some m.Address.TownCity)
              Localities = matches |> findFacet (fun m -> m.Address.Locality)
              Districts = matches |> findFacet (fun m -> Some m.Address.District)
              Counties = matches |> findFacet (fun m -> Some m.Address.County)
              Prices = [] } }
    }