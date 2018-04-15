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
open Fulma.Components
open Fulma.BulmaClasses.Bulma.Properties
open Fulma.Extra.FontAwesome

type CardModel = {
    Selected: bool
    Card: Card
    Quantity: int
}

type TabType =
| CardSearchResults
| SelectedCards

type TabModel = {
    Label: string
    Type: TabType
}

type Model =
    { CardResults : CardModel list option
      SearchText : string option
      Searching: bool
      ErrorMessage: string
      SelectedCards: CardModel list
      Tabs: TabModel list
      ActiveTab: TabModel }


let searchResultsTab = { Label = "Search Results"; Type = CardSearchResults }
let selectedCardsTab = { Label = "Selected Cards"; Type = SelectedCards }

let tabs = [
    searchResultsTab
    selectedCardsTab
]

type Msg =
| Init
| Search
| SetSearchText of string
| SearchSuccess of CardModel list
| SearchFailed of exn
| CardSelected of CardModel
| CardRemoved of CardModel
| TabSelected of TabModel
| QuantityIncremented of CardModel
| QuantityDecremented of CardModel

let init () : Model * Cmd<Msg> =
    let model =
        { CardResults = None
          SearchText = None
          Searching = false
          ErrorMessage = ""
          SelectedCards = []
          Tabs = tabs
          ActiveTab = searchResultsTab }
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
                return response |> List.map (fun c -> { Selected = false; Card = c; Quantity = 1})
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

    let updatedSelected =
        { Card = selectedCard.Card; Quantity = 1; Selected = true }  :: model.SelectedCards
    { model with CardResults = updatedCardResults; SelectedCards = updatedSelected}

let handleRemoved model removedCard =
    let updatedCardResults = setCardSelected model.CardResults removedCard
    let updatedSelected = model.SelectedCards |> List.filter (fun c -> (c.Card <> removedCard.Card))
    { model with CardResults = updatedCardResults; SelectedCards = updatedSelected}

let handleIncrement model card =
    let updatedCards =
        model.SelectedCards
        |> List.map (fun c ->
            if c <> card then c
            else
                { c with Quantity = c.Quantity + 1})
    {model with SelectedCards = updatedCards}

let handleDecrement model card =
    let updatedCards =
        model.SelectedCards
        |> List.map (fun c ->
            if c <> card then c
            else { c with Quantity = c.Quantity - 1})
        |> List.filter (fun c -> c.Quantity > 0)
    let updatedResults =
        model.CardResults.Value
        |> List.map (fun c ->
            if c.Quantity = 1 && c = card then { c with Selected = false} else c)
    { model with SelectedCards = updatedCards; CardResults = Some updatedResults }

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
    | _, TabSelected t -> { model with ActiveTab = t }, Cmd.none
    | _, QuantityIncremented c -> handleIncrement model c, Cmd.none
    | _, QuantityDecremented c -> handleDecrement model c, Cmd.none
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

let proxyCroakCodeFormatter (c : Card) =
      sprintf "%s %s %s" c.Name c.PtcgoCode c.Number

let card dispatch (c: CardModel)  =
    let icon = if c.Selected then Fa.I.MinusSquare else Fa.I.PlusSquare
    let color = if c.Selected then IsDanger else IsPrimary
    let clickHandler = (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )
    Panel.block [] [
        div [ClassName "is-grouped"] [
            Button.button
                [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "control"]
                [ Icon.faIcon [ ] [ Fa.icon icon; Fa.faLg ] ] ]
        div [] [
            proxyCroakCodeFormatter c.Card |> sprintf "  %s" |> str]
    ]

let cardResultsView (model : Model) (dispatch: Msg -> unit) =
    match model.CardResults with
    | Some cs ->
        cs |> List.map (card dispatch)
    | None ->
        [Panel.block [] [str "There are no search results to display"]]

let selectedCardsView  model dispatch =

    match model.SelectedCards with
    | [] -> [ Panel.block [] [str "You haven't selected any cards!"] ]
    | scs ->
        let quantityElement sc =
            sc.Quantity |> sprintf "%d"

        let deleteButton sc dispatch =
            Button.button
                [ Button.Color IsDanger;
                  Button.CustomClass "control"
                  Button.OnClick (fun _ -> CardRemoved sc |> dispatch)]
                [ Icon.faIcon [ ] [ Fa.icon Fa.I.Trash; Fa.faLg ] ]

        let incrementButton sc dispatch =
            Button.button
                [ Button.Color IsPrimary
                  Button.CustomClass "control"
                  Button.OnClick (fun _ -> QuantityIncremented sc |> dispatch)]
                [ Icon.faIcon [ ] [ Fa.icon Fa.I.PlusSquare; Fa.faLg ] ]

        let decrementButton sc dispatch =
            Button.button
                [ Button.Color IsPrimary
                  Button.CustomClass "control"
                  Button.OnClick (fun _ -> QuantityDecremented sc |> dispatch) ]
                [ Icon.faIcon [ ] [ Fa.icon Fa.I.MinusSquare; Fa.faLg ] ]

        let formattedCodeElement c = proxyCroakCodeFormatter c

        let columns lc rc =
            Columns.columns [Columns.IsMobile] [
                Column.column [Column.Width (Column.All, Column.IsTwoFifths)] [div [ClassName "field is-grouped"] lc]
                Column.column [Column.Width (Column.All, Column.IsThreeFifths)] [div [] rc]
            ]

        let panelBlock dispatch sc =
            let text =
                sprintf "%s %s" (quantityElement sc) (formattedCodeElement sc.Card)
                |> str
            Panel.block [] [
                columns
                    [incrementButton sc dispatch
                     decrementButton sc dispatch
                     deleteButton sc dispatch]
                    [span [ClassName "has-text-left"] [text]]]

        scs
        |> List.map (panelBlock dispatch)

let tabsView (model: Model) (dispatch: Msg -> unit) =
    let active ta t =
        ta = t
    let tabView t =
        Panel.tab
            [ Panel.Tab.IsActive (active model.ActiveTab t)
              Panel.Tab.Props [OnClick (fun _ -> TabSelected t |> dispatch)]]
            [ str t.Label ]
    model.Tabs
    |> List.map tabView

let panelsView model dispatch =
    let activePanel =
        match model.ActiveTab.Type with
        | CardSearchResults -> cardResultsView model dispatch
        | SelectedCards -> selectedCardsView model dispatch

    let ts = Panel.tabs [] (tabsView model dispatch)
    let panels = Panel.panel [GenericOption.CustomClass "results"] (ts::activePanel)
    panels

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
                panelsView model dispatch
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
