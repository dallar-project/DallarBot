using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DallarBot.Services;

namespace DallarBot.Commands
{
    public class ExchangeCommands : BaseCommandModule
    {
        /** USD to Dallar **/

        [Command("usd")]
        [Description("Converts USD to Dallar")]
        [Aliases("usd-dal")]
        public async Task USDToDallar(CommandContext Context, [Description("Optional amount of USD to covert to Dallar.")]params decimal[] Amounts)
        {
            await Context.TriggerTypingAsync();

            decimal Amount = 1m;
            if (Amounts.Length > 0)
            {
                Amount = Amounts[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked USD command with amount {Amount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();
            var Info = String.Format("{0:#,##0.00000000}", Amount) + " USD is " + String.Format("{0:#,##0.00000000}", decimal.Round(Amount / Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 8)) + " DAL.";
            await Context.RespondAsync($"{Context.User.Mention}: {Info}");
            _ = Context.Message.DeleteAsync();
        }

        /** BTC to Dallar */

        [Command("btc")]
        [Aliases("btc-dal")]
        public async Task BTCToDallar(CommandContext Context, params decimal[] Amounts)
        {
            await Context.TriggerTypingAsync();

            decimal Amount = 1m;
            if (Amounts.Length > 0)
            {
                Amount = Amounts[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked BTC command with amount {Amount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();
            var Info = String.Format("{0:#,##0.00000000}", Amount) + " BTC is " + String.Format("{0:#,##0.00000000}", decimal.Round(Amount / Program.DigitalPriceExchange.DallarInfo.Price, 8)) + " DAL.";
            await Context.RespondAsync($"{Context.User.Mention}: {Info}");
            _ = Context.Message.DeleteAsync();
        }

        /** Dallar to BTC/USD */

        [Command("dal")]
        [Aliases("dal-btc", "dal-usd", "dalvalue")]
        public async Task DallarValueInfo(CommandContext Context, params decimal[] Amounts)
        {
            await Context.TriggerTypingAsync();

            decimal Amount = 1m;
            if (Amounts.Length > 0)
            {
                Amount = Amounts[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked DAL command with amount {Amount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();

            float.TryParse(Program.DigitalPriceExchange.DallarInfo.PriceChange.TrimEnd('%'), out float PercentChange);
            string ChangeEmoji = PercentChange >= 0.0f ? ":chart_with_upwards_trend:" : ":chart_with_downwards_trend:";

            decimal UsdValue = Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault();

            var Info = $"{Amount} DAL to BTC: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.Price * Amount), 8, MidpointRounding.AwayFromZero):F8} BTC" + Environment.NewLine +
                $"{Amount} DAL to USD: ${UsdValue * Amount} :dollar:" + Environment.NewLine +
                $"24 Hour Stats: :arrow_down_small: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.Low.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrow_up_small: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.High.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrows_counterclockwise: {Program.DigitalPriceExchange.DallarInfo.VolumeMarket} BTC" + Environment.NewLine +
                $"{ChangeEmoji} {Program.DigitalPriceExchange.DallarInfo.PriceChange} Change in 24 Hours";

            await Context.RespondAsync(Info);
            _ = Context.Message.DeleteAsync();
        }
    }
}
