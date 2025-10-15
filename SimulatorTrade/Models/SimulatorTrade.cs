namespace SimulatorTrade.Models
{
    public class PortfolioDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Cash { get; set; }
        public List<PositionDto> Positions { get; set; } = new();
    }

    public class PositionDto
    {
        public int AssetId { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgPrice { get; set; }
    }
}
