using TradingApp.TradingApp.Repository;
using TradingApp.TradingApp.Services;

public class TradingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public TradingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            var _app = new TradingApplication(
                    scope.ServiceProvider.GetRequiredService<ITradingTerminal>(),
                    scope.ServiceProvider.GetRequiredService<ITradingStrategy>(),
                    scope.ServiceProvider.GetRequiredService<TradingDbContext>(),
                    scope.ServiceProvider.GetRequiredService<EmailNotificationService>(),
                    scope.ServiceProvider.GetRequiredService<ILogger<TradingApplication>>());
            await _app.RunAsync();
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
