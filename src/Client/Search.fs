module Client.Search

open Shared
open Fable.Core.JsInterop
open Fable.PowerPack.PromiseImpl
open Fable.PowerPack.Fetch
open Fable.PowerPack
open Fable.Helpers.React
open Fable.Helpers.React.Props

open Elmish
open Fulma
open Fable.FontAwesome
open Thoth.Json

type CardModel = {
    Selected: bool
    Card: Card
    Quantity: int
}

type CardResults = CardModel list option

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

type SearchResultsModel =
    { CardResults : CardResults
      DisplayMode : DisplayMode
      SearchText : string option
      Searching: bool
      ErrorMessage: string
      SelectedCards: CardModel list
      Tabs: TabModel list
      ActiveTab: TabModel }

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

let searchResultsTab = { Label = "Search Results"; Type = CardSearchResults }
let selectedCardsTab = { Label = "Selected Cards"; Type = SelectedCards }

let tabs = [
    searchResultsTab
    selectedCardsTab
]
let init () =
    { CardResults = None
      DisplayMode = Text
      SearchText = None
      Searching = false
      ErrorMessage = ""
      SelectedCards = []
      Tabs = tabs
      ActiveTab = searchResultsTab }

let cardDecoder: Decode.Decoder<Card> =
    Decode.object (fun get ->
        {
            Id = get.Required.Field "Id" Decode.string
            Name = get.Required.Field "Name" Decode.string
            ImageUrl = get.Required.Field "ImageUrl" Decode.string
            Number = get.Required.Field "Number" Decode.string
            PtcgoCode = get.Optional.Field "PtcgoCode" Decode.string |> Option.defaultValue ""
            StandardLegal = get.Required.Field "StandardLegal" Decode.bool
            SymbolUrl = get.Required.Field "SymbolUrl" Decode.string
            SetName = get.Required.Field "SetName" Decode.string
        }
    )

let cardResponseDecoder: Decode.Decoder<CardResponse> =
    Decode.list cardDecoder

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
                let! response = Fetch.fetchAs<CardResponse> url cardResponseDecoder requestProperties
                return response |> List.map (fun c -> { Selected = false; Card = c; Quantity = 1})
            with ex ->
                do Fable.Import.Browser.console.log (sprintf "Error! %A" ex)
                return! failwithf "Could not find %s" ex.Message
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

let update (msg : Msg) (model : SearchResultsModel) : SearchResultsModel * Cmd<Msg> =
    match msg with
    | Search -> { model with Searching = true }, searchCmd model.SearchText
    | SetSearchText s ->
        let searchText = if s.Length = 0 then None else Some s
        { model with SearchText = searchText}, Cmd.none
    | SearchSuccess cs -> { model with CardResults = Some cs; Searching = false}, Cmd.none
    | SearchFailed exn -> { model with ErrorMessage = exn.Message; Searching = false}, Cmd.none
    | CardSelected c -> handleSelected model c, Cmd.none
    | CardRemoved c -> handleRemoved model c, Cmd.none
    | TabSelected t -> { model with ActiveTab = t }, Cmd.none
    | QuantityIncremented c -> handleIncrement model c, Cmd.none
    | QuantityDecremented c -> handleDecrement model c, Cmd.none
    | SetDisplayMode m -> { model with DisplayMode = m}, Cmd.none
    | _ -> model, Cmd.none

let proxyCroakCodeFormatter (c : Card) =
      sprintf "%s %s %s" c.Name c.PtcgoCode c.Number

let largeIcon i = Fa.i [i; Fa.Size Fa.FaLarge] []
let largeTrash = Fa.Solid.Trash |> largeIcon
let largeMinus = Fa.Solid.MinusSquare |> largeIcon
let largePlus = Fa.Solid.PlusSquare |> largeIcon

let imageCard dispatch (c: CardModel) =
    let icon =  if c.Selected then Fa.Regular.MinusSquare else Fa.Regular.PlusSquare
    let color = if c.Selected then IsDanger else IsPrimary
    let clickHandler = (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )
    [
        Column.column [
            Column.Width(Screen.Desktop, Column.IsOneQuarter)
            Column.Width(Screen.Mobile, Column.IsFull)
            ] [

            Card.card [] [
                Card.header [] [
                    p [ClassName "card-header-title"] [ proxyCroakCodeFormatter c.Card |> str]
                ]

                Card.content [GenericOption.CustomClass "is-flex stack"] [
                    div [ClassName "is-horizontal-center"] [
                        Image.image [] [
                            img [ Src c.Card.ImageUrl]
                        ]
                    ]
                ]
                Card.footer [] [
                        Button.a
                            [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "card-footer-item"]
                            [ Fa.i [icon; Fa.Size Fa.FaLarge] []]
                ]
            ]
        ]
    ]

let textCard dispatch (c: CardModel) =
    let icon = if c.Selected then Fa.Solid.MinusSquare else Fa.Solid.PlusSquare
    let color = if c.Selected then IsDanger else IsPrimary
    let clickHandler = (fun _ -> if c.Selected then CardRemoved c
                                       else CardSelected c
                                       |> dispatch )
    [
        Column.column [Column.Width(Screen.All, Column.IsFull)] [
            Media.media [] [
                Media.left [] [
                    Image.image [Image.Is24x24] [ img [Src c.Card.SymbolUrl]]
                ]
                Media.content [] [
                    proxyCroakCodeFormatter c.Card |> sprintf "  %s" |> str
                ]
                Media.right [] [
                    div [ClassName "is-pulled-right"] [
                        Button.button
                            [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "control"]
                            [ icon |> largeIcon ]
                    ]
                ]
            ]

            // Image.image [Image.Is24x24] [ img [Src c.Card.SymbolUrl]]
            // proxyCroakCodeFormatter c.Card |> sprintf "  %s" |> str
            // div [ClassName "is-pulled-right"] [
            //         Button.button
            //             [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "control"]
            //             [ Icon.faIcon [ ] [ Fa.icon icon; Fa.faLg ] ]
            // ]

            // Level.level [Level.Level.IsMobile] [
            //     Level.left [] [
            //         Level.item [] [Image.image [Image.Is24x24] [ img [Src c.Card.SymbolUrl]]]
            //         Level.item [] [
            //             proxyCroakCodeFormatter c.Card |> sprintf "  %s" |> str
            //         ]
            //     ]
            //     Level.right [] [
            //         Button.button
            //             [Button.Props [OnClick clickHandler]; Button.Color color; Button.CustomClass "control"]
            //             [ Icon.faIcon [ ] [ Fa.icon icon; Fa.faLg ] ]
            //     ]
            // ]
        ]
    ]

let card mode dispatch (c: CardModel)  =
    match mode with
    | DisplayMode.Text -> textCard dispatch c
    | DisplayMode.Images -> imageCard dispatch c

let cardResultsView (model : SearchResultsModel) (dispatch: Msg -> unit) =
    match model.CardResults with
    | Some cs ->
        cs
        |> List.map (card model.DisplayMode dispatch)
        |> List.collect id
        |> Columns.columns [Columns.IsMobile; Columns.IsMultiline]
    | None ->
        p [] [str "There are no search results to display"]

let selectedCardsView (model: SearchResultsModel) dispatch =

    let selectedCardText dispatch (sc: CardModel) =
        let quantityElement sc =
            sc.Quantity |> sprintf "%d"

        let deleteButton sc dispatch =
            Button.button
                [ Button.Color IsDanger;
                  Button.CustomClass "control"
                  Button.OnClick (fun _ -> CardRemoved sc |> dispatch)]
                [ largeTrash ]

        let incrementButton sc dispatch =
            Button.button
                [ Button.Color IsPrimary
                  Button.CustomClass "control"
                  Button.Disabled (sc.Quantity = 4)
                  Button.OnClick (fun _ -> QuantityIncremented sc |> dispatch)]
                [ largePlus ]

        let decrementButton sc dispatch =
            Button.button
                [ Button.Color IsPrimary
                  Button.CustomClass "control"
                  Button.OnClick (fun _ -> QuantityDecremented sc |> dispatch) ]
                [ largeMinus ]

        let formattedCodeElement c = proxyCroakCodeFormatter c

        [
                Column.column
                    [ Column.Width(Screen.Mobile, Column.Is6)
                      Column.Width(Screen.Tablet, Column.Is6)
                      Column.Width(Screen.Desktop, Column.Is9) ]
                    [ sprintf "%s %s" (quantityElement sc) (formattedCodeElement sc.Card) |> str ]
                Column.column
                    [ Column.Width(Screen.Mobile, Column.Is6)
                      Column.Width(Screen.Tablet, Column.Is6)
                      Column.Width(Screen.Desktop, Column.Is3)] [
                    span [ClassName "is-pulled-right"] [
                        incrementButton sc dispatch
                        decrementButton sc dispatch
                        deleteButton sc dispatch
                    ]

                ]
            ]

    let selectedCardImage dispatch (sc: CardModel) =
        let quantityElement sc =
            match sc.Quantity with
            | 1 -> sprintf "%d card selected" 1
            | q -> sprintf "%d cards selected" q

        let deleteButton sc dispatch =
            Button.a
                [ Button.Color IsDanger;
                  Button.CustomClass "card-footer-item"
                  Button.OnClick (fun _ -> CardRemoved sc |> dispatch)]
                [ largeTrash ]

        let incrementButton sc dispatch =
            Button.a
                [ Button.Color IsPrimary
                  Button.Disabled (sc.Quantity = 4)
                  Button.CustomClass "card-footer-item"
                  Button.OnClick (fun _ -> QuantityIncremented sc |> dispatch)]
                [ largePlus ]

        let decrementButton sc dispatch =
            Button.a
                [ Button.Color IsPrimary
                  Button.CustomClass "card-footer-item"
                  Button.OnClick (fun _ -> QuantityDecremented sc |> dispatch) ]
                [ largeMinus ]

        let formattedCodeElement c = proxyCroakCodeFormatter c
        [
            Column.column [
                Column.Width(Screen.Desktop, Column.IsOneQuarter)
                Column.Width(Screen.Mobile, Column.IsFull)
                ] [

                Card.card [] [
                    Card.header [] [
                        p [ClassName "card-header-title"] [ proxyCroakCodeFormatter sc.Card |> str]
                    ]

                    Card.content [GenericOption.CustomClass "is-flex stack"] [
                        div [ClassName "is-horizontal-center"] [
                            Image.image [] [
                                img [ Src sc.Card.ImageUrl]
                            ]
                        ]
                        p [ClassName "has-text-centered"] [
                            quantityElement sc
                            |> str
                        ]
                    ]
                    Card.footer [] [
                        incrementButton sc dispatch
                        decrementButton sc dispatch
                        deleteButton sc dispatch
                    ]
                ]
            ]
        ]

    let selectedCardView =
        match model.DisplayMode with
        | Text -> selectedCardText
        | Images -> selectedCardImage

    match model.SelectedCards with
    | [] -> p [] [str "You haven't selected any cards!"]
    | scs ->
        scs
        |> List.map (selectedCardView dispatch)
        |> List.collect id
        |> Columns.columns [Columns.IsMultiline; Columns.IsMobile]

let tabsView (model: SearchResultsModel) (dispatch: Msg -> unit) =
    let active ta t =
        ta = t
    let tabView t =
        Tabs.tab
            [ Tabs.Tab.IsActive (active model.ActiveTab t)
              Tabs.Tab.Props [OnClick (fun _ -> TabSelected t |> dispatch)]]
            [ a [] [str t.Label] ]
    model.Tabs
    |> List.map tabView
    |> Tabs.tabs [ Tabs.Option.Size ISize.IsSmall; Tabs.Option.IsFullWidth; Tabs.Option.IsCentered ]

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

let containerBox model (dispatch : Msg -> unit) =
    Box.box' []
        [
            Heading.h1 [ Heading.Option.Is3; Heading.Option.CustomClass "has-text-centered" ] [str "Proxy Croak Codes"]
            form [] [
                Field.div [ Field.HasAddons;Field.HasAddonsCentered] [
                    Control.div [ ] [
                        Input.text
                            [ Input.Placeholder "Enter a Pokemon name"
                              Input.Props
                                [ OnChange (fun ev ->
                                              ev.preventDefault()
                                              dispatch (SetSearchText !!ev.target?value))
                                  AutoFocus true ]
                            ]
                    ]
                    Control.div [ ] [
                        Button.button
                          [ Button.Color IsPrimary
                            Button.IsLoading model.Searching
                            Button.Disabled (model.Searching || model.SearchText.IsNone)
                            Button.OnClick (fun ev -> ev.preventDefault(); Search |> dispatch) ]
                          [ str "Search" ]

                    ]
                ]
                Field.div [ Field.HasAddons;Field.HasAddonsCentered] [
                    Control.div [ ] [
                        Button.a
                          [ if model.DisplayMode = DisplayMode.Images then yield Button.Color IsPrimary
                            yield Button.Disabled (model.DisplayMode = DisplayMode.Text)
                            yield Button.IsFocused (model.DisplayMode = DisplayMode.Text)
                            yield Button.OnClick (fun ev -> ev.preventDefault(); SetDisplayMode DisplayMode.Text |> dispatch) ]
                          [
                              span [ClassName "icon"] [
                                  Fa.i [ Fa.Solid.FileAlt ] [ ]]
                              span [] [str "Text Only"]
                          ]
                        Button.a
                          [ if model.DisplayMode = DisplayMode.Text then yield Button.Color IsPrimary
                            yield Button.Disabled (model.DisplayMode = DisplayMode.Images)
                            yield Button.IsFocused (model.DisplayMode = DisplayMode.Images)
                            yield Button.OnClick (fun ev -> ev.preventDefault(); SetDisplayMode DisplayMode.Images |> dispatch) ]
                          [
                              span [ClassName "icon"] [
                                  Fa.i [ Fa.Solid.Image ] [ ]
                              ]
                              span [] [str "Images"]
                          ]
                    ]
                ]
            ]
        ]

let view model dispatch =
    [ containerBox model dispatch
      contentView model dispatch ]