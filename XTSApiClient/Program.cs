using XTSApiClient.Models;
using XTSApiClient.Services;

class Program
{
    private const string BASE_URL = "https://xts.rmoneyindia.co.in:3000";
    private const string SOCKET_URL = "https://xts.rmoneyindia.co.in:3000";
    private const string API_KEY = "7c972gfh76hvfd9ee9577";
    private const string SECRET_KEY = LIfUf757#oD";

    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================================");
        Console.WriteLine("         XTS API CLIENT - C# IMPLEMENTATION             ");
        Console.WriteLine("========================================================\n");

        using var client = new XTSClient(BASE_URL, API_KEY, SECRET_KEY);

        try
        {
            Console.WriteLine("FEATURE 1: MARKET DATA LOGIN\n");

            await client.MarketDataLoginAsync();

            Console.WriteLine("Login successful!");
            Console.WriteLine($"User ID : {client.UserID}");
            Console.WriteLine($"Token   : {client.Token?.Substring(0, 20)}...\n");

            Console.WriteLine("FEATURE 2: DOWNLOAD OHLC DATA - TOP 5 NIFTY 50\n");

            var endTime = DateTime.Now;
            var startTime = endTime.AddMinutes(-30);

            var ohlcData = await client.GetTop5Nifty50OHLCAsync(
                startTime,
                endTime,
                60
            );

            foreach (var stock in ohlcData)
            {
                if (stock.Value.Count == 0)
                    continue;

                CsvSaver.SaveOhlc(stock.Key, stock.Value, startTime, endTime, 60);
            }

            foreach (var stock in ohlcData)
            {
                Console.WriteLine($"\n{stock.Key}:");

                var candles = stock.Value;

                if (candles.Count == 0)
                {
                    Console.WriteLine("  No data available");
                    continue;
                }

                var last = candles.Last();

                Console.WriteLine($"  Time   : {last.Timestamp}");
                Console.WriteLine($"  Open   : {last.Open}");
                Console.WriteLine($"  High   : {last.High}");
                Console.WriteLine($"  Low    : {last.Low}");
                Console.WriteLine($"  Close  : {last.Close}");
                Console.WriteLine($"  Volume : {last.Volume}");
            }

            Console.WriteLine("\nFEATURE 3: STREAM DATA VIA WEBSOCKET\n");

            await StartSocketStreaming(client.Token!, client.UserID!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
        }

        Console.WriteLine("\n========================================================");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task StartSocketStreaming(string token, string userID)
    {
        using var socket = new MarketDataSocket(SOCKET_URL, token, userID);

        var streamStartedAt = DateTime.Now;
        var streamCsvPath = CsvSaver.EnsureStreamCsv(streamStartedAt);

        socket.OnConnected += () =>
            Console.WriteLine("WebSocket connected!");

        socket.OnTouchlineData += data =>
        {
            CsvSaver.AppendStreamEvent(streamCsvPath, "TOUCHLINE", data);
            Console.WriteLine($"[LIVE] {data}");
        };

        socket.OnMarketDepthData += data =>
        {
            CsvSaver.AppendStreamEvent(streamCsvPath, "MARKET_DEPTH", data);
            Console.WriteLine($"[DEPTH] {data}");
        };

        socket.OnError += error =>
        {
            CsvSaver.AppendStreamEvent(streamCsvPath, "ERROR", error);
            Console.WriteLine($"[ERROR] {error}");
        };

        await socket.ConnectAsync();

        var instruments = new List<Instrument>
        {
            new Instrument { ExchangeSegment = 1, ExchangeInstrumentID = 2885 },
            new Instrument { ExchangeSegment = 1, ExchangeInstrumentID = 1333 }
        };

        await socket.SubscribeAsync(instruments);

        Console.WriteLine("\nStreaming for 30 seconds...\n");
        await Task.Delay(30000);

        await socket.UnsubscribeAsync(instruments);
        await socket.DisconnectAsync();

        Console.WriteLine("Socket streaming demo complete");
    }
}

