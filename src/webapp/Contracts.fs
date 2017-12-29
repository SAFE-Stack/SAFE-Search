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
type PropertyType =
  | Detached | SemiDetached | Terraced | FlatsMaisonettes | Other
  member this.Description =
    match this with
    | PropertyType.SemiDetached -> "Semi Detatch"
    | PropertyType.FlatsMaisonettes -> "Flats / Maisonettes"
    | _ -> string this
type BuildType =
  | NewBuild | OldBuild
  member this.Description =
    match this with
    | BuildType.OldBuild -> "Old Build"
    | BuildType.NewBuild -> "New Build"

type ContractType =
  | Freehold | Leasehold
  member this.Description = string this

type Address =
    { Building : string
      Street : string
      Locality : string
      TownCity : string
      District : string
      County : string
      PostCode : string }
type BuildDetails =
    { PropertyType : PropertyType
      Build : BuildType
      Contract : ContractType }
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
