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

        public IndexModel(AppDbContext db, OrderBook book, OrderMatcher matcher)
        {
            _db = db;
            _book = book;
            _matcher = matcher;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Message { get; set; } = "";
        public Portfolio Portfolio { get; set; }

        public class InputModel
        {
            public string Symbol { get; set; } = "BTCUSDT";
            public string Type { get; set; } = "Market";  // Market o Limit
            public string Side { get; set; } = "Buy";     // Buy o Sell
            public decimal Quantity { get; set; } = 0.001m;
            public decimal? Price { get; set; }            // Solo para Limit orders
        }

        public async Task OnGetAsync()
        {
            Portfolio = await _db.Portfolios.Include(p => p.Positions).FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Symbol == Input.Symbol);
            if (asset == null)
            {
                Message = "Símbolo no existe. Importa histórico primero.";
                await OnGetAsync();
                return Page();
            }

            var portfolio = await _db.Portfolios.Include(p => p.Positions).FirstOrDefaultAsync();
            Portfolio = portfolio;

            var order = new Order
            {
                AssetId = asset.Id,
                PortfolioId = portfolio.Id,
                Side = Enum.Parse<Side>(Input.Side),
                Type = Enum.Parse<OrderType>(Input.Type),
                Quantity = Input.Quantity,
                Price = Input.Type == "Limit" ? Input.Price : null,
                TIF = TimeInForce.GTC,
                Status = OrderStatus.New
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var lastPrice = await _db.Candles
                .Where(c => c.AssetId == asset.Id)
                .OrderByDescending(c => c.Time)
                .Select(c => c.Close)
                .FirstOrDefaultAsync();

            if (order.Type == OrderType.Limit)
            {
                _book.Add(order, asset.Symbol);
                Message = $"Orden LIMIT registrada para {Input.Side} {Input.Quantity} {Input.Symbol} a {Input.Price}";
            }
            else
            {
                var fills = await _matcher.MatchAsync(order, asset.Symbol, lastPrice);
                if (order.Status == OrderStatus.Filled)
                    Message = $"Orden MARKET ejecutada completamente ({fills.Count} fills).";
                else
                    Message = $"Orden MARKET ejecutada parcialmente; resto agregado al libro como LIMIT.";
            }

            Portfolio = await _db.Portfolios.Include(p => p.Positions).FirstOrDefaultAsync();
            return Page();
        }
    }
}
