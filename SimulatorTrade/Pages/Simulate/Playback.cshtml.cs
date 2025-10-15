using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Services;

namespace SimulatorTrade.Pages.Simulate
{
    public class PlaybackModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly SimulationState _state;
        private readonly RealDataIngestor _ing;

        public PlaybackModel(AppDbContext db, SimulationState state, RealDataIngestor ing){ _db=db; _state=state; _ing=ing; }
        [BindProperty] public InputModel Input { get; set; } = new();
        public string Message { get; set; } = "";

        public class InputModel
        {
            public string Symbol { get; set; } = "BTCUSDT";
            public int MonthsBack { get; set; } = 1;
            public string Interval { get; set; } = "1m";
            public double SpeedMultiplier { get; set; } = 1.0;
        }

        public void OnGet(){}

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure asset and historical data exist
            var asset = await _db.Assets.FirstOrDefaultAsync(a=>a.Symbol==Input.Symbol);
            if (asset == null){ asset = new Models.Asset{ Symbol=Input.Symbol, Name=Input.Symbol }; _db.Assets.Add(asset); await _db.SaveChangesAsync(); }
            var from = DateTime.UtcNow.AddMonths(-Input.MonthsBack).AddDays(-7);
            var to = DateTime.UtcNow.AddMonths(-Input.MonthsBack).AddDays(7);
            var count = await _ing.ImportAsync(asset.Id, Input.Symbol, from, to, Input.Interval);
            // Update state
            _state.Symbol = Input.Symbol;
            _state.MonthsBack = Input.MonthsBack;
            _state.Interval = Input.Interval;
            _state.SpeedMultiplier = Input.SpeedMultiplier;
            _state.PlaybackEnabled = true;
            Message = $"Playback configurado ({Input.Symbol}, {Input.MonthsBack} meses atrás, x{Input.SpeedMultiplier}). Importadas {count} velas (si hacían falta).";
            return Page();
        }
    }
}
