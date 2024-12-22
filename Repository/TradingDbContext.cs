using Microsoft.EntityFrameworkCore;
using TradingApp.Models;

namespace TradingApp.Repository
{
    public class TradingDbContext : DbContext
    {
        public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

        public DbSet<Position> Positions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Ticker> Tickers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Position>().HasKey(p => p.Id);
            modelBuilder.Entity<Position>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<Order>()
                .Property(o => o.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Ticker>().HasKey(t => t.Id);
            modelBuilder.Entity<Ticker>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Ticker>()
                .HasIndex(t => t.Symbol)
                .IsUnique(); // Make the symbol name unique
        }
    }
}
