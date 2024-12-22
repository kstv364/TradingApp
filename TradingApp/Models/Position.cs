using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingApp.TradingApp.Models
{
    public class Position
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Ticker { get; set; } // Primary key
        public required double EntryPrice { get; set; }
        public double StopLoss { get; set; }
        public double TargetPrice { get; set; }
        public required DateTime EntryDate { get; set; }
        public required int Quantity { get; set; } // Quantity of stocks held

        public double ClosePrice { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime CloseDate { get; set; }

        public int ClosedByOrderId { get; set; }

        public bool isOpen { get; set; } = true;

        public static Position CreateBy(Order order)
        {
            return new Position
            {
                Ticker = order.Ticker,
                EntryPrice = order.Price,
                StopLoss = order.StopLoss,
                TargetPrice = order.Price * 1.2,
                Quantity = order.Quantity,
                EntryDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                isOpen = true
            };
        }

        public Position ModifyBy(Order order)
        {
            StopLoss = order.StopLoss;
            LastUpdated = DateTime.UtcNow;
            return this;
        }

        public Position CloseBy(Order order)
        {
            ClosePrice = order.Price;
            CloseDate = DateTime.UtcNow;
            ClosedByOrderId = order.Id;
            LastUpdated = DateTime.UtcNow;
            isOpen = false;
            return this;
        }
    }
}
