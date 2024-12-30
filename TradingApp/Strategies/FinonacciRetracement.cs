using TradingApp.Models;

namespace TradingApp.Strategies
{
    public class FinonacciRetracement : BaseStrategy, ITradingStrategy
    {
        public async override Task<List<Order>> GenerateOrdersAsync(Dictionary<Ticker, IEnumerable<Candle>> candlesByTicker, IEnumerable<Position> openPositions)
        {
            var orders = new List<Order>();
            // Use common logic to create sell orders if stop loss is triggered
            orders.AddRange(await base.GenerateOrdersAsync(candlesByTicker, openPositions));

            foreach (var ticker in candlesByTicker.Keys)
            {
                var candles = candlesByTicker[ticker];
                if (!candles.Any()) continue;
                var latestCandle = candles.Last();

                // Calculate Fibonacci retracement levels
                var high = candles.Max(c => c.High);
                var low = candles.Min(c => c.Low);
                var retracementLevels = CalculateFibonacciRetracementLevels(high, low);

                foreach (var position in openPositions.Where(p => p.Ticker == ticker.Symbol))
                {
                    if (latestCandle.Close < retracementLevels[0.618])
                    {
                        // Create a buy order if the price is below the 61.8% retracement level
                        orders.Add(new Order
                        {
                            Ticker = ticker.Symbol,
                            Type = OrderType.Buy,
                            Price = latestCandle.Close,
                            StopLoss = latestCandle.Close * 0.98,
                            IsExitOrder = false,
                            Quantity = position.Quantity
                        });
                    }
                    else if (latestCandle.Close > retracementLevels[0.382])
                    {
                        // Create a sell order if the price is above the 38.2% retracement level
                        orders.Add(new Order
                        {
                            Ticker = ticker.Symbol,
                            Type = OrderType.Sell,
                            Price = latestCandle.Close,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id
                        });
                    }
                }
            }

            return await Task.FromResult(orders);
        }

        private Dictionary<double, double> CalculateFibonacciRetracementLevels(double high, double low)
        {
            var levels = new Dictionary<double, double>
            {
                { 0.0, high },
                { 0.236, high - (high - low) * 0.236 },
                { 0.382, high - (high - low) * 0.382 },
                { 0.5, high - (high - low) * 0.5 },
                { 0.618, high - (high - low) * 0.618 },
                { 1.0, low }
            };

            return levels;
        }
    }
}
