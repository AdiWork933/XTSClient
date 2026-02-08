using System.Net.Http.Json;
using System.Text.Json;
using XTSApiClient.Models;

namespace XTSApiClient.Services
{
    public class XTSClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _secretKey;

        private string? _token;
        private string? _userID;

        public const int NSECM = 1;
        public const int NSEFO = 2;

        public XTSClient(string baseUrl, string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public string? Token => _token;
        public string? UserID => _userID;
        public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

        public async Task<LoginResponse> MarketDataLoginAsync()
        {
            var payload = new
            {
                appKey = _apiKey,
                secretKey = _secretKey,
                source = "WEBAPI"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/apimarketdata/auth/login",
                payload
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            _token = result!.Result!.Token;
            _userID = result.Result.UserID;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", _token);

            return result;
        }

        public async Task<List<OHLCBar>> GetOHLCAsync(
            int exchangeSegment,
            int exchangeInstrumentID,
            DateTime startTime,
            DateTime endTime,
            int compressionValue = 60)
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Login first");

            var url =
                "/apimarketdata/instruments/ohlc?" +
                $"exchangeSegment={exchangeSegment}&" +
                $"exchangeInstrumentID={exchangeInstrumentID}&" +
                $"startTime={Uri.EscapeDataString(startTime.ToString("MMM dd yyyy HHmmss"))}&" +
                $"endTime={Uri.EscapeDataString(endTime.ToString("MMM dd yyyy HHmmss"))}&" +
                $"compressionValue={compressionValue}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<OHLCResponse>();

            return ParseOHLC(json!.Result!.DataReponse);
        }

        private List<OHLCBar> ParseOHLC(JsonElement element)
        {
            var list = new List<OHLCBar>();

            if (element.ValueKind == JsonValueKind.Null ||
                element.ValueKind == JsonValueKind.Undefined)
                return list;

            if (element.ValueKind == JsonValueKind.String)
            {
                var csv = element.GetString();
                if (string.IsNullOrWhiteSpace(csv))
                    return list;

                foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var p = line.Split('|');
                    if (p.Length < 6)
                        continue;

                    list.Add(new OHLCBar
                    {
                        Timestamp = FromUnix(p[0]),
                        Open = decimal.Parse(p[1]),
                        High = decimal.Parse(p[2]),
                        Low = decimal.Parse(p[3]),
                        Close = decimal.Parse(p[4]),
                        Volume = long.Parse(p[5]),
                        OpenInterest = p.Length > 6 ? long.Parse(p[6]) : 0
                    });
                }

                return list;
            }

            foreach (var row in element.EnumerateArray())
            {
                var r = row.EnumerateArray().ToArray();
                if (r.Length < 6)
                    continue;

                list.Add(new OHLCBar
                {
                    Timestamp = ParseTime(r[0]),
                    Open = r[1].GetDecimal(),
                    High = r[2].GetDecimal(),
                    Low = r[3].GetDecimal(),
                    Close = r[4].GetDecimal(),
                    Volume = r[5].GetInt64(),
                    OpenInterest = r.Length > 6 ? r[6].GetInt64() : 0
                });
            }

            return list;
        }

        private DateTime ParseTime(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? DateTime.Parse(element.GetString()!)
                : DateTimeOffset.FromUnixTimeSeconds(element.GetInt64()).LocalDateTime;
        }

        private DateTime FromUnix(string value)
        {
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(value)).LocalDateTime;
        }

        public async Task<Dictionary<string, List<OHLCBar>>> GetTop5Nifty50OHLCAsync(
            DateTime startTime,
            DateTime endTime,
            int compressionValue = 60)
        {
            var stocks = new Dictionary<string, int>
            {
                { "RELIANCE", 2885 },
                { "HDFCBANK", 1333 },
                { "INFY", 1594 },
                { "TCS", 11536 },
                { "ICICIBANK", 4963 }
            };

            var result = new Dictionary<string, List<OHLCBar>>();

            Console.WriteLine("\n========== TOP 5 NIFTY 50 OHLC ==========\n");

            foreach (var stock in stocks)
            {
                try
                {
                    Console.WriteLine($"Fetching {stock.Key}...");
                    result[stock.Key] = await GetOHLCAsync(
                        NSECM,
                        stock.Value,
                        startTime,
                        endTime,
                        compressionValue
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching {stock.Key}: {ex.Message}");
                }
            }

            return result;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
