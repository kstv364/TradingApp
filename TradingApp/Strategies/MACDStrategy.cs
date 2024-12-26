using TradingApp.Models;

namespace TradingApp.Strategies
{
    public class MACDStrategy : BaseStrategy
    {
        private const double TargetMultiplier = 1.3; // 30% profit target
        private double _capital = 10000; // Example capital amount

        private const int PERIODS_PER_DAY = 72;
        private int _windowLength = 10 * PERIODS_PER_DAY;

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

                // Use the common timestamp checking logic
                if (!ShouldProcessTicker(ticker, latestCandle.Time))
                {
                    continue; // Skip processing if candles have not changed
                }

                var filteredcandles = candles.Where(c =>
                {
                    return !ticker.LastProcessedTimestamp.HasValue || c.Time >= ticker.LastProcessedTimestamp;
                }).ToList();

                var macdValues = CalculateMACD(candles);

                var allowedWindow = Math.Min(_windowLength, Math.Min(filteredcandles.Count, macdValues.Count));

                for (int i = macdValues.Count - 1; i >= macdValues.Count - allowedWindow + 1; i--)
                {
                    if (macdValues[i - 1].MACDLine > macdValues[i - 1].SignalLine && macdValues[i].MACDLine < macdValues[i].SignalLine)
                    {
                        // sell all open positions
                        foreach (var position in openPositions.Where(p => p.Ticker == ticker.Symbol))
                        {
                            // Bearish signal detected, create a sell order
                            orders.Add(new Order
                            {
                                Ticker = ticker.Symbol,
                                Type = OrderType.Sell,
                                Price = latestCandle.Close,
                                StopLoss = 0,
                                IsExitOrder = true,
                                Quantity = position.Quantity,
                                positionId = position.Id,
                                Notes =
                                $"Bearish signal detected at {macdValues[i].Time}-> Sell at Current Price : {latestCandle.Close}"
                            });
                        }
                        break;
                    }
                    if (macdValues[i - 1].MACDLine < macdValues[i - 1].SignalLine && macdValues[i].MACDLine > macdValues[i].SignalLine)
                    {
                        // Calculate signal strength and determine quantity
                        var signalStrength = CalculateSignalStrength(macdValues[i]);
                        var quantity = (int)(_capital * signalStrength / candles.ElementAt(i).Close);

                        // Allow only long positions
                        orders.Add(new Order
                        {
                            Ticker = ticker.Symbol,
                            Type = OrderType.Buy,
                            Price = latestCandle.Close,
                            StopLoss = latestCandle.Close * 0.98,
                            IsExitOrder = false,
                            Quantity = quantity,
                            TargetPrice = CalculateTargetPrice(latestCandle.Close),
                            Notes = $"Bullish signal detected at {macdValues[i].Time}-> Buy at Current Price : {latestCandle.Close}"
                        });
                        break;
                    }
                }

                // Update the last processed timestamp for this ticker
                ticker.LastProcessedTimestamp = latestCandle.Time;
            }

            return await Task.FromResult(orders);
        }

        private double CalculateSignalStrength(MACDValue macdValue)
        {
            // Calculate signal strength based on the difference between MACD line and Signal line
            double difference = Math.Abs(macdValue.MACDLine - macdValue.SignalLine);

            // Normalize the difference to a value between 0 and 1
            // Assuming a maximum difference of 1 for normalization
            double maxDifference = 1.0;
            double signalStrength = Math.Min(difference / maxDifference, 1.0);

            return signalStrength;
        }

        private List<MACDValue> CalculateMACD(IEnumerable<Candle> candles)
        {
            var closePrices = candles.Select(c => c.Close).ToList();
            var shortEma = CalculateEMA(closePrices, 12);
            var longEma = CalculateEMA(closePrices, 26);
            var macdLine = shortEma.Zip(longEma, (shortVal, longVal) => shortVal - longVal).ToList();
            var signalLine = CalculateEMA(macdLine, 9);

            return macdLine.Select((macd, index) => new MACDValue
            {
                MACDLine = macd,
                SignalLine = index < signalLine.Count ? signalLine[index] : 0,
                Time = candles.ElementAt(index).Time
            }).ToList();
        }

        private List<double> CalculateEMA(List<double> prices, int period)
        {
            var ema = new List<double>();
            double multiplier = 2.0 / (period + 1);

            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period - 1)
                {
                    ema.Add(0); // Not enough data to calculate EMA
                }
                else if (i == period - 1)
                {
                    ema.Add(prices.Take(period).Average()); // Simple average for the first EMA value
                }
                else
                {
                    ema.Add((prices[i] - ema[i - 1]) * multiplier + ema[i - 1]);
                }
            }

            return ema;
        }

        protected override double CalculateTargetPrice(double currentPrice)
        {
            return currentPrice * TargetMultiplier;
        }
    }
}
