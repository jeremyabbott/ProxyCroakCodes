module Client.Pages

open Elmish.UrlParser

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Page =
    | Home
    | About

let toHash =
    function
        | Page.Home -> "#home"
        | Page.About -> "#about"

/// The URL is turned into a Result.
let pageParser : Parser<Page -> Page,_> =
    oneOf
        [ map Page.Home (s "home")
          map Page.About (s "about") ]

let urlParser location = parseHash pageParser location