using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Models;
using SimulatorTrade.Services;

namespace SimulatorTrade.Pages.Trade
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly OrderBook _book;
        private readonly OrderMatcher _matcher;

        public IndexModel(AppDbContext db, OrderBook book, OrderMatcher matcher){ _db=db; _book=book; _matcher=matcher; }

        [BindProperty] public InputModel Input { get; set; } = new();
        public string Message { get; set; } = "";

        public class InputModel
        {
            public string Symbol { get; set; } = "BTCUSDT";
            public string Type { get; set; } = "Market";
            public string Side { get; set; } = "Buy";
            public decimal Quantity { get; set; } = 0.001m;
            public decimal? Price { get; set; }
        }

        public void OnGet(){}

        public async Task<IActionResult> OnPostAsync()
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a=>a.Symbol==Input.Symbol);
            if (asset == null){ ModelState.AddModelError("","Símbolo no existente. Importa histórico primero."); return Page(); }
            var portfolio = await _db.Portfolios.FirstAsync();
            var order = new Order{
                AssetId = asset.Id, PortfolioId = portfolio.Id,
                Side = Enum.Parse<Side>(Input.Side), Type= Enum.Parse<OrderType>(Input.Type),
                Quantity = Input.Quantity, Price = Input.Type=="Limit" ? Input.Price : null, TIF = TimeInForce.GTC
            };
            _db.Orders.Add(order); await _db.SaveChangesAsync();

            var last = await _db.Candles.Where(c=>c.AssetId==asset.Id).OrderByDescending(c=>c.Time).Select(c=>c.Close).FirstOrDefaultAsync();
            if (order.Type == OrderType.Limit){ _book.Add(order, asset.Symbol); Message = $"Orden LIMIT registrada #{order.Id}"; }
            else {
                var fills = await _matcher.MatchAsync(order, asset.Symbol, last);
                if (order.Status == OrderStatus.Filled) Message = $"MARKET llenada ({fills.Count} fills)";
                else Message = $"MARKET parcial; resto al libro como LIMIT @{last}";
            }
            return Page();
        }
    }
}
