using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingApp.Models;
using TradingApp.Repository;

namespace TradingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TickerController : ControllerBase
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<TickerController> _logger;

        public TickerController(TradingDbContext dbContext, ILogger<TickerController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticker>>> GetTickers()
        {
            return await _dbContext.Tickers.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Ticker>> AddTicker([FromBody] Ticker ticker)
        {
            if (_dbContext.Tickers.Any(t => t.Symbol == ticker.Symbol))
            {
                return BadRequest("Ticker already exists.");
            }

            _dbContext.Tickers.Add(ticker);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Ticker {Symbol} added with ID {Id}", ticker.Symbol, ticker.Id);

            return CreatedAtAction(nameof(GetTickers), new { id = ticker.Id }, ticker);
        }

        [HttpDelete("{symbol}")]
        public async Task<IActionResult> DeleteTicker(string symbol)
        {
            var ticker = await _dbContext.Tickers.FirstOrDefaultAsync(t => t.Symbol == symbol);
            if (ticker == null)
            {
                _logger.LogWarning("Ticker {Symbol} not found", symbol);
                return NotFound();
            }

            _dbContext.Tickers.Remove(ticker);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Ticker {Symbol} deleted", symbol);

            return NoContent();
        }
    }
}
