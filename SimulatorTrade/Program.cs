using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Hubs;
using SimulatorTrade.Services;
using SimulatorTrade.Services.MarketData;

var builder = WebApplication.CreateBuilder(args);

// ========================= EF Core =========================
// SOLO factory. Nada de AddDbContext para evitar scoped deps en root.
builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ==================== Razor Pages + SignalR =================
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// ======================= Domain services ====================
builder.Services.AddSingleton<OrderBook>();
builder.Services.AddSingleton<OrderMatcher>();   // usa IDbContextFactory
builder.Services.AddSingleton<RiskEngine>();
builder.Services.AddScoped<PortfolioService>();  // -> debe usar factory adentro
builder.Services.AddScoped<RealDataIngestor>();  // -> idem
builder.Services.AddSingleton<SimulationState>();

// ============== Market data provider selection ==============
var kind = builder.Configuration["DataProvider:Kind"] ?? "Binance";
if (kind.Equals("Binance", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<BinanceProvider>();
    builder.Services.AddTransient<IMarketDataProvider, BinanceProvider>();
}
else
{
    builder.Services.AddTransient<IMarketDataProvider, CsvProvider>();
}

// ===================== Background services ==================
builder.Services.AddHostedService<LivePriceStreamer>();
builder.Services.AddHostedService<HistoricalPlaybackService>();

var app = builder.Build();

// ===================== DB init + Migrations ==================
using (var scope = app.Services.CreateScope())
{
    // Como no hay AddDbContext, pedimos la factory y creamos un contexto local
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();

    await db.Database.MigrateAsync();

    if (!db.Portfolios.Any())
    {
        db.Portfolios.Add(new SimulatorTrade.Models.Portfolio { UserId = "demo", Cash = 100000m });
        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<MarketHub>("/hubs/market");

app.Run();
