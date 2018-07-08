using System.Threading.Tasks;
using System.Net;

using DallarBot.Services;
using System.Net.Http;
using Newtonsoft.Json;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DallarBot.Classes;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;

namespace DallarBot.Commands
{
    [RequireBotPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class MiscCommands : BaseCommandModule
    {
        /** Dad Joke API Puller */

        [Command("dad")]
        [HelpCategory("Joke")]
        [Description("Fetches a dad joke")]
        [Cost(1.0f)]
        public async Task FetchDadJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked Dad Joke");

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context, 1))
            {
                await LogHandlerService.LogUserActionAsync(Context, "Failed charged for dad Joke");
                return;
            }

            await LogHandlerService.LogUserActionAsync(Context, "Successfully charged for dad Joke");

            await Context.TriggerTypingAsync();

            var client = new WebClient();
            client.Headers.Add("Accept", "text/plain");

            var joke = await client.DownloadStringTaskAsync("https://icanhazdadjoke.com/");
            await Context.RespondAsync($"{Context.User.Mention} : {joke}");
        }

        /** Allar (Chuck Norris) API Puller */

        [Command("allar")]
        [HelpCategory("Joke")]
        [Description("Fetches an Allar (Chuck Norris) joke")]
        [Cost(1.0f)]
        public async Task FetchAllarJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked Allar Joke");

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context, 1))
            {
                await LogHandlerService.LogUserActionAsync(Context, "Failed charged for Allar Joke");
                return;
            }

            await LogHandlerService.LogUserActionAsync(Context, "Successfully charged for Allar Joke");

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
                await LogHandlerService.LogUserActionAsync(Context, "Failed to perform Allar Joke");
                await Context.RespondAsync($"{Context.User.Mention}: Failed to fetch joke. Please contact an Administrator.");
            }
        }

        [Command("momma")]
        [Aliases("mama","mom", "mum")]
        [HelpCategory("Joke")]
        [Description("Fetches a Yo Momma joke")]
        [Cost(1.0f)]
        public async Task FetchMommaJoke(CommandContext Context)
        {
            await LogHandlerService.LogUserActionAsync(Context, "Invoked mom joke.");

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context, 1))
            {
                await LogHandlerService.LogUserActionAsync(Context, "Failed charged for mom Joke");
                return;
            }

            await LogHandlerService.LogUserActionAsync(Context, "Successfully charged for mom Joke");

            await Context.TriggerTypingAsync();
            await Context.RespondAsync($"{Context.User.Mention}: {Program.YoMommaJokes.GetRandomYoMommaJoke()}");
        }

        [Command("giveafuck")]
        [HelpCategory("Misc")]
        [Description("Emits the Emoji string GIVE A FUCK if user has permission to do so")]
        public async Task GiveAFuck(CommandContext Context)
        {
            if (DiscordHelpers.IsUserAdmin(Context) || DiscordHelpers.IsUserModerator(Context) || DiscordHelpers.IsUserDallarDevTeam(Context))
            {
                await LogHandlerService.LogUserActionAsync(Context, "Invoked GiveAFuck");
                await Context.TriggerTypingAsync();
                await Context.RespondAsync(":regional_indicator_g: :regional_indicator_i: :regional_indicator_v: :regional_indicator_e: :a: :regional_indicator_f: :regional_indicator_u: :regional_indicator_c: :regional_indicator_k:");
            }
        }
    }
}
