using Dallar.Exchange;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Dallar.Exchange
{
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

        public DateTime FetchedTime { get; set; }

        public DigitalPriceCurrencyInfo Clone()
        {
            return (DigitalPriceCurrencyInfo)this.MemberwiseClone();
        }

        public static implicit operator DallarPriceInfo(DigitalPriceCurrencyInfo d)
        {
            DallarPriceInfo i = new DallarPriceInfo()
            {
                PriceInBTC = d.Price,
                Volume = d.Volume,
                Low = d.Low.GetValueOrDefault(),
                High = d.High.GetValueOrDefault(),
                PriceInUSD = d.USDValue.GetValueOrDefault(),
                FetchedTime = d.FetchedTime
            };

            decimal.TryParse(d.PriceChange.TrimEnd('%'), out decimal PriceChange);
            i.PriceChange = PriceChange;

            return i;
        }
    }

    public class DigitalPriceExchangeService : IDallarPriceProviderService
    {
        protected DigitalPriceCurrencyInfo DallarInfo;
        protected Timer FetchTimer;

        public DigitalPriceExchangeService()
        {
            FetchValueInfo().GetAwaiter().GetResult();
            FetchTimer = new Timer(FetchTimerInvoke, null, 10, Timeout.Infinite);
        }

        protected void FetchTimerInvoke(object state)
        {
            FetchValueInfo().GetAwaiter().GetResult();
        }

        protected async Task FetchValueInfo()
        {
            if (DallarInfo == null || DateTime.Compare(DallarInfo.FetchedTime, DateTime.Now.AddSeconds(-9.0d)) < 0)
            {
                var client = new WebClient();
                var DigitalPriceJSON = await client.DownloadStringTaskAsync("https://digitalprice.io/markets/get-currency-summary?currency=BALANCE_COIN_BITCOIN");
                var btcPrice = await client.DownloadStringTaskAsync("https://blockchain.info/tobtc?currency=USD&value=1");

                DigitalPriceCurrencyInfo[] DigitalPriceInfo = null;

                try
                {
                    DigitalPriceInfo = JsonConvert.DeserializeObject<DigitalPriceCurrencyInfo[]>(DigitalPriceJSON);

                    for (int i = 0; i < DigitalPriceInfo.Length; i++)
                    {
                        if (DigitalPriceInfo[i].MiniCurrency == "dal-btc")
                        {
                            DallarInfo = DigitalPriceInfo[i];
                            DallarInfo.USDValue = decimal.Round(DallarInfo.Price / Convert.ToDecimal(btcPrice), 8, MidpointRounding.AwayFromZero);
                            DallarInfo.FetchedTime = DateTime.Now;
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to get DigitalPrice Exchange Info: {e.ToString()}");
                }
            }
        }

        public bool GetPriceInfo(out DallarPriceInfo PriceInfo, out bool bStale)
        {
            if (DallarInfo != null)
            {
                bStale = false;

                PriceInfo = DallarInfo.Clone();
                if (DateTime.Compare(DallarInfo.FetchedTime, DateTime.Now.AddSeconds(-12.0d)) < 0)
                {
                    bStale = true;
                }
                return true;
            }

            bStale = true;
            PriceInfo = null;
            return false;
        }
    }
}
