namespace SimulatorTrade.Services
{
    public class SimulationState
    {
        public bool PlaybackEnabled { get; set; } = true;
        public string Symbol { get; set; } = "BTCUSDT";
        public string Interval { get; set; } = "1m";
        public int MonthsBack { get; set; } = 1;
        public double SpeedMultiplier { get; set; } = 1.0; // 1x -> 1 candle per Polling tick
    }
}
