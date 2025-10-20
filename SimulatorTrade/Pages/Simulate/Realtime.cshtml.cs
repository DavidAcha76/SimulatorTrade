using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace SimulatorTrade.Pages.Simulate
{
    public class RealtimeModel : PageModel
    {
        private readonly IConfiguration _cfg;
        public string Symbol { get; set; } = "BTCUSDT";
        public string Interval { get; set; } = "1m";
        public RealtimeModel(IConfiguration cfg) { _cfg = cfg; }
        public void OnGet()
        {
            Symbol = _cfg["DataProvider:Symbol"] ?? "BTCUSDT";
            Interval = _cfg["DataProvider:Interval"] ?? "1m";
        }
    }
}
