package models

// ORDER BOOK
type OrderBook struct {
    Success bool    `json:"success"`
    Payload Payload `json:"payload"`
}
type Payload struct {
    Bids      []Order `json:"bids"`
    Asks      []Order `json:"asks"`
    UpdatedAt string  `json:"updated_at"`
    Sequence  string  `json:"sequence"`
}
type Order struct {
    Book   string `json:"book"`
    Price  string `json:"price"`
    Amount string `json:"amount"`
}

// TICKER
type Ticker struct {
    Success bool          `json:"success"`
    Payload TickerPayload `json:"payload"`
}
type TickerPayload struct {
    High   string `json:"high"`
    Last   string `json:"last"`
    Volume string `json:"volume"`
    Ask    string `json:"ask"`
    Bid    string `json:"bid"`
}

// TRADES
type Trades struct {
    Success bool    `json:"success"`
    Payload []Trade `json:"payload"`
}
type Trade struct {
    MakerSide string `json:"maker_side"`
    Book      string `json:"book"`
    CreatedAt string `json:"created_at"`
}
