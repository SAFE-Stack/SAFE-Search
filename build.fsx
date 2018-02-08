// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @".\packages\Newtonsoft.Json\lib\netstandard1.3"
#r @"packages/build/FAKE/tools/FakeLib.dll"
#r @"packages/build/Microsoft.Rest.ClientRuntime.Azure/lib/netstandard1.4/Microsoft.Rest.ClientRuntime.Azure.dll"
#load @"src\scripts\importdata.fsx"
      @".paket\load\netstandard2.0\Build\build.group.fsx"
      @"paket-files\build\CompositionalIT\fshelpers\src\FsHelpers\ArmHelper\ArmHelper.fs"

open Cit.Helpers.Arm
open Cit.Helpers.Arm.Parameters
open Fake
open System
open System.IO
open Microsoft.IdentityModel.Clients.ActiveDirectory
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Core

let project = "Property Mapper"
let summary = "A project to illustrate use of Azure Search to map UK property sales using SAFE."
let description = summary

let clientPath = FullName "./src/Client"
let serverPath = FullName "./src/Server/"
let dotnetcliVersion = DotNetCli.getVersion()
let mutable dotnetExePath = "dotnet"
let deployDir = "./deploy"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

let run cmd args dir =
    if execProcess (fun info ->
        info.FileName <- cmd
        if not (String.IsNullOrWhiteSpace dir) then
            info.WorkingDirectory <- dir
        info.Arguments <- args
    ) System.TimeSpan.MaxValue |> not then
        failwithf "Error while running '%s' with args: %s" cmd args

let runDotnet workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "dotnet %s failed" args

let platformTool tool winTool =
    let tool = if isUnix then tool else winTool
    tool
    |> ProcessHelper.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let yarnTool = platformTool "yarn" "yarn.cmd"

do if not isWindows then
    // We have to set the FrameworkPathOverride so that dotnet sdk invocations know
    // where to look for full-framework base class libraries
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName(mono) </> ".." </> "lib" </> "mono" </> "4.5"
    setEnvironVar "FrameworkPathOverride" frameworkPath

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    !!"src/**/bin" |> CleanDirs
    !! "src/**/obj/*.nuspec" |> DeleteFiles
    CleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")])

Target "InstallDotNetCore" (fun _ -> dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion)

// --------------------------------------------------------------------------------------
// Build library

Target "BuildServer" (fun _ -> runDotnet serverPath "build")

Target "InstallClient" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__)

Target "BuildClient" (fun _ ->
    runDotnet clientPath "restore"
    runDotnet clientPath "fable webpack -- -p")

// --------------------------------------------------------------------------------------
// Azure Deployment

Target "DeployArmTemplate" <| fun _ ->
    let armTemplate = @"src\arm-template.json"
    let environment = getBuildParamOrDefault "environment" (Guid.NewGuid().ToString().ToLower().Split '-' |> Array.head)
    let resourceGroupName = sprintf "safe-property-mapper"

    tracefn "Deploying template '%s' to resource group '%s'..." armTemplate resourceGroupName
           
    let deployment =
        { DeploymentName = "FAKE-PropertyMapper-Deploy"
          ResourceGroup = ResourceGroupType.New(resourceGroupName, Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region.EuropeWest)
          ArmTemplate = File.ReadAllText armTemplate
          Parameters =
            [ "environment", environment
              "searchSize", getBuildParam "searchSize"
              "webServerSize", getBuildParam "webServerSize"
              "alwaysOn", getBuildParam "alwaysOn" ]
            |> List.choose(fun (k, v) -> if String.IsNullOrWhiteSpace v then None else Some (k, ArmString v))
            |> Parameters.Simple
          DeploymentMode = Incremental }

    let authCtx =
        match getBuildParam "subscriptionId", (getBuildParam "clientId", getBuildParam "clientSecret", getBuildParam "tenantId") with
        | "", _ -> failwith "No subscription id supplied!"
        | subscriptionId, ("", _, _ | _, "", _ | _, _, "")->
            let client = AuthenticationContext "https://login.microsoftonline.com/common"
            async {
                let! deviceResult = client.AcquireDeviceCodeAsync("https://management.core.windows.net/", "84edda74-cb5b-49e3-99b3-82d1c419e651") |> Async.AwaitTask
                tracefn "%s" deviceResult.Message
                let! tokenResult = client.AcquireTokenByDeviceCodeAsync(deviceResult) |> Async.AwaitTask
                let client = RestClient.Configure().WithEnvironment(AzureEnvironment.AzureGlobalCloud).WithCredentials(Microsoft.Rest.TokenCredentials(tokenResult.AccessToken)).Build()
                return ResourceManager.Authenticate(client).WithSubscription subscriptionId |> AuthenticatedContext } |> Async.RunSynchronously
        | subscriptionId, (clientId, clientSecret, tenantId) ->
            let authCredentials =
                { ClientId = Guid.Parse clientId
                  ClientSecret = clientSecret
                  TenantId = Guid.Parse tenantId }
            authenticate authCredentials (Guid.Parse subscriptionId)

    deployment
    |> deployWithProgress authCtx
    |> Seq.iter(function
    | DeploymentInProgress (state, operations) -> tracefn "State is %s, completed %d operations." state operations
    | DeploymentError (statusCode, message) -> traceError <| sprintf "DEPLOYMENT ERROR: %s - '%s'" statusCode message
    | DeploymentCompleted _ -> ())

// --------------------------------------------------------------------------------------
// Data Import
open PropertyMapper
open Importdata
let (|Local|Cloud|) (x:string) =
    match x.ToLower() with
    | "cloud" -> Cloud
    | _ -> Local

Target "ImportData" (fun _ ->
    let mode = getBuildParam "DataMode"
    
    log "Downloading transaction data..."

    // Insert postcode / geo lookup
    if (not (fileExists (FullName "ukpostcodes.csv"))) then
        let txns = fetchTransactions 1000
        log "Downloading geolocation data..."
        let archivePath = "ukpostcodes.zip"
        do
            use wc = new Net.WebClient()
            wc.DownloadFile(Uri "https://www.freemaptools.com/download/full-postcodes/ukpostcodes.zip", archivePath)
        archivePath |> Unzip "."
        DeleteFile archivePath

        let postCodes =
            let loadedPostcodes = txns |> Array.Parallel.choose(fun t -> t.Address.PostCode) |> Set
            fetchPostcodes (FullName "ukpostcodes.csv")
            |> Array.filter(fun (r:Importdata.GeoPostcode) -> loadedPostcodes.Contains r.PostCodeDescription)

        let tryFindGeo = postCodes |> Seq.map(fun r -> r.PostCodeDescription, (r.Latitude, r.Longitude)) |> Map.ofSeq |> fun m -> m.TryFind
        let insertPostcodeLookup (ConnectionString connectionString) =
            postCodes
            |> insertPostcodes connectionString
            |> Array.collect snd
            |> Array.countBy(function FSharp.Azure.StorageTypeProvider.Table.SuccessfulResponse _ -> "Success" | _ -> "Failed")
            |> logfn "Postcode insertion results: %A"

        log "Now inserting property transactions into search index and creating postcode lookup..."
        match mode with
        | Local ->
            let path = serverPath </> "properties.json"
            File.WriteAllText(path, FableJson.toJson txns)
            AzureHelper.StartStorageEmulator()
            insertPostcodeLookup (ConnectionString "UseDevelopmentStorage=true")

        | Cloud ->
            let config =
                { AzureSearchServiceName = ""
                  AzureStorage = ConnectionString ""
                  AzureSearch = ConnectionString "" }

            Search.Azure.Management.initialize config

            txns
            |> Search.Azure.insertProperties config tryFindGeo
            |> ignore

            insertPostcodeLookup config.AzureStorage)

// --------------------------------------------------------------------------------------
// Run the Website

let ipAddress = "localhost"
let port = 8080

FinalTarget "KillProcess" (fun _ ->
    killProcess "dotnet"
    killProcess "dotnet.exe")

Target "RunServer" (fun _ -> async { runDotnet serverPath "run" } |> Async.Start)

Target "RunClient" (fun _ ->
    runDotnet clientPath "restore"

    let fablewatch = async { runDotnet clientPath "fable webpack-dev-server" }
    let openBrowser = async {
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" port) |> ignore }

    Async.Parallel [| fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore)

Target "BundleClient" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- serverPath
            info.Arguments <- "publish -c Release -o \"" + FullName deployDir + "\"") TimeSpan.MaxValue
    if result <> 0 then failwith "Publish failed"

    let clientDir = deployDir </> "client"
    let publicDir = clientDir </> "public"
    let jsDir = clientDir </> "js"
    let cssDir = clientDir </> "css"
    let imageDir = clientDir </> "Images"

    !! "src/Client/public/**/*.*" |> CopyFiles publicDir
    !! "src/Client/js/**/*.*" |> CopyFiles jsDir
    !! "src/Client/css/**/*.*" |> CopyFiles cssDir
    !! "src/Client/Images/**/*.*" |> CopyFiles imageDir

    "src/Client/index.html" |> CopyFile clientDir)

// -------------------------------------------------------------------------------------
Target "Build" DoNothing
Target "All" DoNothing

"Clean"
  ==> "InstallDotNetCore"
  ==> "InstallClient"
  ==> "BuildServer"
  ==> "BuildClient"
  ==> "BundleClient"

"BuildClient"
  ==> "Build"

"InstallClient"
  ==> "ImportData"
  ==> "RunServer"
  ==> "All"

"RunClient"
  ==> "All"

RunTargetOrDefault "All"