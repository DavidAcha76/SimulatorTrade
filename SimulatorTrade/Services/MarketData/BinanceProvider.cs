using System.Text.Json;
using SimulatorTrade.Models;

namespace SimulatorTrade.Services.MarketData
{
    public class BinanceProvider : IMarketDataProvider
    {
        private readonly HttpClient _http;
        public BinanceProvider(HttpClient http){ _http = http; }

        public async Task<List<Candle>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc, string interval, CancellationToken ct)
        {
            long start = new DateTimeOffset(fromUtc).ToUnixTimeMilliseconds();
            long end = new DateTimeOffset(toUtc).ToUnixTimeMilliseconds();
            var url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&startTime={start}&endTime={end}&limit=1000";
            var json = await _http.GetStringAsync(url, ct);
            var arr = JsonSerializer.Deserialize<List<List<JsonElement>>>(json)!;
            var list = new List<Candle>(arr.Count);
            foreach (var k in arr){
                list.Add(new Candle{
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(k[0].GetInt64()).UtcDateTime,
                    Open = decimal.Parse(k[1].GetString()!), High = decimal.Parse(k[2].GetString()!),
                    Low = decimal.Parse(k[3].GetString()!), Close = decimal.Parse(k[4].GetString()!),
                    Volume = (long)decimal.Parse(k[5].GetString()!)
                });
            }
            return list;
        }

        public async Task<Candle?> GetLatestAsync(string symbol, string interval, CancellationToken ct)
        {
            var url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&limit=1";
            var json = await _http.GetStringAsync(url, ct);
            var arr = JsonSerializer.Deserialize<List<List<JsonElement>>>(json)!;
            if (arr.Count==0) return null;
            var k = arr[0];
            return new Candle{
                Time = DateTimeOffset.FromUnixTimeMilliseconds(k[0].GetInt64()).UtcDateTime,
                Open = decimal.Parse(k[1].GetString()!), High = decimal.Parse(k[2].GetString()!),
                Low = decimal.Parse(k[3].GetString()!), Close = decimal.Parse(k[4].GetString()!),
                Volume = (long)decimal.Parse(k[5].GetString()!)
            };
        }
    }
}
