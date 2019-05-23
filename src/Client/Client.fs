module Client.App

open Elmish
open Elmish.Navigation
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Pages

open Fulma
open Browser.Types

type PageModel =
| Home of Client.Search.SearchResultsModel
| About

type Model =
    { PageModel: PageModel
      BurgerActive: bool
      SearchResults: Client.Search.SearchResultsModel }

type Msg =
| BurgerClicked
| SearchMsg of Client.Search.Msg

// Navigation Stuff

let handleNotFound (model: Model) =
    ( model, Navigation.modifyUrl (toHash Page.Home) )

let urlUpdate (result:Page option) (model: Model) : Model * Cmd<Msg> =
    match result with
    | None ->
        handleNotFound model
    | Some Page.About ->
        { model with PageModel = About}, Cmd.none
    | Some Page.Home ->
        { model with PageModel = Home model.SearchResults }, Cmd.none

let goToUrl (e: MouseEvent)  =
    e.preventDefault()
    let href = !!e.target?href
    Navigation.newUrl href |> List.map (fun f -> f ignore) |> ignore

let init result : Model * Cmd<Msg> =
    let searchResults = Client.Search.init()
    let model =
        { SearchResults = searchResults
          BurgerActive = false
          PageModel = Home searchResults }

    urlUpdate result model

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg with
    | SearchMsg m ->
        let sm, m = Client.Search.update m model.SearchResults
        { model with SearchResults = sm; PageModel = Home sm}, Cmd.map SearchMsg m
    | BurgerClicked ->
        { model with BurgerActive = not model.BurgerActive}, Cmd.none

let show = function
| Some x -> string x
| None -> "Enter a Pokemon name"

let navBrand model dispatch =
    let active = if model.BurgerActive then "is-active" else ""

    Navbar.Brand.div [] [
        Navbar.Item.a [ Navbar.Item.Props [ Pages.toHash Page.Home |> Href ] ]
            [ img [ Src "/Images/Pokeball-Transparent-Background.png"; Alt "Logo" ] ]
        Navbar.burger [ GenericOption.CustomClass active; GenericOption.Props[OnClick (fun _ -> dispatch BurgerClicked)]] [
            span [ ] [ ]
            span [ ] [ ]
            span [ ] [ ]
        ]
    ]

let navMenu model =
    Navbar.menu [Navbar.Menu.IsActive model.BurgerActive] [
        Navbar.Start.div [] [
            Navbar.Item.a [

                Navbar.Item.Option.Props [
                    Pages.toHash Page.Home |> Href
                    OnClick goToUrl
                ]
            ] [str "Proxy Croak Codes"]
        ]
        Navbar.End.div [] [
            Navbar.Item.div [] [
                Button.a [
                    Button.IsOutlined
                    Button.Size IsSmall
                    Button.OnClick goToUrl
                    Button.Props [ Pages.toHash Page.About |> Href ] ]
                    [ str "About"]
                Button.a
                    [ Button.IsOutlined
                      Button.Size IsSmall
                      Button.Props [ Href "https://github.com/jeremyabbott/ProxyCroakCodes" ] ]
                    [ Icon.icon [] [i [ClassName "fab fa-github "] []]
                      span [ ] [ str "View Source" ] ]
            ]
        ]
    ]

let navBar model dispatch =
    Navbar.navbar [] [
        navBrand model dispatch
        navMenu model
    ]

let view (model : Model) (dispatch : Msg -> unit) =
    let content =
        match model.PageModel with
        | Home m -> Client.Search.view m (SearchMsg >> dispatch)
        | About -> [Client.About.view model dispatch]
    div [] [
        navBar model dispatch
        div [] content
    ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
|> Program.toNavigable urlParser urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
