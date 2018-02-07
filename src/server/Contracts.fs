namespace PropertyMapper.Contracts

open System

[<CLIMutable>]
type PropertyFilter =
  { Town : string option
    Locality : string option
    District : string option
    County : string option
    ``Price range`` : string option }
type PropertyType =
  | Detached | SemiDetached | Terraced | FlatsMaisonettes | Other
  member this.Description =
    match this with
    | SemiDetached -> "Semi Detatch"
    | FlatsMaisonettes -> "Flats / Maisonettes"
    | _ -> string this
  static member Parse = function "D" -> Some Detached | "S" -> Some SemiDetached | "T" -> Some Terraced | "F" -> Some FlatsMaisonettes | "O" -> Some Other | _ -> None
type BuildType =
  | NewBuild | OldBuild
  member this.Description =
    match this with
    | OldBuild -> "Old Build"
    | NewBuild -> "New Build"
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
    member address.FirstLine =
      [ Some address.Building; address.Street ]
      |> List.choose id
      |> String.concat " "
type BuildDetails =
    { PropertyType : PropertyType option
      Build : BuildType
      Contract : ContractType }
type PriceRange =
    | ``P < 200``
    | ``P 200 - 300``
    | ``P 300 - 400``
    | ``P > 400``
    member this.Description =
        match this with
        | ``P < 200`` -> "Less than £200k"
        | ``P 200 - 300`` -> "£200k - £300k"
        | ``P 300 - 400`` -> "£300k - £400k"
        | ``P > 400`` -> "More than £400k"
    static member ofPrice price =
        if price < 200_000 then ``P < 200``
        elif price < 300_000 then ``P 200 - 300``
        elif price < 400_000 then ``P 300 - 400``
        else ``P > 400``

type PropertyResult =
    { TransactionId : Guid
      BuildDetails : BuildDetails
      Address : Address
      Price : int
      DateOfTransfer : DateTime
      PriceRange : PriceRange }
type Facets =
    { Towns : string list
      Localities : string list
      Districts : string list
      Counties : string list
      PriceRanges : string list }
type SearchResponse =
  { Results : PropertyResult array
    TotalTransactions : int option
    Page : int
    Facets : Facets }
