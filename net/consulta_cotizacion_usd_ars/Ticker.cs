namespace ConsultaCotizacionUsdArs.Models
{
    public record Ticker(bool Success, TickerPayload payload);
    public record TickerPayload(string high, string last, string volume, string ask, string bid);
}
