using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DallarBot.Services;
using DallarBot.Classes;
using DSharpPlus.Entities;

namespace DallarBot.Commands
{
    [RequireBotPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class ExchangeCommands : BaseCommandModule
    {
        /** USD to Dallar **/

        [Command("usd")]
        [Aliases("usd-dal")]
        [Description("Converts USD to Dallar")]
        [HelpCategory("Exchange")]
        public async Task USDToDallar(CommandContext Context, [Description("Optional amount of USD to covert to Dallar, default is 1")] params decimal[] Amount)
        {
            decimal ParsedAmount = 1m;
            if (Amount.Length > 0)
            {
                ParsedAmount = Amount[0];
            }
            await LogHandlerService.LogUserActionAsync(Context, $"Invoked USD command with amount {ParsedAmount}.");

            if (true)//!Program.DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: It appears that Dallar Bot is unable to evaluate the price of Dallar at the moment. Perhaps an exchange is down?");
                return;
            }

            await Context.TriggerTypingAsync();            

            var Info = "Error";//String.Format("{0:#,##0.00000000}", Amount) + " USD is " + String.Format("{0:#,##0.00000000}", decimal.Round(ParsedAmount / PriceInfo.USDValue.GetValueOrDefault(), 8)) + " DAL.";
            if (true)//bPriceStale)
            {
                Info += "\n:warning: Info potentially out of date due to Exchange API lag.";
            }

            await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: {Info}");
        }

        /** BTC to Dallar */

        [Command("btc")]
        [Aliases("btc-dal")]
        [Description("Converts BTC to Dallar")]
        [HelpCategory("Exchange")]
        public async Task BTCToDallar(CommandContext Context, [Description("Optional amount of BTC to covert to Dallar, default is 1")] params decimal[] Amount)
        {
            decimal ParsedAmount = 1m;
            if (Amount.Length > 0)
            {
                ParsedAmount = Amount[0];
            }

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked BTC command with amount {ParsedAmount}.");

            if (true)//!Program.DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: It appears that Dallar Bot is unable to evaluate the price of Dallar at the moment. Perhaps an exchange is down?");
                return;
            }

            await Context.TriggerTypingAsync();

            var Info = "error";//String.Format("{0:#,##0.00000000}", ParsedAmount) + " BTC is " + String.Format("{0:#,##0.00000000}", decimal.Round(ParsedAmount / PriceInfo.Price, 8)) + " DAL.";
            if (true)//bPriceStale)
            {
                Info += "\n:warning: Info potentially out of date due to Exchange API lag.";
            }

            await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: {Info}");
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

            if (true)//!Program.DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: It appears that Dallar Bot is unable to evaluate the price of Dallar at the moment. Perhaps an exchange is down?");
                return;
            }

            // float.TryParse(PriceInfo.PriceChange.TrimEnd('%'), out float PercentChange);
            // string ChangeEmoji = PercentChange >= 0.0f ? ":chart_with_upwards_trend:" : ":chart_with_downwards_trend:";

            // decimal UsdValue = PriceInfo.USDValue.GetValueOrDefault();

            // var Info = $"{ParsedAmount} DAL to BTC: {decimal.Round((PriceInfo.Price * ParsedAmount), 8, MidpointRounding.AwayFromZero):F8} BTC" + Environment.NewLine +
            //     $"{ParsedAmount} DAL to USD: ${UsdValue * ParsedAmount} :dollar:" + Environment.NewLine +
            //     $"24 Hour Stats: :arrow_down_small: {decimal.Round((PriceInfo.Low.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrow_up_small: {decimal.Round((PriceInfo.High.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero)} sats / :arrows_counterclockwise: {PriceInfo.VolumeMarket} BTC" + Environment.NewLine +
            //     $"{ChangeEmoji} {PriceInfo.PriceChange} Change in 24 Hours";

            // if (bPriceStale)
            // {
            //     Info += "\n:warning: Info potentially out of date due to Exchange API lag.";
            // }

            //await DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: {Info}");
        }
    }
}
