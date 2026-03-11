package api

import (
    "encoding/json"
    "fmt"
    "io/ioutil"
    "log"
    "net/http"
    "strings"
    "consulta_cotizacion_usd_ars/internal/mails"
    "consulta_cotizacion_usd_ars/internal/models"
)

func Consultar(url string, currency string) {
    resp, err := http.Get(fmt.Sprintf("%s?book=%s", url, currency))
    if err != nil {
        log.Printf("{\"event\":\"error\",\"endpoint\":\"%s\",\"currency\":\"%s\",\"message\":\"%s\"}", url, currency, err.Error())
        return
    }
    defer resp.Body.Close()

    body, _ := ioutil.ReadAll(resp.Body)

    switch {
    case strings.Contains(url, "order_book"): 
        var ob models.OrderBook
        if err := json.Unmarshal(body, &ob); err != nil {
            log.Printf("{\"event\":\"error\",\"message\":\"error parseando order_book: %s\"}", err.Error())
            return
        }
        log.Printf("ORDER_BOOK %s actualizado en %s", currency, ob.Payload.UpdatedAt)

        if len(ob.Payload.Bids) > 0 && len(ob.Payload.Asks) > 0 {
            var bidPrice, askPrice float64
            fmt.Sscanf(ob.Payload.Bids[0].Price, "%f", &bidPrice)
            fmt.Sscanf(ob.Payload.Asks[0].Price, "%f", &askPrice)
            log.Printf("Bid: %.2f Ask: %.2f Book: %s", bidPrice, askPrice, ob.Payload.Bids[0].Book)

            if currency == "usd_ars" && bidPrice > 1563.5 {
                mail.EnviarMail(bidPrice, askPrice, "0", "0", "0", "0", "0", ob.Payload.UpdatedAt, currency, 0, 0)
            }
        }

    case strings.Contains(url, "ticker"):
        var tk models.Ticker
        if err := json.Unmarshal(body, &tk); err != nil {
            log.Printf("{\"event\":\"error\",\"message\":\"error parseando ticker: %s\"}", err.Error())
            return
        }
        log.Printf("TICKER %s -> High: %s, Last: %s, Vol: %s, Ask: %s, Bid: %s",
            currency, tk.Payload.High, tk.Payload.Last, tk.Payload.Volume, tk.Payload.Ask, tk.Payload.Bid)

        var highVal float64
        fmt.Sscanf(tk.Payload.High, "%f", &highVal)

        if (currency == "btc_usd" && highVal > 99000.0) || (currency == "aave_usd" && highVal > 210.0) {
            mail.EnviarMail(0.0, 0.0, tk.Payload.High, tk.Payload.Last, tk.Payload.Volume,
                tk.Payload.Ask, tk.Payload.Bid, "N/A", currency, 0, 0)
        }

    case strings.Contains(url, "trades"):
        var tr models.Trades
        if err := json.Unmarshal(body, &tr); err != nil {
            log.Printf("{\"event\":\"error\",\"message\":\"error parseando trades: %s\"}", err.Error())
            return
        }
        countSell := 0
        countBuy := 0
        lastBook := ""
        for _, t := range tr.Payload {
            if t.MakerSide == "sell" {
                countSell++
            } else if t.MakerSide == "buy" {
                countBuy++
            }
            lastBook = t.Book
        }
        log.Printf("Total SELL: %d, BUY: %d, to %s", countSell, countBuy, lastBook)
    }
}
