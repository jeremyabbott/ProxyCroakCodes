module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

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

type Model = Counter option

type Msg =
| Increment
| Decrement
| Init of Result<Counter, exn>



let init () : Model * Cmd<Msg> =
  let model = None
  let cmd =
    Cmd.ofPromise 
      (fetchAs<int> "/api/init") 
      [] 
      (Ok >> Init) 
      (Error >> Init)
  model, cmd

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
  let model' =
    match model,  msg with
    | Some x, Increment -> Some (x + 1)
    | Some x, Decrement -> Some (x - 1)
    | None, Init (Ok x) -> Some x
    | _ -> None
  model', Cmd.none

let safeComponents =
  let intersperse sep ls =
    List.foldBack (fun x -> function
      | [] -> [x]
      | xs -> x::sep::xs) ls []

  let components =
    [
      "Saturn", "https://saturnframework.github.io/docs/"
      "Fable", "http://fable.io"
      "Elmish", "https://fable-elmish.github.io/"
      "Fulma", "https://mangelmaxime.github.io/Fulma" 
      "Bulma\u00A0Templates", "https://dansup.github.io/bulma-templates/"
    ]
    |> List.map (fun (desc,link) -> a [ Href link ] [ str desc ] )
    |> intersperse (str ", ")
    |> span [ ]

  p [ ]
    [ strong [] [ str "SAFE Template" ]
      str " powered by: "
      components ]

let show = function
| Some x -> string x
| None -> "Loading..."

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
                [ Form.Input.Disabled true
                  Form.Input.Value (show model) ] ]
          Form.Control.p [ ]
            [ Button.a 
                [ Button.Color IsPrimary
                  Button.OnClick (fun _ -> dispatch Increment) ]
                [ str "+" ] ]
          Form.Control.p [ ]
            [ Button.a 
                [ Button.Color IsPrimary
                  Button.OnClick (fun _ -> dispatch Decrement) ]
                [ str "-" ] ] ] ]

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
                    [ str "SAFE Template" ]
                  div [ ClassName "subtitle" ]
                    [ safeComponents ]
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
