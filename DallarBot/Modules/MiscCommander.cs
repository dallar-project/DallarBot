using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Net;

using DallarBot.Classes;
using DallarBot.Services;
using DallarBot.Exchanges;

namespace DallarBot.Modules
{
    public class MiscCommander : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalHandlerService global;

        public MiscCommander(GlobalHandlerService _global)
        {
            global = _global;
        }

        //[Command("life")]
        //public async Task GetLife()
        //{
        //    long localTicks = Context.Guild.CreatedAt.ToLocalTime().Ticks;
        //    DateTime localDate = new DateTime(localTicks);
        //    await Context.Channel.SendMessageAsync("Server was created on: " + localDate.ToLongDateString() + "!");
        //}

        //[Command("count")]
        //public async Task GetUserCount()
        //{
        //    await Context.Message.DeleteAsync();
        //    await Context.Channel.SendMessageAsync("Member count is: " + Context.Guild.MemberCount);
        //}

        //[Command("ping")]
        //public async Task PingPong()
        //{
        //    await Context.Channel.SendMessageAsync("Pong!" + Environment.NewLine + "Discord Server responded in: " + global.discord.Latency.ToString() + "ms");
        //}

        //[Command("help")]
        //public async Task GetHelp()
        //{
        //    if (settings.dallarSettings.helpCommands != null)
        //    {
        //        string helpString = "```" + Environment.NewLine + "DALLAR COMMANDS" + Environment.NewLine;
        //        foreach (var item in settings.dallarSettings.helpCommands)
        //        {
        //            helpString += item.command + " - " + item.description + Environment.NewLine;
        //        }
        //        helpString += "```";
        //        await Context.User.SendMessageAsync(helpString);
        //    }
        //    await Context.Message.DeleteAsync();
        //}

        [Command("giveafuck")]
        public async Task GiveAFuck()
        {
            var user = Context.User as SocketGuildUser;
            if (global.isUserAdmin(user) || global.isUserModerator(user) || global.isUserDevTeam(user))
            {
                await Context.Channel.SendMessageAsync(":regional_indicator_g: :regional_indicator_i: :regional_indicator_v: :regional_indicator_e: :a: :regional_indicator_f: :regional_indicator_u: :regional_indicator_c: :regional_indicator_k:");
            }
        }

        [Command("dal")]
        [Alias("dal-btc", "dal-usd", "dalvalue")]
        public async Task DallarValueInfo()
        {
            await DallarValueInfo(1m);
        }

        [Command("dal")]
        [Alias("dal-btc", "dal-usd", "dalvalue")]
        public async Task DallarValueInfo(decimal amount)
        {
            await global.FetchValueInfo();

            var user = Context.User as SocketGuildUser;

            var client = new WebClient();
            var jsonString = await client.DownloadStringTaskAsync("https://digitalprice.io/markets/get-currency-summary?currency=BALANCE_COIN_BITCOIN");
            var btcPrice = await client.DownloadStringTaskAsync("https://blockchain.info/tobtc?currency=USD&value=1");

            var dalConverter = DigitalPriceDallarInfo.FromJson(jsonString);




            for (int i = 0; i < dalConverter.Length; i++)
            {
                if (dalConverter[i].MiniCurrency == "dal-btc")
                {
                    var dalToUSD = decimal.Round((dalConverter[i].Price / Convert.ToDecimal(btcPrice)) * amount, 8, MidpointRounding.AwayFromZero);
                    var percentChange = 0.0f;
                    float.TryParse(dalConverter[i].PriceChange.TrimEnd('%'), out percentChange);
                    var changeEmoji = percentChange >= 0.0f ? ":chart_with_upwards_trend:" : ":chart_with_downwards_trend:";

                    var Info = amount.ToString() + " DAL to BTC: " + decimal.Round((dalConverter[i].Price * amount * 100000000.0m), 0, MidpointRounding.AwayFromZero).ToString() + " sats" + Environment.NewLine +
                        amount.ToString() + " DAL to USD: $" + dalToUSD.ToString() + Environment.NewLine +
                        "24 Hour Stats: :arrow_down_small: " + decimal.Round((dalConverter[i].Low.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero).ToString() + " sats / :arrow_up_small: " + decimal.Round((dalConverter[i].High.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero).ToString() + " sats / :arrows_counterclockwise: " + dalConverter[i].VolumeMarket.ToString() + Environment.NewLine +
                        changeEmoji + " " + dalConverter[i].PriceChange + " Change in 24 Hours" + Environment.NewLine;

                    await Context.Channel.SendMessageAsync(Info);
                }
            }
        }
    }
}
