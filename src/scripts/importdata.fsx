/// This script creates a local dataset that can be used instead of Azure Search.

#I @"..\.."
#I @"..\..\.paket\load\"
#load @"net461\Build\FSharp.Data.fsx"
      @"packages\build\FSharp.Azure.StorageTypeProvider\StorageTypeProvider.fsx"
      @"netstandard2.0\Fable.JsonConverter.fsx"
      @"src\server\Contracts.fs"
      @"netstandard2.0\Giraffe.fsx"
      @"netstandard2.0\Microsoft.Azure.Search.fsx"
      @"netstandard2.0\System.ComponentModel.Annotations.fsx"
      @"src\server\Configuration.fs"
      @"src\server\Search.fs"
      @"src\server\Search.Azure.fs"

open FSharp.Data
open Newtonsoft.Json
open PropertyMapper.Contracts

[<Literal>]
let PricePaidSchema = __SOURCE_DIRECTORY__ + @"\price-paid-schema.csv"
type PricePaid = CsvProvider<PricePaidSchema, PreferOptionals = true, Schema="Date=Date">
let fetchTransactions rows =
    let data = PricePaid.Load "http://prod.publicdata.landregistry.gov.uk.s3-website-eu-west-1.amazonaws.com/pp-monthly-update-new-version.csv"
    data.Rows
    |> Seq.take rows
    |> Seq.map(fun t ->
        { TransactionId = t.TransactionId
          Address =
            { Building = [ Some t.PAON; t.SAON ] |> List.choose id |> String.concat " "
              Street = t.Street
              Locality = t.Locality
              TownCity = t.``Town/City``
              District = t.District
              County = t.County
              PostCode = t.Postcode }
          BuildDetails =
            { PropertyType = t.PropertyType |> Option.bind PropertyType.Parse
              Build = t.Duration |> BuildType.Parse
              Contract = t.``Old/New`` |> ContractType.Parse }
          Price = t.Price
          DateOfTransfer = t.Date })
    |> Seq.toArray

module FableJson =
    let private jsonConverter = Fable.JsonConverter() :> JsonConverter
    let toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])
    let ofJson (json:string) = JsonConvert.DeserializeObject<'a>(json, [|jsonConverter|])

[<Literal>]
let PostCodesSchema = __SOURCE_DIRECTORY__ + @"\uk-postcodes-schema.csv"

type Postcodes = CsvProvider<PostCodesSchema, PreferOptionals = true, Schema="Latitude=decimal option,Longitude=decimal option">

type GeoPostcode =
    { PostCode : string * string
      Latitude : float
      Longitude : float }
    member this.PostCodeDescription = sprintf "%s %s" (fst this.PostCode) (snd this.PostCode)
let fetchPostcodes (path:string) =
    let data = Postcodes.Load path
    data.Rows
    |> Seq.choose(fun r ->
        match r.Postcode.Split ' ', r.Latitude, r.Longitude with
        | [| partA; partB |], Some latitude, Some longitude ->
            Some
                { PostCode = (partA, partB)
                  Latitude = float latitude
                  Longitude = float longitude }
        | _ -> None)
    |> Seq.toArray

open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Table

[<Literal>]
let AzureSchema = __SOURCE_DIRECTORY__ + @"\azure-schema.json"
type Azure = AzureTypeProvider<tableSchema = AzureSchema>
let table = Azure.Tables.postcodes
let insertPostcodes connectionString postcodes =
    table.AsCloudTable().CreateIfNotExists() |> ignore
    let entities =
        postcodes
        |> Seq.map(fun p ->
            let partA, partB = p.PostCode
            Azure.Domain.postcodesEntity(Partition partA, Row partB, p.PostCodeDescription, p.Longitude, p.Latitude))
        |> Seq.toArray
    table.Insert(entities, TableInsertMode.Upsert, connectionString)

