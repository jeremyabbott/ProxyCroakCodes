namespace ProxyCroakCodes
open System.Net.Http
open Giraffe.Serialization
open FSharp.Control.Tasks.ContextInsensitive
open Shared

module Domain =
    type CardsApiResponse = {
        Cards: PtcgCard seq
    }

    type PtcgSetResponse = {
        Sets: PtcgSet seq
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

    let private getSets' (client: HttpClient) (serializer : IJsonSerializer) =
        task {
            try
                let! res = client.GetStreamAsync("sets")
                let! sets = serializer.DeserializeAsync<PtcgSetResponse>(res)
                return Ok sets.Sets
            with
            | ex -> return Error ex.Message
        }

    let getCardsFromApiByName serializer name = getByName' client serializer name

    let getSetsFromApi serializer = getSets' client serializer

    let getPtcgoSetCode (p:PtcgCard) (sets: PtcgSet seq) =
        sets
        |> Seq.find (fun s -> s.Code = p.SetCode)

    let ptcgCardToCard (p : PtcgCard) set =
        {   Id = p.Id
            Name = p.Name
            ImageUrl = p.ImageUrl
            Number = p.Number
            PtcgoCode = set.PtcgoCode
            Set = p.Set }

    let mapPtcgCardToCard sets pCard =
       getPtcgoSetCode pCard sets
       |> ptcgCardToCard pCard