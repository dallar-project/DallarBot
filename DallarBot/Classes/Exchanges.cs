/* Fetches json data from the DigialPrice exchange. */
namespace DallarBot.Exchanges
{
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    public partial class DigitalPriceDallarInfo
    {
        [J("url")] public string Url { get; set; }
        [J("mini_currency")] public string MiniCurrency { get; set; }
        [J("currency")] public string Currency { get; set; }
        [J("base_currency")] public string BaseCurrency { get; set; }
        [J("volume")] public long Volume { get; set; }
        [J("volume_market")] public decimal VolumeMarket { get; set; }
        [J("price")] public decimal Price { get; set; }
        [J("price_change")] public string PriceChange { get; set; }
        [J("class_change")] public string ClassChange { get; set; }
        [J("low")] public decimal? Low { get; set; }
        [J("high")] public decimal? High { get; set; }
    }
}
