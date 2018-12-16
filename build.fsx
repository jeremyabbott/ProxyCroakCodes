#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open Fake.Core
open System
open System.IO
open Fake.IO
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
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"
let npxTool = platformTool "npx" "npx.exe"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore

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

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    runDotNet "restore" clientPath
)

Target.create "InstallDotNetCore" (fun _ ->
    let version = DotNet.CliVersion.GlobalJson

    DotNet.install (fun opts -> { opts with Version = version }) (DotNet.Options.Create()) |> ignore
)

Target.create "RestoreServer" (fun _ ->
    runDotNet "restore" serverPath
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    // runDotNet "fable webpack-cli -- --config src/Client/webpack.config.js -p" clientPath
    runTool npxTool "webpack --config webpack.config.js -p" clientPath
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runDotNet "fable webpack-dev-server -- --config src/Client/webpack.config.js" clientPath
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    [ server; client; browser ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "Bundle" (fun _ ->
    let serverDir = Path.combine deployDir "Server"
    let clientDir = Path.combine deployDir "Client"
    let publicDir = Path.combine clientDir "public"

    let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
    runDotNet publishArgs serverPath

    Shell.copyDir publicDir "src/Client/public" FileFilter.allFiles
)

let dockerFullName = sprintf "%s/%s" dockerUser dockerImageName


Target.create "Docker" (fun _ ->
    let buildArgs = sprintf "build -t %s ." dockerFullName
    runTool "docker" buildArgs "."

    let tagArgs = sprintf "tag %s %s" dockerFullName dockerFullName
    runTool "docker" tagArgs "."
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
    ==> "InstallClient"
    ==> "Build"
//#if (deploy == "docker")
    ==> "Bundle"
    ==> "Docker"

"Clean"
    ==> "InstallClient"
    ==> "RestoreServer"
    ==> "Run"

Target.runOrDefault "Build"