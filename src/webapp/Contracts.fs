namespace PropertyMapper.Contracts

open System

[<CLIMutable>]
type PropertyFilterRaw =
  { Towns : string
    Localities : string
    Districts : string
    Counties : string
    MaxPrice : int option
    MinPrice : int option }

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
    Page : int
    Facets : Facets }