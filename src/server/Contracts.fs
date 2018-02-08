namespace PropertyMapper.Contracts

open System

[<CLIMutable>]
type PropertyFilter =
  { Town : string option
    Locality : string option
    District : string option
    County : string option
    PriceRange : string option }
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
      PriceRanges : string list }
type SearchResponse =
  { Results : PropertyResult array
    TotalTransactions : int option
    Page : int
    Facets : Facets }

[<AutoOpen>]
module Helpers =
  let calculatePriceRange =
      let bands = [| 0; 50; 100; 150; 200; 250; 350; 500; 750; 1000 |] |> Array.map ((*) 1000) |> Array.pairwise
      fun price ->
          bands
          |> Array.tryFind(fun (min, max) -> price > min && price <= max)
          |> Option.map(fun (min, max) -> String.Format("{0:C0} - {1:C0}", min, max))
          |> Option.defaultValue "OVER £1M"