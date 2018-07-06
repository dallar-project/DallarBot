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
using System.Net.Http;
using SimpleJson;
using Newtonsoft.Json;

namespace DallarBot.Modules
{
    public class MiscCommander
    {
        private readonly GlobalHandlerService global;

        protected string[] YoMommaJokes;
        protected Random YoMommaRandom;

        public MiscCommander(GlobalHandlerService _global)
        {
            global = _global;
            YoMommaJokes = new string[] { };
            YoMommaRandom = new Random();
        }

        [Command("help")]
        public async Task GetHelp()
        {
            //if (settings.dallarSettings.helpCommands != null)
            //{
            //    string helpString = "```" + Environment.NewLine + "DALLAR COMMANDS" + Environment.NewLine;
            //    foreach (var item in settings.dallarSettings.helpCommands)
            //    {
            //        helpString += item.command + " - " + item.description + Environment.NewLine;
            //    }
            //    helpString += "```";
            //    await Context.User.SendMessageAsync(helpString);
            //}
            await Context.Message.DeleteAsync();
        }

        [Command("giveafuck")]
        public async Task GiveAFuck()
        {
            var user = Context.User as SocketGuildUser;
            if (global.IsUserAdmin(user) || global.IsUserModerator(user) || global.IsUserDevTeam(user))
            {
                await LogHandlerService.LogUserAction(Context, "Invoked GiveAFuck");
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

        /** Allar (Chuck Norris) API Puller */

        [Command("allar")]
        public async Task FetchAllarJoke()
        {
            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync("http://api.icndb.com/jokes/random?firstName=Michael&lastName=Allar");

            try
            {
                dynamic jokeResult = JsonConvert.DeserializeObject(content);
                string joke = jokeResult.value.joke;
                joke = System.Net.WebUtility.HtmlDecode(joke);
                await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + joke);
            }
            catch
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention + ": Failed to fetch joke.");
            }            
        }

        public bool PopulateMommaJokes()
        {
            if (YoMommaJokes.Length == 0)
            {
                YoMommaJokes = File.ReadAllLines("momma.txt");
            }

            return YoMommaJokes.Length > 1;
        }

        [Command("momma")]
        [Alias("mama","mom", "mum")]
        public async Task FetchMommaJoke()
        {
            if (PopulateMommaJokes())
            {
                var joke = YoMommaJokes[YoMommaRandom.Next(1, YoMommaJokes.Length)];
                await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + joke);
            }
        }
    }
}
