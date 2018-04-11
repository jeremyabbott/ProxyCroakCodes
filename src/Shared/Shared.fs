namespace Shared

type Counter = int

type CardRequest = {
    Name : string
}

type Card = {
    Id : string
    Name : string
    ImageUrl : string
    Number : int
    SetCode : string
    Set : string
}

type CardResponse = Card list