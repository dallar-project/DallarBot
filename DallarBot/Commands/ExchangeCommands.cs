using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DallarBot.Services;
using DallarBot.Classes;

namespace DallarBot.Commands
{
    public class ExchangeCommands : BaseCommandModule
    {
        /** USD to Dallar **/

        [Command("usd")]
        [Aliases("usd-dal")]
        [Description("Converts USD to Dallar")]
        [HelpCategory("Exchange")]
        public async Task USDToDallar(CommandContext Context, [Description("Optional amount of USD to covert to Dallar, default is 1")] params decimal[] Amount)
        {
            await Context.TriggerTypingAsync();

            decimal ParsedAmount = 1m;
            if (Amount.Length > 0)
            {
                ParsedAmount = Amount[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked USD command with amount {ParsedAmount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();
            var Info = String.Format("{0:#,##0.00000000}", Amount) + " USD is " + String.Format("{0:#,##0.00000000}", decimal.Round(ParsedAmount / Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 8)) + " DAL.";
            await Context.RespondAsync($"{Context.User.Mention}: {Info}");
            _ = Context.Message.DeleteAsync();
        }

        /** BTC to Dallar */

        [Command("btc")]
        [Aliases("btc-dal")]
        [Description("Converts BTC to Dallar")]
        [HelpCategory("Exchange")]
        public async Task BTCToDallar(CommandContext Context, [Description("Optional amount of BTC to covert to Dallar, default is 1")] params decimal[] Amount)
        {
            await Context.TriggerTypingAsync();

            decimal ParsedAmount = 1m;
            if (Amount.Length > 0)
            {
                ParsedAmount = Amount[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked BTC command with amount {ParsedAmount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();
            var Info = String.Format("{0:#,##0.00000000}", ParsedAmount) + " BTC is " + String.Format("{0:#,##0.00000000}", decimal.Round(ParsedAmount / Program.DigitalPriceExchange.DallarInfo.Price, 8)) + " DAL.";
            await Context.RespondAsync($"{Context.User.Mention}: {Info}");
            _ = Context.Message.DeleteAsync();
        }

        /** Dallar to BTC/USD */

        [Command("dal")]
        [Aliases("dal-btc", "dal-usd", "dalvalue")]
        [Description("Converts Exchange to Dallar")]
        [HelpCategory("Exchange")]
        public async Task DallarValueInfo(CommandContext Context, [Description("Optional amount of DAL to covert to BTC and USD, default is 1")] params decimal[] Amount)
        {
            await Context.TriggerTypingAsync();

            decimal ParsedAmount = 1m;
            if (Amount.Length > 0)
            {
                ParsedAmount = Amount[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked DAL command with amount {ParsedAmount}.");

            await Program.DigitalPriceExchange.FetchValueInfo();

            float.TryParse(Program.DigitalPriceExchange.DallarInfo.PriceChange.TrimEnd('%'), out float PercentChange);
            string ChangeEmoji = PercentChange >= 0.0f ? ":chart_with_upwards_trend:" : ":chart_with_downwards_trend:";

            decimal UsdValue = Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault();

            var Info = $"{ParsedAmount} DAL to BTC: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.Price * ParsedAmount), 8, MidpointRounding.AwayFromZero):F8} BTC" + Environment.NewLine +
                $"{ParsedAmount} DAL to USD: ${UsdValue * ParsedAmount} :dollar:" + Environment.NewLine +
                $"24 Hour Stats: :arrow_down_small: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.Low.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrow_up_small: {decimal.Round((Program.DigitalPriceExchange.DallarInfo.High.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrows_counterclockwise: {Program.DigitalPriceExchange.DallarInfo.VolumeMarket} BTC" + Environment.NewLine +
                $"{ChangeEmoji} {Program.DigitalPriceExchange.DallarInfo.PriceChange} Change in 24 Hours";

            await Context.RespondAsync(Info);
            _ = Context.Message.DeleteAsync();
        }
    }
}
