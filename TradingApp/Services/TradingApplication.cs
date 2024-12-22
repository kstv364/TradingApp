using TradingApp.TradingApp.Models;
using TradingApp.TradingApp.Repository;
using TradingApp.TradingApp.Services;

public class TradingApplication
{
    private readonly ITradingTerminal _terminal;
    private readonly ITradingStrategy _strategy;
    private readonly TradingDbContext _dbContext;
    private readonly EmailNotificationService _notificationService;
    private readonly ILogger<TradingApplication> _logger;

    public TradingApplication(ITradingTerminal terminal, ITradingStrategy strategy, TradingDbContext dbContext, EmailNotificationService notificationService, ILogger<TradingApplication> logger)
    {
        _terminal = terminal;
        _strategy = strategy;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var tickers = _dbContext.Tickers.ToList();

        var candlesByTicker = await _terminal.GetRealTimeCandlesAsync(tickers);
        var unsoldTrades = _dbContext.Positions.Where(p => p.isOpen).ToList();

        var orders = await _strategy.GenerateOrdersAsync(candlesByTicker, unsoldTrades);
        
        if(orders.Count == 0)
        {
            _logger.LogInformation("No orders to place.");
            return;
        }

        foreach(var ticker in candlesByTicker.Keys)
        {
            _dbContext.Tickers.Update(ticker);
        }

        await _dbContext.Orders.AddRangeAsync(orders);
        await _dbContext.SaveChangesAsync();
        var storedOrders = orders.ToList();

        foreach (var order in storedOrders)
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
                var newPosition = new Position
                {
                    Ticker = order.Ticker,
                    EntryPrice = order.Price,
                    StopLoss = order.StopLoss,
                    TargetPrice = order.Price * 1.2,
                    Quantity = order.Quantity,
                    EntryDate = DateTime.UtcNow
                };
                var pos = _dbContext.Positions.Add(newPosition);
                await _dbContext.SaveChangesAsync();

                // Update the positionId of the order immediately after creating the position
                order.positionId = pos.Entity.Id;
            }

            await _terminal.PlaceOrderAsync(order);
            
            _logger.LogInformation($"Trade Advised: {order.Type} Ticker: {order.Ticker} Type: {order.Type} Quantity: {order.Quantity} StopLoss: {order.StopLoss}");
            await _notificationService.SendNotificationAsync($"Trade Advised: {order.Type}", $"Ticker: {order.Ticker}\nType: {order.Type}\nQuantity: {order.Quantity}\nStopLoss: {order.StopLoss}");
        }

        await _dbContext.SaveChangesAsync();
    }
}