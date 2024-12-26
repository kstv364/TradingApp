using Microsoft.EntityFrameworkCore;
using TradingApp.Models;
using TradingApp.Repository;

namespace TradingApp.Services.Traders
{
    public class AutomatedTrader : Trader
    {

        public AutomatedTrader(ITradingTerminal terminal, ITradingStrategy strategy, TradingDbContext dbContext, EmailNotificationService notificationService, ILogger<Trader> logger) : base(terminal, strategy, dbContext, notificationService, logger)
        {
        }

        public async override Task TradeAsync()
        {
            await base.TradeAsync();

            var unsoldTrades = _dbContext.Positions.Where(p => p.isOpen).ToList();
            var storedOpenOrders = _dbContext.Orders.Where(order => order.IsOpen).ToList();
            await UpdatePositions(unsoldTrades, storedOpenOrders);
        }


        private async Task UpdatePositions(List<Position> unsoldTrades, List<Order> storedOpenOrders)
        {
            foreach (var order in storedOpenOrders)
            {
                if (order.Type == OrderType.Sell)
                {
                    var position = unsoldTrades.FirstOrDefault(p => p.Id == order.positionId);
                    if (position != null)
                    {
                        var _closedPosition = position.CloseBy(order);
                        _dbContext.Positions.Update(_closedPosition);
                    }
                }
                else if (order.Type == OrderType.Modify)
                {
                    var existingPosition = unsoldTrades.FirstOrDefault(p => p.Id == order.positionId);
                    if (existingPosition != null)
                    {
                        var modifiedPosition = existingPosition.ModifyBy(order);
                        _dbContext.Positions.Update(modifiedPosition);
                    }
                }
                else
                {
                    var newPosition = Position.CreateBy(order);
                    var pos = _dbContext.Positions.Add(newPosition);
                    //await _dbContext.SaveChangesAsync();                   
                    order.positionId = pos.Entity.Id;
                    _dbContext.Orders.Update(order);
                }
            }
            await _dbContext.SaveChangesAsync();
        }

    }
}
