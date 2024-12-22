namespace TradingApp.TradingApp.Models
{
    public class Ticker
    {
        public int Id { get; set; }
        public required string Symbol { get; set; }
        public DateTime? LastProcessedTimestamp { get; set; } // Add this property
    }
}
