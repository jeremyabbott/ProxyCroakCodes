module Client

open Elmish
open Elmish.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import.React
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

type CardModel = {
    Selected: bool
    Card: Card
}

type Model =
    { CardResults : CardModel list option
      SearchText : string option
      Searching: bool
      ErrorMessage: string
      SelectedCards: Card list }

type Msg =
| Init
| Search
| SetSearchText of string
| SearchSuccess of CardModel list
| SearchFailed of exn
| CardSelected of CardModel
| CardRemoved of CardModel

let init () : Model * Cmd<Msg> =
    let model = { CardResults = None; SearchText = None; Searching = false; ErrorMessage = ""; SelectedCards = [] }
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
                let! response = Fetch.fetchAs<CardResponse> url requestProperties
                return response |> List.map (fun c -> { Selected = false; Card = c})
            with _ -> return! failwithf "Could not find %s" s
    }

let searchCmd text =
    Cmd.ofPromise search text SearchSuccess SearchFailed

let setCardSelected cards selectedCard =
    match cards with
        | Some mcr ->
            mcr
            |> List.map (fun sc ->
                            match sc with
                            | sc when sc = selectedCard -> { sc with Selected = not selectedCard.Selected }
                            | _ -> sc) |> Some
        | None -> None

let handleSelected model selectedCard =
    let updatedCardResults = setCardSelected model.CardResults selectedCard
    let updatedSelected = selectedCard.Card :: model.SelectedCards
    { model with CardResults = updatedCardResults; SelectedCards = updatedSelected}

let handleRemoved model removedCard =
    let updatedCardResults = setCardSelected model.CardResults removedCard
    let updatedSelected = model.SelectedCards |> List.filter (fun c -> (c <> removedCard.Card))
    { model with CardResults = updatedCardResults; SelectedCards = updatedSelected}

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match model, msg with
    | _, Search -> { model with Searching = true }, searchCmd model.SearchText
    | _, SetSearchText s ->
        let searchText = if s.Length = 0 then None else Some s
        { model with SearchText = searchText}, Cmd.none
    | _, SearchSuccess cs -> { model with CardResults = Some cs; Searching = false}, Cmd.none
    | _, SearchFailed exn -> { model with ErrorMessage = exn.Message; Searching = false}, Cmd.none
    | _, CardSelected c -> handleSelected model c, Cmd.none
    | _, CardRemoved c -> handleRemoved model c, Cmd.none
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

let card dispatch (c: CardModel)  =
    let icon = if c.Selected then "fa fa-minus-square" else "fa fa-plus-square"
    Panel.block [] [
      Panel.icon [ GenericOption.Props
                    [OnClick (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )]]
                 [ i [ClassName icon][]]
      sprintf "%s %s %s" c.Card.Name c.Card.PtcgoCode c.Card.Number |> str
    ]

let cards (model : Model) (dispatch: Msg -> unit) =
    match model.CardResults with
    | Some cs ->
        let panels =
          cs
          |> List.map (card dispatch)

        let heading = Panel.heading [] [str "Search Results"]
        let panel = Panel.panel [GenericOption.CustomClass "results"] (heading::panels)
        panel

    | None ->
        div [] []

let containerBox (model : Model) (dispatch : Msg -> unit) =
  Box.box' []
    [ form [] [
        Form.Field.div [ Form.Field.IsGrouped ]
          [ Form.Control.p [ Form.Control.CustomClass "is-expanded"]
              [ Form.Input.text
                  [ Form.Input.Placeholder "Enter a Pokemon name"
                    Form.Input.Props
                      [ OnChange (fun ev ->
                                      ev.preventDefault()
                                      dispatch (SetSearchText !!ev.target?value))
                        AutoFocus true ]] ]
            Form.Control.p [ ]
              [ Button.button
                  [ Button.Color IsPrimary
                    Button.IsLoading model.Searching
                    Button.Disabled (model.Searching || model.SearchText.IsNone)
                    Button.OnClick (fun ev -> ev.preventDefault(); Search |> dispatch) ]
                  [ str "Search" ] ] ]
        ]
    ]

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
