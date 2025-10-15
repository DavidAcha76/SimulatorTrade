namespace SimulatorTrade.Models
{
    public class TradeFill
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int AssetId { get; set; }
        public Asset? Asset { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}
