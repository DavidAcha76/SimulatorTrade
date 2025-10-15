using SimulatorTrade.Models;

namespace SimulatorTrade.Services.MarketData
{
    public interface IMarketDataProvider
    {
        Task<List<Candle>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc, string interval, CancellationToken ct);
        Task<Candle?> GetLatestAsync(string symbol, string interval, CancellationToken ct);
    }
}
