module Client

open Elmish
open Elmish.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fable.Core.JsInterop

open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Elements.Form
open Fulma.Components
open Fulma.BulmaClasses

open Fulma.BulmaClasses.Bulma
open Fulma.BulmaClasses.Bulma.Properties
open Fulma.Extra.FontAwesome

type Model =
    { CardResults : CardResponse option;
      SearchText : string option;
      Searching: bool;
      ErrorMessage: string }

type Msg =
| Init
| Search
| SetSearchText of string
| SearchSuccess of CardResponse
| SearchFailed of exn

let init () : Model * Cmd<Msg> =
    let model = { CardResults = None; SearchText = None; Searching = false; ErrorMessage = "" }
    let cmd = Cmd.none
    model, cmd

let search text =
    promise {
        match text with
        | None -> return! failwithf "Please enter a Pokemon name"
        | Some s ->
            let requestProperties =
                [ RequestProperties.Method HttpMethod.GET
                  Fetch.requestHeaders [
                      HttpRequestHeaders.ContentType "application/json" ]]
            let url = sprintf "/api/search/%s" s
            try
                return! Fetch.fetchAs<CardResponse> url requestProperties
            with _ -> return! failwithf "Could not find %s" s
    }

let searchCmd text =
  Cmd.ofPromise search text SearchSuccess SearchFailed
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match model, msg with
    | _, Search -> { model with Searching = true }, searchCmd model.SearchText
    | _, SetSearchText s -> { model with SearchText = Some s}, Cmd.none
    | _, SearchSuccess cs -> { model with CardResults = Some cs; Searching = false}, Cmd.none
    | _, SearchFailed exn -> { model with ErrorMessage = exn.Message; Searching = false}, Cmd.none
    | _ -> model, Cmd.none

let show = function
| Some x -> string x
| None -> "Enter a Pokemon name"

let navBrand =
  Navbar.Brand.div [ ]
    [ Navbar.Item.a
        [ Navbar.Item.Props [ Href "https://safe-stack.github.io/" ]
          Navbar.Item.IsActive true ]
        [ img [ Src "https://safe-stack.github.io/images/safe_top.png"
                Alt "Logo" ] ]
      Navbar.burger [ ]
        [ span [ ] [ ]
          span [ ] [ ]
          span [ ] [ ] ] ]

let navMenu =
  Navbar.menu [ ]
    [ Navbar.End.div [ ]
        [ Navbar.Item.a [ ]
            [ str "Home" ]
          Navbar.Item.a [ ]
            [ str "Examples" ]
          Navbar.Item.a [ ]
            [ str "Documentation" ]
          Navbar.Item.div [ ]
            [ Button.a
                [ Button.Color IsWhite
                  Button.IsOutlined
                  Button.Size IsSmall
                  Button.Props [ Href "https://github.com/SAFE-Stack/SAFE-template" ] ]
                [ Icon.faIcon [ ]
                    [ Fa.icon Fa.I.Github; Fa.fw ]
                  span [ ] [ str "View Source" ] ] ] ] ]

let card (c: Card) =
  Panel.block [] [
    sprintf "%s %s %s" c.Name c.PtcgoCode c.Number |> str
  ]

let cards (model : Model) (dispatch: Msg -> unit) =
    match model.CardResults with
    | Some cs ->
        let panels =
          cs
          |> Seq.toList
          |> List.map card

        let heading = Panel.heading [] [str "Search Results"]
        let panel = Panel.panel [GenericOption.CustomClass "results"] (heading::panels)
        panel

    | None ->
        div [] []

let containerBox (model : Model) (dispatch : Msg -> unit) =
  Box.box' [ ]
    [ Form.Field.div [ Form.Field.IsGrouped ]
        [ Form.Control.p [ Form.Control.CustomClass "is-expanded"]
            [ Form.Input.text
                [ Form.Input.Placeholder "Enter a Pokemon name"
                  Form.Input.Props
                    [ OnChange (fun ev -> dispatch (SetSearchText !!ev.target?value))
                      AutoFocus true ]] ]
          Form.Control.p [ ]
            [ Button.button
                [ Button.Color IsPrimary
                  Button.IsLoading model.Searching
                  Button.Disabled model.Searching
                  Button.OnClick (fun _ -> Search |> dispatch) ]
                [ str "Search" ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
  Hero.hero [ Hero.Color IsPrimary; Hero.IsFullHeight ]
    [ Hero.head [ ]
        [ Navbar.navbar [ ]
            [ Container.container [ ]
                [ navBrand
                  navMenu ] ] ]

      Hero.body [ ]
        [ Container.container [ Container.CustomClass Alignment.HasTextCentered ] [
            Columns.columns [] [
              Column.column
                  [ Column.Width (Column.All, Column.Is6)
                    Column.Offset (Column.All, Column.Is3) ]
                  [ h1 [ ClassName "title" ]
                      [ str "Proxy Croak Codes" ]
                    div [ ClassName "subtitle" ]
                      [ str "Find the codes you need to create your deck in Proxy Croak" ]
                    containerBox model dispatch ] ]
            div [ClassName "columns"] [
              Column.column
                [ Column.Width (Column.All, Column.Is6)
                  Column.Offset (Column.All, Column.Is3) ] [
                cards model dispatch
              ]
            ]]]]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
