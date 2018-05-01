module Client.Pages

open Elmish.Browser.UrlParser

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Page =
    | Home
    | About

let toPath =
    function
    | Page.Home -> "/"
    | Page.About -> "/about"


/// The URL is turned into a Result.
let pageParser : Parser<Page -> Page,_> =
    oneOf
        [ map Page.Home (s "")
          map Page.About (s "about") ]

let urlParser location = parsePath pageParser location