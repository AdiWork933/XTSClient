using System.Text.Json;

namespace XTSApiClient.Models
{
    public class OHLCResponse
    {
        public string? Type { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public OHLCResult? Result { get; set; }
    }

    public class OHLCResult
    {
        public int ExchangeSegment { get; set; }
        public int ExchangeInstrumentID { get; set; }
        public JsonElement DataReponse { get; set; }
    }

    public class OHLCBar
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
    }
}
