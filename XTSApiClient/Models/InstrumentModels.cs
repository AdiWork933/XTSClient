namespace XTSApiClient.Models
{
    public class Instrument
    {
        public int ExchangeSegment { get; set; }
        public int ExchangeInstrumentID { get; set; }
    }

    public class MasterResponse
    {
        public string? Type { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Result { get; set; }
    }

    public class InstrumentDetail
    {
        public int ExchangeSegment { get; set; }
        public int ExchangeInstrumentID { get; set; }
        public string? InstrumentType { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Series { get; set; }
        public string? NameWithSeries { get; set; }
        public decimal TickSize { get; set; }
        public int LotSize { get; set; }
        public decimal StrikePrice { get; set; }
        public string? OptionType { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
