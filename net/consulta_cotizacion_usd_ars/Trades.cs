namespace ConsultaCotizacionUsdArs.Models
{
    public record Trades(bool Success, List<Trade> payload);
    public record Trade(string maker_side, string book, string created_at);
}
