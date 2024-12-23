using Microsoft.EntityFrameworkCore;
using Serilog;
using TradingApp.Repository;
using TradingApp.Services;
using TradingApp.Strategies;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Configure services
services.AddHttpClient<ITradingTerminal, YahooFinanceTerminal>();
services.AddSingleton<EmailNotificationService>();
services.AddSingleton<ITradingStrategy, MACDStrategy>();

var connectionString = configuration.GetConnectionString("DefaultConnection");
services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(connectionString));
services.AddHostedService<TradingService>();
services.AddControllers(); // Add this line to register controllers

// Configure logging
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .WriteTo.Console()
    .WriteTo.File("Logs/tradingapp-.txt", rollingInterval: RollingInterval.Day));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();
app.MapControllers();
if (!app.Environment.IsDevelopment())
{
    ApplyMigration();
}

app.Run();

void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}
