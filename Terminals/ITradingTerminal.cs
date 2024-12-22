using TradingApp.Models;

public interface ITradingTerminal
{
    Task<Dictionary<Ticker, IEnumerable<Candle>>> GetRealTimeCandlesAsync(IEnumerable<Ticker> tickers);
    Task PlaceOrderAsync(Order order);
}
