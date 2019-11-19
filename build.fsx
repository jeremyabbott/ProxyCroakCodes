#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open Fake.Core
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

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run

let runToolIgnore cmd args workingDir = runTool cmd args workingDir |> ignore

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

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [deployDir]
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__ |> ignore
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__ |> ignore
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__ |> ignore
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
    Shell.regexReplaceInFileWithEncoding
        "let app = \".+\""
       ("let app = \"" + release.NugetVersion + "\"")
        System.Text.Encoding.UTF8
        (Path.combine clientPath "Version.fs")
    runToolIgnore yarnTool "webpack-cli --config src/Client/webpack.config.js -p" clientPath
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runToolIgnore yarnTool "webpack-dev-server --config src/Client/webpack.config.js" clientPath
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

Target.create "RunImage" (fun _ ->
    let args = sprintf "run -it --rm -p 8085:8085 %s" dockerFullName
    let result = runTool "docker" args __SOURCE_DIRECTORY__
    if result.ExitCode <> 0 then
        printfn "Failed to run docker container: %A" result.Result
)

Target.create "Docker" (fun _ ->
    let buildArgs = sprintf "build -t %s ." dockerFullName
    let result = runTool "docker" buildArgs __SOURCE_DIRECTORY__
    if result.ExitCode <> 0 then
        printfn "Failed to build docker image: %A" result.Result
)

Target.create "PrepareRelease" (fun _ ->
    Branches.checkout "" false "master"
    CommandHelper.directRunGitCommand __SOURCE_DIRECTORY__ "fetch origin" |> ignore
    CommandHelper.directRunGitCommand __SOURCE_DIRECTORY__ "fetch origin --tags" |> ignore

    Staging.stageAll __SOURCE_DIRECTORY__
    Commit.exec __SOURCE_DIRECTORY__ (sprintf "Bumping version to %O" release.NugetVersion)
    Branches.pushBranch __SOURCE_DIRECTORY__ "origin" "master"

    let tagName = string release.NugetVersion
    Branches.tag __SOURCE_DIRECTORY__ tagName
    Branches.pushTag __SOURCE_DIRECTORY__ "origin" tagName

    try
        let arguments = sprintf "tag %s %s:%s" dockerFullName dockerFullName release.NugetVersion
        runToolIgnore "docker" arguments __SOURCE_DIRECTORY__
    with
        _ -> failwith "Docker tag failed"
)


Target.create "Deploy" (fun _ ->
    try
        let arguments = sprintf "login %s --username %s --password %s" dockerLoginServer dockerUser dockerPassword
        runToolIgnore "docker" arguments __SOURCE_DIRECTORY__
    with _ ->
        failwith "Docker login failed"

    try
        let arguments = sprintf "push %s" dockerFullName
        runToolIgnore "docker" arguments __SOURCE_DIRECTORY__
     with _ -> failwith "Docker push failed"
)

open Fake.Core.TargetOperators
"Clean"
    ==> "InstallClient"
    ==> "RestoreServer"
    ==> "Build"
    ==> "Bundle"
    ==> "Docker"
    ==> "PrepareRelease"
    ==> "Deploy"

"Clean"
    ==> "InstallClient"
    ==> "RestoreServer"
    ==> "Run"

Target.runOrDefaultWithArguments "Build"