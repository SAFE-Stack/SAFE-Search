namespace Pages
open PropertyMapper.Contracts

type SearchTerm =
    | SearchTerm of string
    static member Empty = SearchTerm ""