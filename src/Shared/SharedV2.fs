namespace Shared.V2

[<CLIMutable>]
type CardSearchRequest = {
    Name : string
    StandardOnly: bool
    ExpandedOnly: bool
} with
    override this.ToString() =
        sprintf "Name: %s; StandardOnly: %b; ExpandedOnly: %b" this.Name this.StandardOnly this.ExpandedOnly