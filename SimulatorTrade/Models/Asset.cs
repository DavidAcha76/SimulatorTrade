using System.ComponentModel.DataAnnotations;

namespace SimulatorTrade.Models
{
    public class Asset
    {
        public int Id { get; set; }
        [Required, MaxLength(20)] public string Symbol { get; set; } = "BTCUSDT";
        [MaxLength(80)] public string Name { get; set; } = "Bitcoin/USDT";
    }
}
