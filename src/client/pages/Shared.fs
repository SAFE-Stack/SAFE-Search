namespace Pages
open PropertyMapper.Contracts

type SearchTerm =
    | Term of string
    | PostcodeSearch of string
    static member Empty = Term ""
    member this.Description = match this with | Term x | PostcodeSearch x -> x

[<AutoOpen>]
module Helpers =
    let commaSeparate : int -> _ = string >> Seq.toArray >> Array.rev >> Array.chunkBySize 3 >> Array.map Array.rev >> Array.rev >> Array.map System.String >> String.concat ","
    