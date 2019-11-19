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

let apiRouter = router {
  get "/search2" search2
  // get "/search2" (fun next ctx -> text "search2" next ctx)
  forward "/search" ProxyCroakCodes.Cards.Controller.controller
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