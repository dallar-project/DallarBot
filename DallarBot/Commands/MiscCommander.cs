using System.Threading.Tasks;
using System.Net;

using DallarBot.Services;
using System.Net.Http;
using Newtonsoft.Json;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DallarBot.Classes;

namespace DallarBot.Commands
{
    public class MiscCommands
    {
        [Command("giveafuck")]
        public async Task GiveAFuck(CommandContext Context)
        {
            if (DiscordHelpers.IsUserAdmin(Context) || DiscordHelpers.IsUserModerator(Context) || DiscordHelpers.IsUserDallarDevTeam(Context))
            {
                await LogHandlerService.LogUserActionAsync(Context, "Invoked GiveAFuck");
                await Context.TriggerTypingAsync();
                await Context.RespondAsync(":regional_indicator_g: :regional_indicator_i: :regional_indicator_v: :regional_indicator_e: :a: :regional_indicator_f: :regional_indicator_u: :regional_indicator_c: :regional_indicator_k:");
            }
        }

        /** Dad Joke API Puller */

        [Command("dad")]
        public async Task FetchDadJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked Dad Joke");
            await Context.TriggerTypingAsync();

            var client = new WebClient();
            client.Headers.Add("Accept", "text/plain");

            var joke = await client.DownloadStringTaskAsync("https://icanhazdadjoke.com/");
            await Context.RespondAsync($"{Context.User.Mention} : {joke}");
        }

        /** Allar (Chuck Norris) API Puller */

        [Command("allar")]
        public async Task FetchAllarJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked Allar Joke");
            await Context.TriggerTypingAsync();

            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync("http://api.icndb.com/jokes/random?firstName=Michael&lastName=Allar");
            
            try
            {
                dynamic jokeResult = JsonConvert.DeserializeObject(content);
                string joke = jokeResult.value.joke;
                joke = System.Net.WebUtility.HtmlDecode(joke);
                await Context.RespondAsync($"{Context.User.Mention} : {joke}");
            }
            catch
            {
                await LogHandlerService.LogUserActionAsync(Context, "Failed to invoke Allar Joke");
                await Context.RespondAsync($"{Context.User.Mention}: Failed to fetch joke. Please contact an Administrator.");
            }            
        }

        [Command("momma")]
        [Aliases("mama","mom", "mum")]
        public async Task FetchMommaJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked mom joke.");
            await Context.TriggerTypingAsync();
            await Context.RespondAsync($"{Context.User.Mention}: {Program.YoMommaJokes.GetRandomYoMommaJoke()}");
        }
    }
}
