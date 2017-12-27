namespace PropertyMapper.Contracts

open System

[<CLIMutable>]
type PropertyFilter =
  { Town : string option
    Locality : string option
    District : string option
    County : string option
    MaxPrice : int option
    MinPrice : int option }
  static member Empty = { Town = None; Locality = None; District = None; County = None; MaxPrice = None; MinPrice = None }
type FindNearestRequest = { Postcode : string; MaxDistance : int; Page : int; Filter : PropertyFilter }
type FindGenericRequest = { Text : string; Page : int; Filter : PropertyFilter }
type Address =
    { Building : string
      Street : string
      Locality : string
      TownCity : string
      District : string
      County : string
      PostCode : string }
type BuildDetails =
    { PropertyType : string
      OldNew : string
      Duration : string }
type PropertyResult =
    { BuildDetails : BuildDetails
      Address : Address
      Price : int
      DateOfTransfer : DateTime }
type Facets =
    { Towns : string list
      Localities : string list
      Districts : string list
      Counties : string list
      Prices : string list }
type SearchResponse =
  { Results : PropertyResult array
    TotalTransactions : int option
    Facets : Facets }