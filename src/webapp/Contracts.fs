namespace Contracts

open System

type FindPropertiesRequest = { Postcode : string; Distance : int; Page : int}
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
type FindPropertiesResponse = { Results : PropertyResult array; Facets : Facets }
