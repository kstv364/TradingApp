using TradingApp.Models;
using TradingApp.Repository;
using TradingApp.Services;

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

        foreach (var ticker in candlesByTicker.Keys)
        {
            _dbContext.Tickers.Update(ticker);
        }

        if (orders.Count == 0)
        {
            _logger.LogInformation("No orders to place.");
            return;
        }

        await _dbContext.Orders.AddRangeAsync(orders);
        await _dbContext.SaveChangesAsync();
        var storedOrders = orders.ToList();

        var notificationMessages = new List<string>();

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
                var newPosition = Position.CreateBy(order);
                var pos = _dbContext.Positions.Add(newPosition);
                await _dbContext.SaveChangesAsync();

                // Update the positionId of the order immediately after creating the position
                order.positionId = pos.Entity.Id;
            }

            await _terminal.PlaceOrderAsync(order);

            var message = $"\nTrade Advised: {order.Type} " +
                $"\nTicker: {order.Ticker} " +
                $"\nPrice :{order.Price} \nType: {order.Type} " +
                $"\nQuantity: {order.Quantity} " +
                $"\nStopLoss: {order.StopLoss} " +
                $"\nNotes: {order.Notes}";
            _logger.LogInformation(message);
            notificationMessages.Add(message);
        }

        await _notificationService.SendNotificationAsync(
            $"Trade Advisories for :{DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}", 
            string.Join("\n====================================", notificationMessages));
        await _dbContext.SaveChangesAsync();
    }
}