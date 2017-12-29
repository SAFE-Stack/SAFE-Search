namespace Pages
open PropertyMapper.Contracts

type SearchTerm =
    | SearchTerm of string
    static member Empty = SearchTerm ""

[<AutoOpen>]
module Helpers =
    let commaSeparate : int -> _ = string >> Seq.toArray >> Array.rev >> Array.chunkBySize 3 >> Array.map Array.rev >> Array.rev >> Array.map System.String >> String.concat ","
    