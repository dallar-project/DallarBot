using Dallar.Exchange;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace Dallar.Bots
{
    public partial class TwitchBot
    {
        protected void USDCommand(TwitchUserAccountContext InvokerTwitchAccount, List<string> Args)
        {
            decimal Amount;
            if (Args.Count == 0)
            {
                Amount = 1m;
            }
            else if (!(Args.Count == 1 && DallarClientService.TryParseAmountString(InvokerTwitchAccount.GetDallarAccount(), Args[0], false, out Amount)))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the USD command incorrectly. The syntax is 'd!usd <amount>'. For more help please visit {BotURL}");
                return;
            }

            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo PriceInfo, out _))
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: ${String.Format("{0:#,##0.00}", Amount)} USD is currently {String.Format("{0:#,##0.0#######}", decimal.Round(Amount / PriceInfo.PriceInUSD, 8))} DAL.");
            }
            else
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: Sorry, could not get price data at this time.");
            }
        }

        protected void BTCCommand(TwitchUserAccountContext InvokerTwitchAccount, List<string> Args)
        {
            decimal Amount;
            if (Args.Count == 0)
            {
                Amount = 1m;
            }
            else if (!(Args.Count == 1 && DallarClientService.TryParseAmountString(InvokerTwitchAccount.GetDallarAccount(), Args[0], false, out Amount)))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the BTC command incorrectly. The syntax is 'd!btc <amount>'. For more help please visit {BotURL}");
                return;
            }

            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo PriceInfo, out _))
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: {String.Format("{0:#,##0.0#######}", Amount)} BTC is currently {String.Format("{0:#,##0.0#######}", decimal.Round(Amount / PriceInfo.PriceInBTC, 8))} DAL.");
            }
            else
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: Sorry, could not get price data at this time.");
            }
        }

        protected void DALCommand(TwitchUserAccountContext InvokerTwitchAccount, List<string> Args)
        {
            decimal Amount;
            if (Args.Count == 0)
            {
                Amount = 1m;
            }
            else if (!(Args.Count == 1 && DallarClientService.TryParseAmountString(InvokerTwitchAccount.GetDallarAccount(), Args[0], false, out Amount)))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the DAL command incorrectly. The syntax is 'd!dal <amount>'. For more help please visit {BotURL}");
                return;
            }

            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo PriceInfo, out _))
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: {String.Format("{0:#,##0.0#######}", Amount)} DAL is currently {String.Format("{0:#,##0.0#######}", decimal.Round(Amount * PriceInfo.PriceInBTC, 8))} BTC and ${String.Format("{0:#,##0.00}", decimal.Round(Amount * PriceInfo.PriceInUSD, 8))} USD.");
            }
            else
            {
                Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: Sorry, could not get price data at this time.");
            }
        }
    }
}
