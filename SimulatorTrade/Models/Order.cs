using System.ComponentModel.DataAnnotations;

namespace SimulatorTrade.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required] public int AssetId { get; set; }
        public Asset? Asset { get; set; }
        [Required] public int PortfolioId { get; set; }
        public Portfolio? Portfolio { get; set; }
        public Side Side { get; set; }
        public OrderType Type { get; set; }
        public TimeInForce TIF { get; set; } = TimeInForce.GTC;
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal FilledQuantity { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.New;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
