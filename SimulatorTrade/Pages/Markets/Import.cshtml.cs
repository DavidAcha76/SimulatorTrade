using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Services;

namespace SimulatorTrade.Pages.Markets
{
    public class ImportModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly RealDataIngestor _ing;
        public ImportModel(AppDbContext db, RealDataIngestor ing){ _db=db; _ing=ing; }

        [BindProperty] public InputModel Input { get; set; } = new();
        public string Message { get; set; } = "";

        public class InputModel
        {
            public string Symbol { get; set; } = "BTCUSDT";
            public DateTime From { get; set; } = DateTime.UtcNow.AddMonths(-1);
            public DateTime To { get; set; } = DateTime.UtcNow;
            public string Interval { get; set; } = "1m";
        }

        public void OnGet(){}

        public async Task<IActionResult> OnPostAsync()
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a=>a.Symbol==Input.Symbol);
            if (asset == null){ asset = new Models.Asset{ Symbol=Input.Symbol, Name=Input.Symbol }; _db.Assets.Add(asset); await _db.SaveChangesAsync(); }
            var rows = await _ing.ImportAsync(asset.Id, Input.Symbol, Input.From, Input.To, Input.Interval);
            Message = $"Importadas {rows} velas para {Input.Symbol} ({Input.Interval}).";
            return Page();
        }
    }
}
