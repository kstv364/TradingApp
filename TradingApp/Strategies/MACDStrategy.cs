using ScottPlot;
using TradingApp.Models;

namespace TradingApp.Strategies
{
    public class MACDStrategy : BaseStrategy
    {
        private const double TargetMultiplier = 1.3; // 30% profit target
        private double _capital = 10000; // Example capital amount

        public async override Task<List<Order>> GenerateOrdersAsync(Dictionary<Ticker, IEnumerable<Candle>> candlesByTicker, IEnumerable<Position> openPositions)
        {
            var orders = new List<Order>();

            // Use common logic to create sell orders if stop loss is triggered
            orders.AddRange(await base.GenerateOrdersAsync(candlesByTicker, openPositions));

            const int SignalWindow = 200; // Define the window size for processing signals

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

                // Restrict to the most recent 1000 candles
                var windowedCandles = candles.Skip(Math.Max(0, candles.Count() - SignalWindow)).ToList();

                if (windowedCandles.Count < 26) continue; // Ensure sufficient data for MACD calculation

                var macdValues = CalculateMACD(windowedCandles);

                if (macdValues.Count < 2) continue; // Ensure sufficient MACD data

                // Extract MACD and Signal Line values for plotting
                var macdLine = macdValues.Select(m => m.MACDLine).Skip(26).ToArray();
                var signalLine = macdValues.Select(m => m.SignalLine).Skip(26).ToArray();

                // a sequence of 1,2,3 values for plotting
                var times = Enumerable.Range(1, macdLine.Length).ToArray();

                // cast times to double array
                var timesDouble = times.Select(t => (double)t).ToArray();

                // Plot the MACD values
                PlotMACD(macdLine, signalLine, timesDouble, ticker.Symbol);

                // Iterate through the MACD values in the defined window
                for (int i = macdValues.Count - 1; i >= Math.Max(0, macdValues.Count - SignalWindow); i--)
                {
                    if (i == 0) continue; // Skip first index to avoid out-of-bounds for (i - 1)

                    if (macdValues[i - 1].MACDLine > macdValues[i - 1].SignalLine &&
                        macdValues[i].MACDLine < macdValues[i].SignalLine)
                    {
                        // Bearish signal detected, create sell orders
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
                                Notes =
                                    $"Bearish signal detected at {macdValues[i].Time.ToLocalTime()} -> Sell at Current Price: {latestCandle.Close}"
                            });
                        }
                        break; // Exit the loop after processing the most recent signal
                    }
                    else if (macdValues[i - 1].MACDLine < macdValues[i - 1].SignalLine &&
                             macdValues[i].MACDLine > macdValues[i].SignalLine)
                    {
                        // Bullish signal detected, create buy orders
                        var signalStrength = CalculateSignalStrength(macdValues[i]);
                        var quantity = (int)(_capital * signalStrength / latestCandle.Close);

                        orders.Add(new Order
                        {
                            Ticker = ticker.Symbol,
                            Type = OrderType.Buy,
                            Price = latestCandle.Close,
                            StopLoss = latestCandle.Close * 0.98,
                            IsExitOrder = false,
                            Quantity = quantity,
                            TargetPrice = CalculateTargetPrice(latestCandle.Close),
                            Notes =
                                $"Bullish signal detected at {macdValues[i].Time.ToLocalTime()} -> Buy at Current Price: {latestCandle.Close}"
                        });
                        break; // Exit the loop after processing the most recent signal
                    }
                }

                // Update the last processed timestamp for this ticker
                ticker.LastProcessedTimestamp = latestCandle.Time;
            }

            return await Task.FromResult(orders);
        }

        private void PlotMACD(double[] macdLine, double[] signalLine, double[] times, string ticker)
        {
            // Create a new ScottPlot plot
            var plt = new Plot(1200, 300);

            // Plot the MACD Line
            plt.AddScatter(times, macdLine, label: "MACD Line", color: System.Drawing.Color.Blue);

            // Plot the Signal Line
            plt.AddScatter(times, signalLine, label: "Signal Line", color: System.Drawing.Color.Orange);
            // Customize the plot
            plt.Title($"MACD Chart for {ticker}");
            plt.XLabel("Time");
            plt.YLabel("Value");
            plt.Legend();

            // Save or display the plot
            string filePath = $"MACD_{ticker}.png";
            plt.SaveFig(filePath);
            Console.WriteLine($"MACD plot saved to {filePath}");
        }

        private List<MACDValue> CalculateMACD(IEnumerable<Candle> candles)
    {
            var closePrices = candles.Select(c => c.Close).ToList();

            // Calculate the short-term and long-term EMAs
            var shortEma = CalculateEMA(closePrices, 12).Skip(26);
            var longEma = CalculateEMA(closePrices, 26).Skip(26);

            // Calculate MACD line as the difference between short and long EMAs
            var macdLine = shortEma.Zip(longEma, (shortVal, longVal) => shortVal - longVal).ToList();

            // Calculate Signal line as the EMA of the MACD line
            var signalLine = CalculateEMA(macdLine, 9);

            // Align data and return the results
            var macdValues = new List<MACDValue>();
            for (int i = 0; i < macdLine.Count; i++)
            {
                macdValues.Add(new MACDValue
                {
                    MACDLine = macdLine[i],
                    SignalLine = i < signalLine.Count ? signalLine[i] : 0,
                    Time = candles.ElementAt(i).Time
                });
                //if (i >= 26) // Ensure data aligns with the longest EMA period
                //{
                   
                //}
                //else
                //{
                //    macdValues.Add(new MACDValue
                //    {
                //        MACDLine = 0,
                //        SignalLine = 0,
                //        Time = candles.ElementAt(i).Time
                //    });
                //}
            }

            return macdValues;
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
                    // First EMA value is the simple moving average
                    ema.Add(prices.Take(period).Average());
                }
                else
                {
                    // EMA calculation for subsequent values
                    ema.Add((prices[i] - ema[i - 1]) * multiplier + ema[i - 1]);
                }
            }

            return ema;
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

        protected override double CalculateTargetPrice(double currentPrice)
        {
            return currentPrice * TargetMultiplier;
        }
    }
}
