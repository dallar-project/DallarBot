/* Fetches json data from the DigialPrice exchange. */
namespace DallarBot.Services
{
    using Newtonsoft.Json;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    public partial class DigitalPriceCurrencyInfo
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
        [J("usd_value")] public decimal? USDValue { get; set; }
    }

    public class DigitalPriceExchangeService
    {
        protected DateTime LastFetchTime;
        public DigitalPriceCurrencyInfo DallarInfo;

        public DigitalPriceExchangeService()
        {
            LastFetchTime = new DateTime();
            FetchValueInfo().GetAwaiter().GetResult();
        }

        public async Task FetchValueInfo()
        {
            if (DateTime.Compare(LastFetchTime, DateTime.Now.AddSeconds(-5.0d)) < 0)
            {
                await LogHandlerService.LogAsync("Fetching DigitalPrice Exchange Info.");

                var client = new WebClient();
                var DigitalPriceJSON = await client.DownloadStringTaskAsync("https://digitalprice.io/markets/get-currency-summary?currency=BALANCE_COIN_BITCOIN");
                var btcPrice = await client.DownloadStringTaskAsync("https://blockchain.info/tobtc?currency=USD&value=1");

                var DigitalPriceInfo = JsonConvert.DeserializeObject<DigitalPriceCurrencyInfo[]>(DigitalPriceJSON);

                for (int i = 0; i < DigitalPriceInfo.Length; i++)
                {
                    if (DigitalPriceInfo[i].MiniCurrency == "dal-btc")
                    {
                        DallarInfo = DigitalPriceInfo[i];
                    }
                }

                DallarInfo.USDValue = decimal.Round(DallarInfo.Price / Convert.ToDecimal(btcPrice), 8, MidpointRounding.AwayFromZero);
                LastFetchTime = DateTime.Now;
            }
        }
    }

    
}
