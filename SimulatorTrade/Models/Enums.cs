namespace SimulatorTrade.Models
{
    public enum Side { Buy = 1, Sell = -1 }
    public enum OrderType { Market = 0, Limit = 1 }
    public enum OrderStatus { New, PartiallyFilled, Filled, Canceled }
    public enum TimeInForce { GTC, IOC, FOK }
}
