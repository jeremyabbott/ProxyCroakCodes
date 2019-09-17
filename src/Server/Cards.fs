namespace ProxyCroakCodes.Cards

open FSharp.Control.Tasks.V2.ContextInsensitive
open Saturn.CSRF.View
open Saturn
open Saturn.ControllerHelpers.Response
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Caching.Memory
open ProxyCroakCodes
open Shared

module Controller =
    let private cardsCacheKey = sprintf "cards:%s"
    let private setsCacheKey = "sets"

    let private getFromCache<'t> (cache: IMemoryCache) key =
        let success, entry = cache.TryGetValue<'t>(key)
        if success then Ok entry
        else sprintf "Key %s not found in cache" key |> Error

    let private addToCache (cache: IMemoryCache) key entry =
        let options = MemoryCacheEntryOptions().SetSlidingExpiration(System.TimeSpan.FromHours(1.))
        cache.Set(key, entry, options)

    let private getService<'t> (ctx: HttpContext) =
        ctx.RequestServices.GetService(typeof<'t>) :?> 't

    let private getCards cache serializer name =
        task {
            let key = cardsCacheKey name
            let cachedCards = getFromCache<PtcgCard seq> cache key
            match cachedCards with
            | Ok cc -> return Ok cc
            | Error e ->
                let! apiCards = Domain.getCardsFromApiByName serializer name
                return apiCards
        }

    let requestSets cache serializer =
        task {
            let! apiSets = Domain.getSetsFromApi serializer
            match apiSets with
            | Ok a ->
                return addToCache cache setsCacheKey a |> Ok
            | Error e ->
                return Error e
        }

    let getSets cache serializer =
        task {
            let cachedSets = getFromCache<PtcgSet seq>(cache) setsCacheKey
            match cachedSets with
            | Ok cs -> return Ok cs
            | Error _ -> return! requestSets cache serializer
        }

    let toCardResponse s c = Domain.mapPtcgCardToCard s c

    let getCardsByName serializer cache name =
        task {
            let sets' = getSets cache serializer
            let cards' = getCards cache serializer name

            let! sets = sets'
            let! cards = cards'

            let errorMessage = sprintf "An error occurred while searching for %s" name
            let result =
                sets
                |> Result.mapError (fun _ -> errorMessage)
                |> Result.bind (fun s ->
                    match cards with
                    | Ok cs -> cs |> Seq.map (toCardResponse s) |> Ok
                    | Error _ -> errorMessage |> Error)
            return result
        }

    let handleSuccess ctx name cards =
        if Seq.isEmpty cards then
            sprintf "No cards were found with name %s" name
            |> notFound ctx
        else
            Controller.json ctx cards

    let nameHandler =
        fun (ctx: HttpContext) name ->
            task {
                let serializer = getService<IJsonSerializer> ctx
                let cache = getService<IMemoryCache> ctx
                let! result = getCardsByName serializer cache name
                return!
                    match result with
                    | Ok cards -> handleSuccess ctx name cards
                    | Error e -> internalError ctx e
            }

    let controller = controller {
        show nameHandler
    }