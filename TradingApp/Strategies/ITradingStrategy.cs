using TradingApp.TradingApp.Models;

public interface ITradingStrategy
{
    Task<List<Order>> GenerateOrdersAsync(Dictionary<Ticker, IEnumerable<Candle>> candlesByTicker, IEnumerable<Position> openPositions);
}
