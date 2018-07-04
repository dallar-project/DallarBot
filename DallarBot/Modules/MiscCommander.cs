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

        /** USD to Dallar **/

        [Command("usd")]
        [Alias("usd-dal")]
        public async Task USDToDallar()
        {
            await USDToDallar(1m);
        }

        [Command("usd")]
        [Alias("usd-dal")]
        public async Task USDToDallar(decimal amount)
        {
            await global.FetchValueInfo();
            var Info = String.Format("{0:#,##0.########}", amount) + " USD is " + String.Format("{0:#,##0.########}", decimal.Round(amount / global.usdValue, 8)) + " DAL.";
            await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + Info);
        }

        /** BTC to Dallar */

        [Command("btc")]
        [Alias("btc-dal")]
        public async Task BTCToDallar()
        {
            await BTCToDallar(1m);
        }

        [Command("btc")]
        [Alias("btc-dal")]
        public async Task BTCToDallar(decimal amount)
        {
            await global.FetchValueInfo();
            var Info = String.Format("{0:#,##0.########}", amount) + " BTC is " + String.Format("{0:#,##0.########}", decimal.Round(amount / global.DallarInfo.Price, 8)) + " DAL.";
            await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + Info);
        }

        /** Dallar to BTC/USD */

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

            var percentChange = 0.0f;
            float.TryParse(global.DallarInfo.PriceChange.TrimEnd('%'), out percentChange);
            var changeEmoji = percentChange >= 0.0f ? ":chart_with_upwards_trend:" : ":chart_with_downwards_trend:";

            var Info = String.Format("{0:#,##0.########}", amount) + " DAL to BTC: " + String.Format("{0:#,##0.########}", decimal.Round((global.DallarInfo.Price * amount), 8, MidpointRounding.AwayFromZero)) + " BTC" + Environment.NewLine +
                String.Format("{0:#,##0.########}", amount) + " DAL to USD: $" + global.usdValue * amount + " :dollar:" + Environment.NewLine +
                "24 Hour Stats: :arrow_down_small: " + decimal.Round((global.DallarInfo.Low.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero) + " sats / :arrow_up_small: " + decimal.Round((global.DallarInfo.High.GetValueOrDefault() * 100000000.0m), 0, MidpointRounding.AwayFromZero) + " sats / :arrows_counterclockwise: " + global.DallarInfo.VolumeMarket + Environment.NewLine +
                changeEmoji + " " + global.DallarInfo.PriceChange + " Change in 24 Hours" + Environment.NewLine;

            await Context.Channel.SendMessageAsync(Info);
        }

        /** Dad Joke API Puller */

        [Command("dad")]
        public async Task FetchDadJoke()
        {
            var client = new WebClient();
            client.Headers.Add("Accept", "text/plain");

            var dadJoke = await client.DownloadStringTaskAsync("https://icanhazdadjoke.com/");

            await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + dadJoke);
        }
    }
}
