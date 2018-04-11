namespace ProxyCroakCodes

open Saturn
open Shared

module Controller =
    let nameSearch =
        fun (ctx, name) ->
            [{  Name = name
                Id = "sm4-30"
                ImageUrl = "https://images.pokemontcg.io/sm4/30.png"
                Number = 30
                SetCode = "sm4"
                Set = "Crimson Invasion" }] |> Controller.json ctx

    let controller = controller {
        show nameSearch
    }