module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
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
    { CardResults : CardResponse option; SearchText : string option }

type Msg =
| Init
| Search of string option
| SetSearchText of string



let init () : Model * Cmd<Msg> =
  let model = { CardResults = None; SearchText = None }
  let cmd = Cmd.none
    // Cmd.ofPromise
    //   (fetchAs<int> "/api/init")
    //   []
    //   (Ok >> Init)
    //   (Error >> Init)
  model, cmd

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
  let model' =
    match model,  msg with
    | _, Search s -> { model with SearchText = s }
    | _, SetSearchText s -> { model with SearchText = Some s}
    | _ -> model
  model', Cmd.none

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

let containerBox (model : Model) (dispatch : Msg -> unit) =
  Box.box' [ ]
    [ Form.Field.div [ Form.Field.IsGrouped ]
        [ Form.Control.p [ Form.Control.CustomClass "is-expanded"]
            [ Form.Input.text
                [ Form.Input.Placeholder "Enter a Pokemon name"
                  // Form.Input.Value (show model.SearchText)
                  Form.Input.Props [ OnBlur (fun ev -> dispatch (SetSearchText !!ev.target?value))]] ]
          Form.Control.p [ ]
            [ Button.a
                [ Button.Color IsPrimary
                  Button.OnClick (fun _ -> Search model.SearchText |> dispatch) ]
                [ str "search" ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
  Hero.hero [ Hero.Color IsPrimary; Hero.IsFullHeight ]
    [ Hero.head [ ]
        [ Navbar.navbar [ ]
            [ Container.container [ ]
                [ navBrand
                  navMenu ] ] ]

      Hero.body [ ]
        [ Container.container [ Container.CustomClass Alignment.HasTextCentered ]
            [ Column.column
                [ Column.Width (Column.All, Column.Is6)
                  Column.Offset (Column.All, Column.Is3) ]
                [ h1 [ ClassName "title" ]
                    [ str "Proxy Croak Codes" ]
                  div [ ClassName "subtitle" ]
                    [ str "Find the codes you need to create your deck in Proxy Croak" ]
                  containerBox model dispatch ] ] ] ]


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
