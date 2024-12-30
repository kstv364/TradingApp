using ScottPlot;
using TradingApp.Models;

namespace TradingApp.Strategies
{
    public class RSIStrategy : BaseStrategy, ITradingStrategy
    {
        private const double OverboughtThreshold = 70;
        private const double OversoldThreshold = 30;
        private const int RSIPeriod = 14;
        private const int SignalWindow = 500; // Define the window size for processing signals
        private double _capital = 10000; // Example capital amount

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

                // Restrict to the most recent 500 candles
                var windowedCandles = candles.Skip(Math.Max(0, candles.Count() - SignalWindow)).ToList();

                var rsiValues = CalculateRSI(windowedCandles);

                if (rsiValues.Count < RSIPeriod) continue; // Ensure sufficient RSI data

                var currentRSI = rsiValues.Last();

                // Plot the RSI values
                PlotRSI(rsiValues, ticker.Symbol);

                if (currentRSI > OverboughtThreshold)
                {
                    // Overbought condition, create sell orders
                    foreach (var position in openPositions.Where(p => p.Ticker == ticker.Symbol))
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker.Symbol,
                            Type = OrderType.Sell,
                            Price = latestCandle.Close,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            Notes = $"Overbought condition detected -> Sell at Current Price: {latestCandle.Close}"
                        });
                    }
                }
                else if (currentRSI < OversoldThreshold)
                {
                    // Oversold condition, create buy orders
                    var quantity = (int)(_capital / latestCandle.Close);

                    orders.Add(new Order
                    {
                        Ticker = ticker.Symbol,
                        Type = OrderType.Buy,
                        Price = latestCandle.Close,
                        StopLoss = latestCandle.Close * 0.98,
                        IsExitOrder = false,
                        Quantity = quantity,
                        TargetPrice = CalculateTargetPrice(latestCandle.Close),
                        Notes = $"Oversold condition detected -> Buy at Current Price: {latestCandle.Close}"
                    });
                }

                // Update the last processed timestamp for this ticker
                ticker.LastProcessedTimestamp = latestCandle.Time;
            }

            return await Task.FromResult(orders);
        }

        private List<double> CalculateRSI(IEnumerable<Candle> candles)
        {
            var closePrices = candles.Select(c => c.Close).ToList();
            var rsiValues = new List<double>();

            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i < RSIPeriod)
                {
                    rsiValues.Add(0); // Not enough data to calculate RSI
                }
                else
                {
                    var gains = 0.0;
                    var losses = 0.0;

                    for (int j = i - RSIPeriod + 1; j <= i; j++)
                    {
                        var change = closePrices[j] - closePrices[j - 1];
                        if (change > 0)
                        {
                            gains += change;
                        }
                        else
                        {
                            losses -= change;
                        }
                    }

                    var averageGain = gains / RSIPeriod;
                    var averageLoss = losses / RSIPeriod;

                    var rs = averageGain / averageLoss;
                    var rsi = 100 - (100 / (1 + rs));

                    rsiValues.Add(rsi);
                }
            }

            return rsiValues;
        }

        private void PlotRSI(List<double> rsiValues, string ticker)
        {
            // Create a new ScottPlot plot
            var plt = new Plot(1200, 300);

            // Plot the RSI values
            var times = Enumerable.Range(1, rsiValues.Count).Select(t => (double)t).ToArray();
            plt.AddScatter(times, rsiValues.ToArray(), label: "RSI", color: System.Drawing.Color.Blue);

            // Add overbought and oversold lines
            plt.AddHorizontalLine(OverboughtThreshold, color: System.Drawing.Color.Red, style: LineStyle.Dash);
            plt.AddHorizontalLine(OversoldThreshold, color: System.Drawing.Color.Green, style: LineStyle.Dash);

            // Customize the plot
            plt.Title($"RSI Chart for {ticker}");
            plt.XLabel("Time");
            plt.YLabel("RSI Value");
            plt.Legend();

            // Save or display the plot
            string filePath = $"RSI_{ticker}.png";
            plt.SaveFig(filePath);
            Console.WriteLine($"RSI plot saved to {filePath}");
        }

        protected override double CalculateTargetPrice(double currentPrice)
        {
            return currentPrice * TargetMultiplier;
        }
    }
}
