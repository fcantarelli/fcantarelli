package main

import (
    "log"
    "sync"
    "time"

    "consulta_cotizacion_usd_ars/internal/api"
)

func main() {
    vecCurrency := []string{"usd_ars", "btc_usd", "btc_ars", "aave_usd", "avax_usd", "pepe_usd"}
    vecUrl := []string{
        "https://api.bitso.com/api/v3/order_book",
        "https://api.bitso.com/api/v3/trades",
        "https://api.bitso.com/api/v3/ticker",
    }

    for {
        var wg sync.WaitGroup
        for _, url := range vecUrl {
            for _, currency := range vecCurrency {
                wg.Add(1)
                go func(u, c string) {
                    defer wg.Done()
                    api.Consultar(u, c)
                }(url, currency)
            }
        }
        wg.Wait()
        log.Println("#------------------------------ query ended OK ------------------------------#")
        time.Sleep(3 * time.Minute)
    }
}
