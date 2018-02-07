namespace PropertyMapper.Search

open PropertyMapper.Contracts
open System.Threading.Tasks

type FindNearestRequest = { Postcode : string; MaxDistance : int; Page : int; Filter : PropertyFilter }
type FindGenericRequest = { Text : string option; Page : int; Filter : PropertyFilter }
type Geo = { Lat : float; Long : float }

type ISearchEngine =
    abstract GenericSearch : FindGenericRequest -> SearchResponse Task
    abstract PostcodeSearch : FindNearestRequest -> SearchResponse Task