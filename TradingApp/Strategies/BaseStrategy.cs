using System.Collections.Generic;
using System.Linq;
using TradingApp.TradingApp.Models;

namespace TradingApp.TradingApp.Strategies
{
    public abstract class BaseStrategy : ITradingStrategy
    {
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
                            positionId = position.Id
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
                            positionId = position.Id
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
                            positionId = position.Id
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

    }
}
