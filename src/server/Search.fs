namespace PropertyMapper.Search

open PropertyMapper.Contracts
open System.Threading.Tasks

type FindNearestRequest = { Postcode : string; MaxDistance : int; Page : int; Filter : PropertyFilter }
type FindGenericRequest = { Text : string option; Page : int; Filter : PropertyFilter; Sort : Sort }
type Geo = { Lat : float; Long : float }
type SuggestRequest = { Text : string }

type ISearch =
    abstract GenericSearch : FindGenericRequest -> SearchResponse Task
    abstract PostcodeSearch : FindNearestRequest -> SearchResponse Task
    abstract Suggest : SuggestRequest -> SuggestResponse Task