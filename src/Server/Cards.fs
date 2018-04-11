namespace ProxyCroakCodes

open Saturn
open Saturn.ControllerHelpers.Response
open Shared
open System.Net.Http
open FSharp.Control.Tasks.ContextInsensitive
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Caching.Memory

module Domain =

    type CardsApiResponse = {
        Cards: Card seq
    }
    let private baseUri = "https://api.pokemontcg.io/v1/"

    // needs to be disposed of at some point
    let private client = new HttpClient()
    client.BaseAddress <- baseUri |> System.Uri
    let private getByName' (client: HttpClient) (serializer : IJsonSerializer) name =
        task {
            let! res = client.GetStreamAsync(sprintf "cards?name=%s"name)
            let! cards = serializer.DeserializeAsync<CardsApiResponse>(res)
            return Ok cards.Cards
        }
    let getByName serializer name = getByName' client serializer name

module Controller =
    let private cacheKey = sprintf "cards:%s"
    let private getFromCache<'t> (cache: IMemoryCache) key =
        let success, entry = cache.TryGetValue<'t>(key)
        if success then Ok entry
        else sprintf "Key %s not found in cache" key |> Error

    let private addToCache (cache: IMemoryCache) key entry =
        let options = MemoryCacheEntryOptions().SetSlidingExpiration(System.TimeSpan.FromHours(1.))
        cache.Set(key, entry, options)

    let private getService<'t> (ctx: HttpContext) =
        ctx.RequestServices.GetService(typeof<'t>) :?> 't

    let nameSearch =
        fun ((ctx: HttpContext), name) ->
            task {
                let serializer = getService<IJsonSerializer> ctx
                let cache = getService<IMemoryCache> ctx
                let key = cacheKey name
                let cc = getFromCache<Card seq> cache (cacheKey name)
                let! cards =
                    match cc with
                    | Ok _ ->
                        task { return cc }
                    | Error _ ->
                        Domain.getByName serializer name

                match cards with
                | Ok c ->
                    // todo: only do this if there was a cache miss
                    // todo: add TaskResult builder?
                    addToCache cache key c |> ignore
                    return! Controller.json ctx c
                | Error _ ->
                    let message = sprintf "%s could not be found" name
                    return! notFound ctx message
            }

    let controller = controller {
        show nameSearch
    }