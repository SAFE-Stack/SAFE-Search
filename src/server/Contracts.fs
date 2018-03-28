namespace PropertyMapper.Contracts

open System

type SortDirection =
    | Ascending | Descending
    static member TryParse str =
        if string Ascending = str then Some Ascending
        elif string Descending = str then Some Descending
        else None
[<CLIMutable>]
type Sort = { SortColumn : string option; SortDirection : SortDirection option }
[<CLIMutable>]
type PropertyFilter =
  { Town : string option
    Locality : string option
    District : string option
    County : string option
    MaxPrice : int option
    MinPrice : int option }
type PropertyTableColumn =
    | Street | Town | Postcode | Date | Price
    static member TryParse s =
        if s = string Street then Some Street
        elif s = string Town then Some Town
        elif s = string Postcode then Some Postcode
        elif s = string Date then Some Date
        elif s = string Price then Some Price
        else None
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
      Prices : string list }
type SearchResponse =
  { Results : PropertyResult array
    TotalTransactions : int option
    Page : int
    Facets : Facets }
type SuggestResponse =
    { Suggestions : string array }
