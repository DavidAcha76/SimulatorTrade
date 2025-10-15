using Microsoft.AspNetCore.SignalR;

namespace SimulatorTrade.Hubs
{
    public class MarketHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public Task Join(string mode) // "realtime" or "playback"
        {
            if (mode != "realtime" && mode != "playback") mode = "realtime";
            return Groups.AddToGroupAsync(Context.ConnectionId, mode);
        }
    }
}
