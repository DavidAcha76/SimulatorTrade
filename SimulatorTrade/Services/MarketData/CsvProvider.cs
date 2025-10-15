using SimulatorTrade.Models;

namespace SimulatorTrade.Services.MarketData
{
    public class CsvProvider : IMarketDataProvider
    {
        public Task<List<Candle>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc, string interval, CancellationToken ct)
        {
            return Task.FromResult(new List<Candle>());
        }
        public Task<Candle?> GetLatestAsync(string symbol, string interval, CancellationToken ct)
        {
            return Task.FromResult<Candle?>(null);
        }
    }
}
