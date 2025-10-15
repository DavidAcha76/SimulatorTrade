using System.ComponentModel.DataAnnotations;

namespace SimulatorTrade.Models
{
    public class Portfolio
    {
        public int Id { get; set; }
        [MaxLength(64)] public string UserId { get; set; } = "demo";
        public decimal Cash { get; set; } = 100000m;

        // Navegaciones
        public ICollection<Position> Positions { get; set; } = new List<Position>();
        public ICollection<Order> Orders { get; set; } = new List<Order>(); // <-- nueva
    }
}
