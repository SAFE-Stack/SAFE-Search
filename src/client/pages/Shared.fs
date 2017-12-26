namespace Pages

type SearchTerm =
    | SearchTerm of string
    static member Empty = SearchTerm ""

