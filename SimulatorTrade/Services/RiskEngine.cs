using SimulatorTrade.Models;
namespace SimulatorTrade.Services
{
    public class RiskEngine
    {
        public bool CanExecute(Order o, decimal px)
        {
            if (px<=0 || o.Quantity<=0) return false;
            return true;
        }
    }
}
