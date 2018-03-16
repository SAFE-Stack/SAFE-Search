module PropertyMapper.Search.InMemory

open PropertyMapper.Contracts
open Giraffe.Tasks

let private data = lazy ("properties.json" |> System.IO.File.ReadAllText |> FableJson.ofJson)

let findByPostcode (request:FindNearestRequest) = task {
    return    
        { Results = [||]
          TotalTransactions = None
          Page = request.Page
          Facets =
            { Towns = []
              Localities = []
              Districts = []
              Counties = []
              Prices = [] } } }

let sortResults sortParams results =
    match sortParams.SortColumn with
    | Some col ->
        let directedCompare a b =
            match PropertyTableColumn.TryParse col with
            | Some Street -> compare a.Address.FirstLine b.Address.FirstLine
            | Some Town -> compare a.Address.TownCity b.Address.TownCity
            | Some Postcode -> compare a.Address.PostCode b.Address.PostCode
            | Some Date -> compare a.DateOfTransfer b.DateOfTransfer
            | Some Price -> compare a.Price b.Price
            | None -> 0
            |> fun v ->
                match sortParams.SortDirection with
                | Some Ascending | None -> v
                | Some Descending -> -v
        results |> Array.sortWith directedCompare
    | None -> results

let findGeneric (request:FindGenericRequest) = task {
    let genericFilter =
        match request.Text with
        | Some text ->
            let text = text.ToUpper()
            fun r ->
                r.Address.County.ToUpper().Contains text ||
                r.Address.District.ToUpper().Contains text ||
                r.Address.Locality |> Option.map(fun l -> l.ToUpper().Contains text) |> Option.defaultValue false ||
                r.Address.TownCity.ToUpper().Contains text ||
                r.Address.Street |> Option.map (fun s -> s.ToUpper().Contains text)  |> Option.defaultValue false
        | None -> fun _ -> true
    let facetFilter filter mapper =
        match filter with
        | Some filter -> mapper >> fun (s:string) -> s.ToUpper() = filter
        | None -> fun _ -> true

    let matches =
        data.Value
        |> Array.filter genericFilter
        |> Array.filter (facetFilter request.Filter.County (fun r -> r.Address.County))
        |> Array.filter (facetFilter request.Filter.District (fun r -> r.Address.District))
        |> Array.filter (facetFilter request.Filter.Locality (fun r -> r.Address.Locality |> Option.defaultValue ""))
        |> Array.filter (facetFilter request.Filter.Town (fun r -> r.Address.TownCity))
        |> sortResults request.Sort
            
    let getFacets mapper = Array.choose mapper >> Array.distinct >> Array.truncate 10 >> Array.toList
    return
        { Results = matches |> Array.skip (request.Page * 20) |> Array.truncate 20
          TotalTransactions = Some matches.Length
          Page = request.Page
          Facets =
            { Towns = matches |> getFacets (fun m -> Some m.Address.TownCity)
              Localities = matches |> getFacets (fun m -> m.Address.Locality)
              Districts = matches |> getFacets (fun m -> Some m.Address.District)
              Counties = matches |> getFacets (fun m -> Some m.Address.County)
              Prices = [] } }
    }