namespace ConsultaCotizacionUsdArs.Models
{
    public record OrderBook(bool Success, Payload payload);
    public record Payload(List<Order>? bids, List<Order>? asks, string? updated_at, string? sequence);
    public record Order(string book, string price, string amount);
}
