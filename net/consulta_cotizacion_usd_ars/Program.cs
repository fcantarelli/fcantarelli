using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Globalization;

namespace ConsultaCotizacionUsdArs
{
    public class Program
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<T> ConsultarConReintento<T>(string response, string url)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(response)!;
            }
            catch (Exception e)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Console.WriteLine($"⚠️ Error al deserializar en {url}: {e.Message}. Reintentando...");
                await Task.Delay(3000);
                try
                {
                    return JsonSerializer.Deserialize<T>(response, options)!;
                }
                catch (Exception e2)
                {
                    Console.WriteLine($"❌ Error persistente al deserializar en {url}: {e2.Message}");
                    throw;
                }
            }
        }

        public static async Task Consultar(string url, string currency)
        {
            var response = await client.GetStringAsync($"{url}?book={currency}");

            switch (url)
            {
                case "https://api.bitso.com/api/v3/order_book":
                    
                    var ob = await ConsultarConReintento<OrderBook>(response, url);
                    if (ob?.payload == null)
                    {
                        Console.WriteLine($"⚠️ Payload nulo en {url} {currency}");
                        return;
                    }

                    var dtOrder = DateTimeOffset.Parse(ob.payload.updated_at);
                    var dtUtc3 = dtOrder.ToOffset(TimeSpan.FromHours(-3));

                    Console.WriteLine($"ORDER_BOOK {currency} actualizado en {dtUtc3}");

                    if (ob.payload.bids?.Count > 0 && ob.payload.asks?.Count > 0)
                    {
                        var bid = ob.payload.bids[0];
                        var ask = ob.payload.asks[0];
                        double bidPrice = double.Parse(bid.price, CultureInfo.InvariantCulture);
                        double askPrice = double.Parse(ask.price, CultureInfo.InvariantCulture);

                        Console.WriteLine($"Bid: {bidPrice} Ask: {askPrice} Book: {bid.book}");

                        if (currency == "usd_ars" && bidPrice > 1663.5)
                        {
                            EnviarMail(bidPrice, askPrice, "0", "0", "0", "0", "0", dtUtc3.ToString(), currency, 0, 0);
                        }
                    }
                    break;

                case "https://api.bitso.com/api/v3/ticker":
                    var tk = await ConsultarConReintento<Ticker>(response, url);

                    if (tk?.payload == null)
                    {
                        Console.WriteLine($"⚠️ Payload nulo en {url} {currency}");
                        return;
                    }
                    Console.WriteLine($"TICKER {currency} -> High: {tk.payload.high}, Last: {tk.payload.last}, Vol: {tk.payload.volume}, Ask: {tk.payload.ask}, Bid: {tk.payload.bid}");

                    double highVal = double.Parse(tk.payload.high, CultureInfo.InvariantCulture);
                    if ((currency == "btc_usd" && highVal > 99000.0) || (currency == "aave_usd" && highVal > 210.0))
                    {
                        Console.WriteLine($"DEBUG highVal={highVal}");
                        EnviarMail(0.0, 0.0, tk.payload.high, tk.payload.last, tk.payload.volume, tk.payload.ask, tk.payload.bid, "N/A", currency, 0, 0);
                    }
                    break;

                case "https://api.bitso.com/api/v3/trades":
                    var tr = await ConsultarConReintento<Trades>(response, url);
                    if (tr?.payload == null || tr.payload.Count == 0)
                    {
                        Console.WriteLine($"⚠️ Payload vacío en {url} {currency}");
                        return;
                    }
                    int countSell = 0, countBuy = 0;
                    string lastBook = "";

                    foreach (var t in tr.payload)
                    {
                        if (t.maker_side == "sell") countSell++;
                        else if (t.maker_side == "buy") countBuy++;
                        lastBook = t.book;
                    }
                    Console.WriteLine($"Total SELL: {countSell}, BUY: {countBuy}, to {lastBook}");
                    break;
            }
        }

        public static void EnviarMail(
            double bidPrice,
            double askPrice,
            string high,
            string last,
            string volume,
            string ask,
            string bid,
            string dtUtc3,
            string book,
            int countSell,
            int countBuy)
        {
            string cuerpo = $@"Cotización actual:
Bid Price: {bidPrice}
Ask Price: {askPrice}
High: {high}
Last: {last}
Volume: {volume}
Ask: {ask}
Bid: {bid}
Última operación (UTC-3): {dtUtc3}
Book: {book}
Total SELL: {countSell}
Total BUY: {countBuy}";

            var msg = new MailMessage("my_email_sender@yahoo.com.ar", "addressee@gmail.com", "bitso_price_alert", cuerpo);

            string appPassword = Environment.GetEnvironmentVariable("YAHOO_APP_PASSWORD") ?? throw new Exception("YAHOO_APP_PASSWORD no definido");
            var smtp = new SmtpClient("smtp.mail.yahoo.com", 587)
            {
                Credentials = new System.Net.NetworkCredential("my_email@yahoo.com", appPassword),
                EnableSsl = true
            };

            try
            {
                smtp.Send(msg);
                Console.WriteLine("📧 Mail enviado correctamente.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error enviando mail: {e.Message}");
            }
        }

        public static async Task Main(string[] args)
        {
            var vecCurrency = new[] { "usd_ars", "btc_usd", "btc_ars", "aave_usd", "avax_usd", "pepe_usd" };
            var vecUrl = new[]
            {
                "https://api.bitso.com/api/v3/order_book",
                "https://api.bitso.com/api/v3/trades",
                "https://api.bitso.com/api/v3/ticker"
            };

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("🛑 Señal de salida recibida, cerrando proceso...");
                cts.Cancel();
                e.Cancel = true;
            };
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    for (int i = 0; i < vecUrl.Length; i++)
                    {
                        foreach (var currency in vecCurrency)
                        {
                            try
                            {
                                await Consultar(vecUrl[i], currency);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"❌ Error en consulta {vecUrl[i]} {currency}: {e.Message}");
                            }
                        }
                        if (i == vecUrl.Length - 1)
                        {
                            Console.WriteLine("#------------------------------ query ended OK ------------------------------#");
                        }
                    }
                    // ⏱️ Espera 3 minutos antes de repetir el ciclo completo
                    await Task.Delay(TimeSpan.FromMinutes(3), cts.Token);
                }
            }
        
            catch (TaskCanceledException)
            { 
            Console.WriteLine("✅ Proceso cancelado correctamente."); 
            }
        }
    }
    // 📊 Modelos
    public record OrderBook(bool Success, Payload payload);
    public record Payload(List<Order> bids, List<Order> asks, string updated_at, string sequence);
    public record Order(string book, string price, string amount);

    public record Ticker(bool Success, TickerPayload payload);
    public record TickerPayload(string high, string last, string volume, string ask, string bid);

    public record Trades(bool Success, List<Trade> payload);
    public record Trade(string maker_side, string book, string created_at);
}
