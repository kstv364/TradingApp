using System.Collections.Generic;
using System.Linq;
using TradingApp.Models;

namespace TradingApp.Strategies
{
    public abstract class BaseStrategy : ITradingStrategy
    {
        private const double TargetMultiplier = 1.3;
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
                    double currentClosePrice = latestCandle.Close;
                    if (currentClosePrice < position.StopLoss)
                    {
                        orders.Add(new Order
                        {
                            Ticker = position.Ticker,
                            Type = OrderType.Sell,
                            Price = currentClosePrice,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            TargetPrice = CalculateTargetPrice(currentClosePrice),
                            Notes =
                            $"Stop loss triggered ->  CurrentPrice : {currentClosePrice}, SL : {position.StopLoss}"
                        });
                    }
                    else if (position.TargetPrice <= currentClosePrice)
                    {
                        // Close position if target price is reached
                        orders.Add(new Order
                        {
                            Ticker = position.Ticker,
                            Type = OrderType.Sell,
                            Price = currentClosePrice,
                            StopLoss = 0,
                            IsExitOrder = true,
                            Quantity = position.Quantity,
                            positionId = position.Id,
                            TargetPrice = CalculateTargetPrice(currentClosePrice),
                            Notes = $"Target price reached -> CurrentPrice : {currentClosePrice}, TargetPrice : {position.TargetPrice}"
                        });
                    }
                    else
                    {
                        var newStopLoss = Math.Max(position.StopLoss, currentClosePrice * 0.98);

                        if(newStopLoss > position.StopLoss)
                        {
                            // Update stop loss to the latest close price
                            orders.Add(new Order
                            {
                                Ticker = position.Ticker,
                                Type = OrderType.Modify,
                                Price = position.EntryPrice,
                                StopLoss = newStopLoss,
                                IsExitOrder = false,
                                Quantity = position.Quantity,
                                positionId = position.Id,
                                TargetPrice = CalculateTargetPrice(currentClosePrice),
                                Notes = $"Stop loss updated -> CurrentPrice : {currentClosePrice}, SL : {position.StopLoss}"
                            });
                        }
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
