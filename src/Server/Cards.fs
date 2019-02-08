namespace ProxyCroakCodes.Cards

open Saturn
open Saturn.ControllerHelpers.Response
open Shared
open FSharp.Control.Tasks.V2
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Caching.Memory
open ProxyCroakCodes
open Microsoft.Extensions.Logging

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
    let private getSets cache serializer =
        task {
            let cachedSets = getFromCache<PtcgSet seq>(cache) setsCacheKey
            match cachedSets with
            | Ok cs -> return Ok cs
            | Error _ ->
                let! apiSets = Domain.getSetsFromApi serializer
                match apiSets with
                | Ok a ->
                    addToCache cache setsCacheKey a |> ignore
                    return Ok a
                | Error e ->
                    return Error e
        }

    let nameSearch =
        fun (ctx: HttpContext) name ->
            task {
                let serializer = getService<IJsonSerializer> ctx
                let cache = getService<IMemoryCache> ctx

                let! sets = getSets cache serializer
                let! cards = getCards cache serializer name

                let response =
                    match sets, cards with
                    | Ok s, Ok cs ->
                        let cards =
                            cs
                            |> Seq.toList
                            |> List.map (fun c -> Domain.mapPtcgCardToCard s c)
                        Controller.json ctx cards
                    | Error _, _ ->
                        let message = sprintf "Card sets could not be found!"
                        notFound ctx message
                    | _, Error _ ->
                        let message = sprintf "Cards with name %s could not be found!" name
                        notFound ctx message
                return! response
            }

    let controller = controller {
        show nameSearch
    }