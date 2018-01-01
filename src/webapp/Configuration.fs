namespace PropertyMapper

type ConnectionString = ConnectionString of string
type Configuration =
    { AzureStorage : ConnectionString
      AzureSearch : ConnectionString
      AzureSearchServiceName : string }

