/// This script creates a local dataset that can be used instead of Azure Search.

#load @"..\..\.paket\load\net471\FSharp.Data.fsx"
      @"..\..\.paket\load\net471\Newtonsoft.Json.fsx"
      @"..\..\.paket\load\net471\Fable.JsonConverter.fsx"
      @"..\server\Contracts.fs"

open Newtonsoft.Json
open FSharp.Data
open PropertyMapper.Contracts

[<Literal>]
let PricePaidSchema = __SOURCE_DIRECTORY__ + @"\schema.csv"
type PricePaid = CsvProvider<PricePaidSchema, PreferOptionals = true, Schema="Date=Date">
let fetchData rows =
    let data = PricePaid.Load "http://prod.publicdata.landregistry.gov.uk.s3-website-eu-west-1.amazonaws.com/pp-monthly-update-new-version.csv"
    data.Rows
    |> Seq.take rows
    |> Seq.map(fun t ->
        { Address =
            { Building = t.PAON + (t.SAON |> Option.map (sprintf " %s") |> Option.defaultValue "")
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