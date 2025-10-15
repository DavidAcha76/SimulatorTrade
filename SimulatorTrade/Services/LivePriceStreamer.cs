using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Hubs;
using SimulatorTrade.Models;
using SimulatorTrade.Services.MarketData;

namespace SimulatorTrade.Services
{
    public class LivePriceStreamer : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _cfg;
        private readonly IHubContext<MarketHub> _hub;
        private readonly IMarketDataProvider _provider;

        public LivePriceStreamer(IServiceProvider sp, IConfiguration cfg, IHubContext<MarketHub> hub, IMarketDataProvider provider)
        { _sp=sp; _cfg=cfg; _hub=hub; _provider=provider; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var symbol = _cfg["DataProvider:Symbol"] ?? "BTCUSDT";
            var interval = _cfg["DataProvider:Interval"] ?? "1m";
            var pollMs = _cfg.GetValue<int>("DataProvider:PollingMs", 2000);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var asset = await db.Assets.FirstOrDefaultAsync(a=>a.Symbol==symbol, stoppingToken);
                    if (asset == null){ asset = new Asset{ Symbol=symbol, Name=symbol }; db.Assets.Add(asset); await db.SaveChangesAsync(stoppingToken); }

                    var latest = await _provider.GetLatestAsync(symbol, interval, stoppingToken);
                    if (latest != null)
                    {
                        latest.AssetId = asset.Id;
                        bool exists = await db.Candles.AnyAsync(x=>x.AssetId==asset.Id && x.Time==latest.Time, stoppingToken);
                        if (!exists){ db.Candles.Add(latest); await db.SaveChangesAsync(stoppingToken); }

                        await _hub.Clients.Group("realtime").SendAsync("Tick", new {
                            assetId = asset.Id, symbol = asset.Symbol, price = latest.Close, time = latest.Time, mode="realtime"
                        }, stoppingToken);
                    }
                }
                catch { /* log in prod */ }
                await Task.Delay(pollMs, stoppingToken);
            }
        }
    }
}
