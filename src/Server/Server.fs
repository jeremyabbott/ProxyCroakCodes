open System.IO

open Giraffe
open Saturn

open Giraffe.Serialization
open Microsoft.Extensions.DependencyInjection

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

let browserRouter = router {
  get "/" (htmlFile (Path.Combine(publicPath, "index.html")))
}

let config (services:IServiceCollection) =
  let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
  fableJsonSettings.Converters.Add(Fable.JsonConverter())
  services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)
    .AddMemoryCache()

let search2: HttpHandler =
  fun next ctx ->
    let query = ctx.BindQueryString<Shared.V2.CardSearchRequest>()
    json query next ctx

let v2Router = router {
  // get "/search2" (fun next ctx -> text "search2" next ctx)
  get "/search" search2
  pipe_through (setHttpHeader "X-Api-Version" "2")
}

let apiRouter = router {
  forward "/v2" v2Router
  forward "/search" ProxyCroakCodes.Cards.Controller.controller
  case_insensitive
}



let mainRouter = router {
  forward "" browserRouter
  forward "/api" apiRouter
}

let app = application {
    use_router mainRouter
    url ("http://0.0.0.0:" + port.ToString() + "/")

    use_static publicPath
    service_config config
    use_gzip
}

run app