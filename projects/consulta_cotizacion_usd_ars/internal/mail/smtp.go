package mail

import (
    "fmt"
    "log"
    "net/smtp"
    "os"
)

func EnviarMail(bidPrice float64, askPrice float64, high string, last string, volume string,
    ask string, bid string, dtUTC3 string, book string, countSell int, countBuy int) {

    cuerpo := fmt.Sprintf(
        "Cotización actual:\nBid Price: %.2f\nAsk Price: %.2f\nHigh: %s\nLast: %s\nVolume: %s\nAsk: %s\nBid: %s\nÚltima operación (UTC-3): %s\nBook: %s\nTotal SELL: %d\nTotal BUY: %d",
        bidPrice, askPrice, high, last, volume, ask, bid, dtUTC3, book, countSell, countBuy)

    from := "sender@yahoo.com.ar"
    pass := os.Getenv("YAHOO_APP_PASSWORD")
    to := "addressee@gmail.com"

    msg := "Subject: bitso_price_alert\r\n\r\n" + cuerpo

    err := smtp.SendMail("smtp.mail.yahoo.com:587",
        smtp.PlainAuth("", from, pass, "smtp.mail.yahoo.com"),
        from, []string{to}, []byte(msg))

    if err != nil {
        log.Printf("Error enviando mail: %s", err.Error())
    } else {
        log.Println("📧 Mail enviado correctamente.")
    }
}
