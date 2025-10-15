using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Hubs;

namespace SimulatorTrade.Services
{
    public class HistoricalPlaybackService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _cfg;
        private readonly IHubContext<MarketHub> _hub;
        private readonly SimulationState _state;

        public HistoricalPlaybackService(IServiceProvider sp, IConfiguration cfg, IHubContext<MarketHub> hub, SimulationState state)
        { _sp=sp; _cfg=cfg; _hub=hub; _state=state; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_state.PlaybackEnabled){ await Task.Delay(1000, stoppingToken); continue; }

                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var symbol = _state.Symbol;
                    var asset = await db.Assets.FirstOrDefaultAsync(a=>a.Symbol==symbol, stoppingToken);
                    if (asset == null){ await Task.Delay(1000, stoppingToken); continue; }

                    var from = DateTime.UtcNow.AddMonths(-_state.MonthsBack);
                    var to = DateTime.UtcNow.AddMonths(-_state.MonthsBack).AddDays(7); // reproduce 1 semana por loop
                    var candles = await db.Candles.Where(c=>c.AssetId==asset.Id && c.Time>=from && c.Time<=to)
                                                  .OrderBy(c=>c.Time).ToListAsync(stoppingToken);
                    if (candles.Count == 0){ await Task.Delay(1000, stoppingToken); continue; }

                    foreach (var c in candles)
                    {
                        await _hub.Clients.Group("playback").SendAsync("Tick", new {
                            assetId = asset.Id, symbol = asset.Symbol, price = c.Close, time = c.Time, mode="playback"
                        }, stoppingToken);
                        // Speed: base 1s per candle / multiplier
                        int delay = (int)(1000 / Math.Max(0.1, _state.SpeedMultiplier));
                        await Task.Delay(delay, stoppingToken);
                        if (!_state.PlaybackEnabled) break;
                    }
                }
                catch { /* log in prod */ }
            }
        }
    }
}
