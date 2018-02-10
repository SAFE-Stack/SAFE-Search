namespace Pages

type SearchTerm =
    | Term of string
    | Postcode of string
    static member Empty = Term ""
    member this.Description = match this with | Term x | Postcode x -> x

[<AutoOpen>]
module Helpers =
    let commaSeparate : int -> _ = string >> Seq.toArray >> Array.rev >> Array.chunkBySize 3 >> Array.map Array.rev >> Array.rev >> Array.map System.String >> String.concat ","
    