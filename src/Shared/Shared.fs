namespace Shared

type Counter = int

type CardRequest = {
    Name : string
}

type Card = {
    Id : string
    Name : string
    ImageUrl : string
    Number : string
    SetCode : string
    Set : string
}

type CardResponse = Card seq