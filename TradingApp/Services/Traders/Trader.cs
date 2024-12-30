using TradingApp.Models;
using TradingApp.Repository;
using TradingApp.Services;

public class Trader
{
    protected readonly ITradingTerminal _terminal;
    protected readonly ITradingStrategy _strategy;
    protected readonly TradingDbContext _dbContext;
    protected readonly EmailNotificationService _notificationService;
    protected readonly ILogger<Trader> _logger;

    public Trader(ITradingTerminal terminal, ITradingStrategy strategy, TradingDbContext dbContext, EmailNotificationService notificationService, ILogger<Trader> logger)
    {
        _terminal = terminal;
        _strategy = strategy;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async virtual Task TradeAsync()
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


        var notificationMessages = new List<string>();

        foreach (var order in orders)
        {
            await _terminal.PlaceOrderAsync(order);
            notificationMessages.Add(BuildNotification(order));
        }

        //await _notificationService.SendNotificationAsync(
        //    $"Trade Advisories for :{DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()}",
        //    string.Join("\n====================================", notificationMessages));
    }
    private string BuildNotification(Order order)
    {
        var message = $"\nTrade Advised: {order.Type} " +
            $"\nTicker: {order.Ticker} " +
            $"\nPrice :{order.Price} \nType: {order.Type} " +
            $"\nQuantity: {order.Quantity} " +
            $"\nStopLoss: {order.StopLoss} " +
            $"\nNotes: {order.Notes}";

        _logger.LogInformation(message);
        return message;
    }
}