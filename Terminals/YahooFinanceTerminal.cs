using System.Text.Json;
using TradingApp.Models;

public class YahooFinanceTerminal : ITradingTerminal
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public YahooFinanceTerminal(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<Dictionary<Ticker, IEnumerable<Candle>>> GetRealTimeCandlesAsync(IEnumerable<Ticker> tickers)
    {
        var candlesByTicker = new Dictionary<Ticker, IEnumerable<Candle>>();
        var baseUrl = _configuration["FinanceApi:BaseUrl"];

        foreach (var ticker in tickers)
        {
            var url = $"{baseUrl}/historical-data?ticker={ticker.Symbol}&interval=1d&date_range=3mo";
            var candles = new List<Candle>();
            var response = string.Empty;
            try
            {
                response = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var results = doc.RootElement.EnumerateArray();

                foreach (var result in results)
                {
                    var candle = new Candle
                    {
                        Time = DateTime.Parse(result.GetProperty("Date").GetString()),
                        Open = result.GetProperty("Open").GetDouble(),
                        Close = result.GetProperty("Close").GetDouble(),
                        High = result.GetProperty("High").GetDouble(),
                        Low = result.GetProperty("Low").GetDouble()
                    };
                    candles.Add(candle);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("API call failed : ", e.Message);
            }
            candlesByTicker[ticker] = candles;
            await Task.Delay(1000);
        }

        return candlesByTicker;
    }

    public Task PlaceOrderAsync(Order order)
    {
        // Implement the actual order placement logic here.
        // Console.WriteLine($"Placing {order.Type} order for {order.Ticker} at {order.Price}");
        return Task.CompletedTask;
    }
}
