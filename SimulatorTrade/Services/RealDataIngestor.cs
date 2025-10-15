using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Models;
using SimulatorTrade.Services.MarketData;

namespace SimulatorTrade.Services
{
    public class RealDataIngestor
    {
        private readonly AppDbContext _db;
        private readonly IMarketDataProvider _provider;
        public RealDataIngestor(AppDbContext db, IMarketDataProvider provider){ _db=db; _provider=provider; }

        public async Task<int> ImportAsync(int assetId, string symbol, DateTime fromUtc, DateTime toUtc, string interval, CancellationToken ct = default)
        {
            var data = await _provider.GetHistoryAsync(symbol, fromUtc, toUtc, interval, ct);
            int added = 0;
            foreach (var c in data)
            {
                bool exists = await _db.Candles.AnyAsync(x=>x.AssetId==assetId && x.Time==c.Time, ct);
                if (exists) continue;
                c.AssetId = assetId;
                _db.Candles.Add(c);
                added++;
            }
            await _db.SaveChangesAsync(ct);
            return added;
        }
    }
}
