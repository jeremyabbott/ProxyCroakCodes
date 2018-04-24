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
open Fable.Import.React
open Fable.Import
open Fable.Helpers.React.ReactiveComponents

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

type DisplayMode =
| Text
| Images

type Model =
    { CardResults : CardModel list option
      DisplayMode : DisplayMode
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
| SetDisplayMode of DisplayMode

let init () : Model * Cmd<Msg> =
    let model =
        { CardResults = None
          DisplayMode = Text
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
    | _, SetDisplayMode m -> { model with DisplayMode = m}, Cmd.none
    | _ -> model, Cmd.none

let show = function
| Some x -> string x
| None -> "Enter a Pokemon name"

let navBrand =
    Navbar.Brand.div [] [
        Navbar.Item.a [ Navbar.Item.Props [ Href "/" ] ]
            [ img [ Src "/Images/Pokeball-Transparent-Background.png"; Alt "Logo" ] ]
        Navbar.burger [ ] [
            span [ ] [ ]
            span [ ] [ ]
            span [ ] [ ]
        ]
    ]

let navMenu =
    Navbar.menu [] [
        Navbar.Start.a [] [
            Navbar.Item.a [] [str "Proxy Croak Codes"]
        ]
        Navbar.End.div [] [
            Navbar.Item.div [] [
                Button.a
                    [ Button.IsOutlined
                      Button.Size IsSmall
                      Button.Props [ Href "https://github.com/jeremyabbott/ProxyCroakCodes" ] ]
                    [ Icon.faIcon [ ]
                        [ Fa.icon Fa.I.Github; Fa.fw ]
                      span [ ] [ str "View Source" ] ]
            ]
        ]
    ]
let navBar =
    Navbar.navbar [] [
        navBrand
        navMenu
    ]

let proxyCroakCodeFormatter (c : Card) =
      sprintf "%s %s %s" c.Name c.PtcgoCode c.Number

let imageCard dispatch (c: CardModel) =
    let icon = if c.Selected then Fa.I.MinusSquare else Fa.I.PlusSquare
    let color = if c.Selected then IsDanger else IsPrimary
    let clickHandler = (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )
    [
        Column.column [
            Column.Width(Column.Desktop, Column.IsOneQuarter)
            Column.Width(Column.Mobile, Column.IsFull)
            ] [

            Card.card [] [
                Card.header [] [
                    p [ClassName "card-header-title"] [ proxyCroakCodeFormatter c.Card |> str]
                ]

                Card.content [GenericOption.CustomClass "is-flex is-horizontal-center"] [
                    Image.image [] [
                        img [ Src c.Card.ImageUrl]
                    ]
                ]
                Card.footer [] [
                        Button.a
                            [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "card-footer-item"]
                            [ Icon.faIcon [ ] [ Fa.icon icon; Fa.faLg ] ]
                ]
            ]
        ]
    ]

let textCard dispatch (c: CardModel) =
    let icon = if c.Selected then Fa.I.MinusSquare else Fa.I.PlusSquare
    let color = if c.Selected then IsDanger else IsPrimary
    let clickHandler = (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )
    [
        Column.column [Column.Width(Column.All, Column.IsFull)] [
             span [ClassName "is-pulled-left"] [proxyCroakCodeFormatter c.Card |> sprintf "  %s" |> str]
             span [ClassName "is-pulled-right"] [
                 Button.button
                    [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "control is-pulled-right"]
                    [ Icon.faIcon [ ] [ Fa.icon icon; Fa.faLg ] ]
             ]
        ]
    ]

let card mode dispatch (c: CardModel)  =
    match mode with
    | DisplayMode.Text -> textCard dispatch c
    | DisplayMode.Images -> imageCard dispatch c

let cardResultsView (model : Model) (dispatch: Msg -> unit) =
    match model.CardResults with
    | Some cs ->
        cs
        |> List.map (card model.DisplayMode dispatch)
        |> List.collect id
        |> Columns.columns [Columns.IsMobile; Columns.IsMultiline]
    | None ->
        p [] [str "There are no search results to display"]

let selectedCardsView  model dispatch =
    match model.SelectedCards with
    | [] -> p [] [str "You haven't selected any cards!"]
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

        let cardRow sc =
            [
                Column.column
                    [ Column.Width(Column.Mobile, Column.Is6)
                      Column.Width(Column.Tablet, Column.Is6)
                      Column.Width(Column.Desktop, Column.Is9) ]
                    [ sprintf "%s %s" (quantityElement sc) (formattedCodeElement sc.Card) |> str ]
                Column.column
                    [ Column.Width(Column.Mobile, Column.Is6)
                      Column.Width(Column.Tablet, Column.Is6)
                      Column.Width(Column.Desktop, Column.Is3)] [
                    span [ClassName "is-pulled-right"] [
                        incrementButton sc dispatch
                        decrementButton sc dispatch
                        deleteButton sc dispatch
                    ]

                ]
            ]

        scs
        |> List.map cardRow
        |> List.collect id
        |> Columns.columns [Columns.IsMultiline; Columns.IsMobile]

let tabsView (model: Model) (dispatch: Msg -> unit) =
    let active ta t =
        ta = t
    let tabView t =
        Tabs.tab
            [ Tabs.Tab.IsActive (active model.ActiveTab t)
              Tabs.Tab.Props [OnClick (fun _ -> TabSelected t |> dispatch)]]
            [ a [] [str t.Label] ]
    model.Tabs
    |> List.map tabView
    |> Tabs.tabs [ Tabs.Option.Size ISize.IsSmall; Tabs.Option.IsFullwidth; Tabs.Option.IsCentered ]

let contentView model dispatch =
    match model with
    | { CardResults = None; SelectedCards = []} -> div [ClassName "content"] []
    | _ ->
        let tabs = tabsView model dispatch
        let content =
            match model.ActiveTab.Type with
            | CardSearchResults -> cardResultsView model dispatch
            | SelectedCards -> selectedCardsView model dispatch
        Box.box' [] [tabs;content]

let containerBox (model : Model) (dispatch : Msg -> unit) =
    Content.content []
        [
            Heading.h1 [ Heading.Option.Is3; Heading.Option.CustomClass "has-text-centered" ] [str "Proxy Croak Codes"]
            form [] [
                Form.Field.div [ Form.Field.HasAddons;Form.Field.HasAddonsCentered] [
                    Form.Control.div [ ] [
                        Form.Input.text
                            [ Form.Input.Placeholder "Enter a Pokemon name"
                              Form.Input.Props
                                [ OnChange (fun ev ->
                                              ev.preventDefault()
                                              dispatch (SetSearchText !!ev.target?value))
                                  AutoFocus true ]
                            ]
                    ]
                    Form.Control.div [ ] [
                        Button.button
                          [ Button.Color IsPrimary
                            Button.IsLoading model.Searching
                            Button.Disabled (model.Searching || model.SearchText.IsNone)
                            Button.OnClick (fun ev -> ev.preventDefault(); Search |> dispatch) ]
                          [ str "Search" ]

                    ]
                ]
                Form.Field.div [ Form.Field.HasAddons;Form.Field.HasAddonsCentered] [
                    Form.Control.div [ ] [
                        Button.a
                          [ if model.DisplayMode = DisplayMode.Images then yield Button.Color IsPrimary
                            yield Button.Disabled (model.DisplayMode = DisplayMode.Text)
                            yield Button.IsFocused (model.DisplayMode = DisplayMode.Text)
                            yield Button.OnClick (fun ev -> ev.preventDefault(); SetDisplayMode DisplayMode.Text |> dispatch) ]
                          [
                              span [ClassName "icon"] [
                                  Icon.faIcon [ ] [ Fa.icon Fa.I.FileText ]
                              ]
                              span [] [str "Text Only"]
                          ]
                        Button.a
                          [ if model.DisplayMode = DisplayMode.Text then yield Button.Color IsPrimary
                            yield Button.Disabled (model.DisplayMode = DisplayMode.Images)
                            yield Button.IsFocused (model.DisplayMode = DisplayMode.Images)
                            yield Button.OnClick (fun ev -> ev.preventDefault(); SetDisplayMode DisplayMode.Images |> dispatch) ]
                          [
                              span [ClassName "icon"] [
                                  Icon.faIcon [ ] [ Fa.icon Fa.I.Image ]
                              ]
                              span [] [str "Images"]
                          ]
                    ]
                ]
            ]
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [] [
        navBar
        containerBox model dispatch
        contentView model dispatch
    ]


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
