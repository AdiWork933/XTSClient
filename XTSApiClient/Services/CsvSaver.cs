using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using XTSApiClient.Models;

namespace XTSApiClient.Services
{
    public static class CsvSaver
    {
        private static readonly object _fileLock = new();

        public static string DataDirectory
        {
            get
            {
                var path = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DATA")
                );

                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string BuildOhlcFileName(string symbol, DateTime startTime, DateTime endTime, int compressionValue)
        {
            var safeSymbol = SanitizeFileNamePart(symbol);
            return $"OHLC_{safeSymbol}_{startTime:yyyyMMdd_HHmmss}_{endTime:yyyyMMdd_HHmmss}_C{compressionValue}.csv";
        }

        public static string BuildStreamFileName(DateTime startedAt)
        {
            return $"STREAM_{startedAt:yyyyMMdd_HHmmss}.csv";
        }

        public static string SaveOhlc(string symbol, IReadOnlyList<OHLCBar> candles, DateTime startTime, DateTime endTime, int compressionValue)
        {
            var fileName = BuildOhlcFileName(symbol, startTime, endTime, compressionValue);
            var fullPath = Path.Combine(DataDirectory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Open,High,Low,Close,Volume,OpenInterest");

            foreach (var c in candles)
            {
                sb.Append(c.Timestamp.ToString("O", CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.Open.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.High.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.Low.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.Close.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.Volume.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(c.OpenInterest.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine();
            }

            lock (_fileLock)
            {
                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            }

            return fullPath;
        }

        public static string EnsureStreamCsv(DateTime startedAt)
        {
            var fileName = BuildStreamFileName(startedAt);
            var fullPath = Path.Combine(DataDirectory, fileName);

            lock (_fileLock)
            {
                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, "Timestamp,EventType,Message" + Environment.NewLine, Encoding.UTF8);
                }
            }

            return fullPath;
        }

        public static void AppendStreamEvent(string streamCsvPath, string eventType, string message)
        {
            var line = string.Join(",",
                DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                EscapeCsv(eventType),
                EscapeCsv(message)
            );

            lock (_fileLock)
            {
                File.AppendAllText(streamCsvPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return "";

            var mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "UNKNOWN";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }

            return sb.ToString();
        }
    }
}
