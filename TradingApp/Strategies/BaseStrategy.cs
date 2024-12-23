using System.Collections.Generic;
using System.Linq;
using TradingApp.Models;

namespace TradingApp.Strategies
{
    public abstract class BaseStrategy : ITradingStrategy
    {
        private const double TargetMultiplier = 1.3; // 30% profit target
        public virtual Task<List<Order>> GenerateOrdersAsync(Dictionary<Ticker, IEnumerable<Candle>> candlesByTicker, IEnumerable<Position> openPositions)
        {
            var orders = new List<Order>();

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

                foreach (var position in openPositions.Where(p => p.Ticker == ticker.Symbol))
                {
                    if (latestCandle.Close < position.StopLoss)
                    {
                        orders.Add(new Order
                        {
                            Ticker = position.Ticker,
                            Type = OrderType.Sell,
                            Price = latestCandle.Close,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            TargetPrice = CalculateTargetPrice(latestCandle.Close),
                            Notes =
                            $"Stop loss triggered ->  CurrentPrice : {latestCandle.Close}, SL : {position.StopLoss}"
                        });
                    }
                    else if (position.TargetPrice <= latestCandle.Close)
                    {
                        // Close position if target price is reached
                        orders.Add(new Order
                        {
                            Ticker = position.Ticker,
                            Type = OrderType.Sell,
                            Price = latestCandle.Close,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            TargetPrice = CalculateTargetPrice(latestCandle.Close),
                            Notes = $"Target price reached -> CurrentPrice : {latestCandle.Close}, TargetPrice : {position.TargetPrice}"
                        });
                    }
                    else
                    {
                        // Update stop loss to the latest close price
                        orders.Add(new Order
                        {
                            Ticker = position.Ticker,
                            Type = OrderType.Modify,
                            Price = position.EntryPrice,
                            StopLoss = Math.Max(position.StopLoss, latestCandle.Close * 0.98),
                            IsExitOrder = false,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            TargetPrice = CalculateTargetPrice(latestCandle.Close),
                            Notes = $"Stop loss updated -> CurrentPrice : {latestCandle.Close}, SL : {position.StopLoss}"
                        });
                    }
                }
            }

            return Task.FromResult(orders);
        }

        protected bool ShouldProcessTicker(Ticker ticker, DateTime latestCandleTime)
        {
            if (ticker.LastProcessedTimestamp.HasValue && latestCandleTime <= ticker.LastProcessedTimestamp.Value)
            {
                return false; // Skip processing if candles have not changed
            }

            return true;
        }

        protected virtual double CalculateTargetPrice(double currentPrice)
        {
            return currentPrice * TargetMultiplier;
        }

    }
}
