using System.Collections.Concurrent;
using SimulatorTrade.Models;

namespace SimulatorTrade.Services
{
    public class OrderBook
    {
        private readonly ConcurrentDictionary<string, List<Order>> _bids = new();
        private readonly ConcurrentDictionary<string, List<Order>> _asks = new();

        public void Add(Order o, string symbol)
        {
            var side = o.Side == Side.Buy ? _bids : _asks;
            var list = side.GetOrAdd(symbol, _ => new List<Order>());
            list.Add(o);
            if (o.Type == OrderType.Limit && o.Price.HasValue)
            {
                if (o.Side == Side.Buy) list.Sort((a,b)=>Nullable.Compare(b.Price, a.Price));
                else list.Sort((a,b)=>Nullable.Compare(a.Price, b.Price));
            }
        }

        public (List<Order> bids, List<Order> asks) Get(string symbol)
        {
            _bids.TryGetValue(symbol, out var bids);
            _asks.TryGetValue(symbol, out var asks);
            return (bids ?? new(), asks ?? new());
        }

        public void Remove(Order o, string symbol)
        {
            var side = o.Side == Side.Buy ? _bids : _asks;
            if (side.TryGetValue(symbol, out var list))
                list.RemoveAll(x => x.Id == o.Id);
        }
    }
}
