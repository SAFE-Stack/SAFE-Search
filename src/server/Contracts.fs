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
type PropertyType =
  | Detached | SemiDetached | Terraced | FlatsMaisonettes | Other
  member this.Description =
    match this with
    | PropertyType.SemiDetached -> "Semi Detatch"
    | PropertyType.FlatsMaisonettes -> "Flats / Maisonettes"
    | _ -> string this
  static member Parse = function "D" -> Some Detached | "S" -> Some SemiDetached | "T" -> Some Terraced | "F" -> Some FlatsMaisonettes | "O" -> Some Other | _ -> None
type BuildType =
  | NewBuild | OldBuild
  member this.Description =
    match this with
    | BuildType.OldBuild -> "Old Build"
    | BuildType.NewBuild -> "New Build"
  static member Parse = function "Y" -> NewBuild | _ -> OldBuild

type ContractType =
  | Freehold | Leasehold
  member this.Description = string this
  static member Parse = function "F" -> Freehold | _ -> Leasehold

type Address =
    { Building : string
      Street : string option
      Locality : string option
      TownCity : string
      District : string
      County : string
      PostCode : string option }
type BuildDetails =
    { PropertyType : PropertyType option
      Build : BuildType
      Contract : ContractType }
type PropertyResult =
    { TransactionId : Guid
      BuildDetails : BuildDetails
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
