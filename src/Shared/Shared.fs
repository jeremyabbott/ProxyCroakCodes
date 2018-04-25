namespace Shared

type CardSearchRequest = {
    Name : string
}

type PtcgCard = {
    Id : string
    Name : string
    ImageUrl : string
    Number : string
    SetCode : string
    Set : string
}

type Card = {
    Id : string
    Name : string
    ImageUrl : string
    Number : string
    PtcgoCode : string
    StandardLegal : bool
    SymbolUrl : string
    SetName : string
}

type CardResponse = Card list

type PtcgSet = {
    Code: string
    Name: string
    PtcgoCode: string
    Series: string
    StandardLegal: bool
    ExpandedLegal: bool
    SymbolUrl: string
    LogoUrl: string
}