/* Fetches json data from the DigialPrice exchange. */
using System;

namespace Dallar.Exchange
{
    public class DallarPriceInfo
    {
        public decimal PriceInBTC { get; set; }
        public decimal PriceChange { get; set; }
        public decimal Volume { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }

        // Fiat Prices
        public decimal PriceInUSD { get; set; }

        // Meta info
        public DateTime FetchedTime { get; set; }
    }

    public interface IDallarPriceProviderService
    {
        bool GetPriceInfo(out DallarPriceInfo PriceInfo, out bool bStale);
    }

       
}
