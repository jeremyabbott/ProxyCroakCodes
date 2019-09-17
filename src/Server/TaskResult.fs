namespace ProxyCroakCodes

open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading.Tasks

module TaskResult =
    let bind f (m: Task<Result<'a, 'b>>) = task {
        let! m' = m
        return
            match m' with
            | Ok x -> f x
            | Error e -> Error e
    }

[<AutoOpen>]
module Builder =
    type TaskResultBuilder () =
        member __.Return(x) = task { return Ok x }
        member __.ReturnFrom(m: Task<Result<'a, 'b>>) = m
        member __.Bind(m, f) = TaskResult.bind f m

    let taskResult = TaskResultBuilder()