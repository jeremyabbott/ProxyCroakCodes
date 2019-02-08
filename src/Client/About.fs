module Client.About
open Fulma
open Fable.Helpers.React
open Fable.Helpers.React.Props

let blankA url text =
    a [Href url; Target "_blank"] [str text]

let safeStack = a [Href "https://safe-stack.github.io/"; Target "_blank"] [str "SAFE Stack"]
let proxyCroak = a [Href "https://proxycroak.com"; Target "_blank"] [str "Proxy Croak"]

let why = [
    p [] [str "I recently started playing the Pokemon Trading Card Game again. The last time I played was in 2000 or so. Starting out one may not the cards they need to build a competitive deck. Getting those cards can be expensive and time consuming. To allow players to experiment with different deck ideas without spending a bunch of money they can use \"proxy cards\"."]
    br []
    p [] [
        str "This is where "
        a [Href "https://proxycroak.com"] [str "Proxy Croak"]
        str " comes in. Proxy Croak lets you list out the cards you want by name and set code. But what if, like me, you're new and don't know anything about set codes? This is one of the reasons I made \"Proxy Croak Codes.\""]
    br []
    p [] [
        str "The primary goal of \"Proxy Croak Codes\" is to allow you to search for Pokemon cards by name, view them, and get the set codes you need to use "
        a [Href "https://proxycroak.com"] [str "Proxy Croak"]
        str ". After finding the card you want, you can select it, and copy the formatted code to use in Proxy Croak."
    ]
]

let how = [
    ol [] [
        li [] [str "Search for a card like \"Pikachu\""]
        li [] [str "The text search results are already formatted for Proxy Croak"]
        li [] [str "If you need more information, change the search result format to \"Images\""]
        li [] [str "After you find the card you want, click the [+} button"]
        li [] [str "Switch over to \"Selected Cards\" to view your cards"]
        li [] [
            str "From \"Selected Cards\" you can highlight all your cards and paste the content directly into "
            proxyCroak
            str "."]
        li [] [
            str "From \"Seleced Cards\" you can increment/decrement the number of cards you need."
        ]
    ]
]

let theTech = [
    p [] [
        str "The other reason I made Proxy Croak Codes is so I can learn more about the super awesome F# "
        safeStack
        str ". The SAFE Stack allows your to write full-stack, functional first, web applications in F#. The server uses "
        blankA "https://saturnframework.github.io/docs/" "Saturn"
        str ", and the client uses "
        blankA "http://fable.io/" "Fable"
        str ", and "
        blankA "https://elmish.github.io/elmish/" "Elmish."
        str " It's hosted as a "
        blankA "https://docs.microsoft.com/en-us/azure/app-service/containers/tutorial-custom-docker-image" "WebApp for Containers"
        str " on Azure."
    ]
]

let view _ _ =
    Section.section [] [
        Container.container [] [
            Heading.h1 [] [
                str "About Proxy Croak Codes"
            ]
            Heading.h2 [] [str "TLDR;"]
            article [] [
                str "A good idea, "
                a [Href "https://proxycroak.com"; Target "_blank"] [str "based on a good idea"]
                str ", "
                a [Href "http://metadeck.me/"; Target "_blank"] [str "based on a good idea"]
                str ", "
                a [Href "https://pokemontcg.io/"; Target "_blank"] [str "via the Pokemon TCG API."]
            ]
            br []
            Heading.h2 [] [str "Why?"]
            article [] why
            br []
            Heading.h2 [] [str "How it Works"]
            article [] how
            br []
            Heading.h2 [] [str "The Tech"]
            article [] theTech
        ]
    ]