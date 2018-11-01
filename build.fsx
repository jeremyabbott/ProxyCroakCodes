open Fake.Core
#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System
open System.IO
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.Tools.Git

let serverPath = "./src/Server" |> Path.getFullName
let clientPath = "./src/Client" |> Path.getFullName
let deployDir = "./deploy" |> Path.getFullName

let dockerUser = Environment.environVarOrDefault "DockerUser" ""
let dockerPassword = Environment.environVarOrDefault "DockerPassword" ""
let dockerLoginServer = Environment.environVarOrDefault "DockerLoginServer" "docker.io"
let dockerImageName = "proxycroakcodes"

let releaseNotes = File.ReadAllLines "RELEASE_NOTES.md"

let releaseNotesData =
    releaseNotes
    |> ReleaseNotes.parseAll

let release = List.head releaseNotesData

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    tool
    |> ProcessUtils.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let mutable dotnetCli = "dotnet"

let run cmd args workingDir =
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = cmd
                WorkingDirectory = workingDir
                Arguments = args
            }
        ) TimeSpan.MaxValue
    if result <> 0 then failwithf "'%s %s' failed" cmd args

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [deployDir]
)

Target.create "InstallDotNetCore" (fun _ ->
    let version = DotNet.CliVersion.GlobalJson

    DotNet.install (fun opts -> { opts with Version = version }) (DotNet.Options.Create()) |> ignore
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target.create "BuildServer" (fun _ ->
    run dotnetCli "build" serverPath
)

Target.create "BuildClient" (fun _ ->
    run dotnetCli "restore" clientPath
    run dotnetCli "fable webpack-cli -- --config src/Client/webpack.config.js -p" clientPath
)


Target.create "Run" (fun _ ->
    let server = async {
        run dotnetCli "watch run" serverPath
    }
    let client = async {
        run dotnetCli "fable webpack-dev-server -- --config src/Client/webpack.config.js" clientPath
    }
    let browser = async {
        Threading.Thread.Sleep 5000
        Diagnostics.Process.Start "http://127.0.0.1:8080" |> ignore
    }

    [ server; client; browser]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "PrepareRelease" (fun _ ->
    Branches.checkout "" false "master"
    CommandHelper.directRunGitCommand "" "fetch origin" |> ignore
    CommandHelper.directRunGitCommand "" "fetch origin --tags" |> ignore

    Staging.stageAll ""
    Fake.Tools.Git.Commit.exec "" (sprintf "Bumping version to %O" release.NugetVersion)
    Branches.pushBranch "" "origin" "master"

    let tagName = string release.NugetVersion
    Branches.tag "" tagName
    Branches.pushTag "" "origin" tagName

    let result =
        Process.execSimple (fun info ->
            let arguments = sprintf "tag %s/%s %s/%s:%s" dockerUser dockerImageName dockerUser dockerImageName release.NugetVersion
            { info with FileName = "docker"; Arguments = arguments }) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker tag failed"
)

Target.create "BundleClient" (fun _ ->
    let result =
        Process.execSimple (fun info ->
            let arguments = "publish -c Release -o \"" + Path.getFullName deployDir + "\""
            { info with FileName = dotnetCli; WorkingDirectory = serverPath; Arguments = arguments }) TimeSpan.MaxValue
    if result <> 0 then failwith "Publish failed"

    let clientDir = deployDir </> "Client"
    let publicDir = clientDir </> "public"
    let jsDir = clientDir </> "js"
    let cssDir = clientDir </> "css"
    let imageDir = clientDir </> "Images"

    !! "src/Client/public/**/*.*" |> Shell.copyFiles publicDir
    !! "src/Client/js/**/*.*" |> Shell.copyFiles jsDir
    !! "src/Client/css/**/*.*" |> Shell.copyFiles cssDir
    !! "src/Client/Images/**/*.*" |> Shell.copyFiles imageDir

    "src/Client/index.html" |> Shell.copyFile clientDir
)

Target.create "CreateDockerImage" (fun _ ->
    if String.IsNullOrEmpty dockerUser then
        failwithf "docker username not given."
    if String.IsNullOrEmpty dockerImageName then
        failwithf "docker image Name not given."
    let result =
        Process.execSimple (fun info ->
            let arguments = sprintf "build -t %s/%s ." dockerUser dockerImageName
            { info with FileName = "docker"; UseShellExecute = false; Arguments = arguments }) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker build failed"
)

Target.create "Deploy" (fun _ ->
    let result =
        Process.execSimple (fun info ->
            let arguments = sprintf "login %s --username \"%s\" --password \"%s\"" dockerLoginServer dockerUser dockerPassword
            { info with FileName = "docker"; WorkingDirectory = deployDir; Arguments = arguments } ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker login failed"

    let result =
        Process.execSimple (fun info ->
            let arguments = sprintf "push %s/%s" dockerUser dockerImageName
            { info with FileName = "docker"; WorkingDirectory = deployDir; Arguments = arguments }) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker push failed"
)

open Fake.Core.TargetOperators
"Clean"
    ==> "InstallDotNetCore"
    ==> "InstallClient"
    ==> "BuildServer"
    ==> "BuildClient"

"InstallClient"
    ==> "Run"

"BuildClient"
    ==> "BundleClient"
    ==> "CreateDockerImage"
    ==> "PrepareRelease"
    ==> "Deploy"

Target.runOrDefault "BuildClient"