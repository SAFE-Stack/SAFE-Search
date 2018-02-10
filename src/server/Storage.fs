module PropertyMapper.AzureStorage

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open PropertyMapper
open FSharp.Control.Tasks
open PropertyMapper.Search

let table (ConnectionString connection) =
    let tableClient =
        let connection = CloudStorageAccount.Parse connection
        connection.CreateCloudTableClient()
    tableClient.GetTableReference "postcodes"

let tryGetGeo config postcodeA postcodeB = task {
    let! result = TableOperation.Retrieve<DynamicTableEntity>(postcodeA, postcodeB) |> (table config.AzureStorage).ExecuteAsync
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
