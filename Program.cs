using Microsoft.EntityFrameworkCore;
using Serilog;
using TradingApp;
using TradingApp.Repository;
using TradingApp.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Configure services
services.AddHttpClient<ITradingTerminal, YahooFinanceTerminal>();
services.AddSingleton<EmailNotificationService>();
services.AddSingleton<ITradingStrategy, MACDStrategy>();

services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
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

app.Run();
