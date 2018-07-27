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
using Dallar.Services;

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
            LogHandlerExtensions.LogUserAction(Context, "Invoked Dad Joke");

            IFunServiceCollection FunServiceCollection = Context.Services.GetService(typeof(IFunServiceCollection)) as IFunServiceCollection;
            string Joke = FunServiceCollection.DadJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"Sorry {Context.Member}, the Dad joke service seems to be down.");
                return;
            }

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context,  1))
            {
                LogHandlerExtensions.LogUserAction(Context, "Failed charged for dad Joke");
                return;
            }

            LogHandlerExtensions.LogUserAction(Context, "Successfully charged for dad Joke");
            await Context.RespondAsync($"{Context.User.Mention} : {Joke}");
        }

        /** Allar (Chuck Norris) API Puller */

        [Command("allar")]
        [HelpCategory("Joke")]
        [Description("Fetches an Allar (Chuck Norris) joke")]
        [Cost(1.0f)]
        public async Task FetchAllarJoke(CommandContext Context)
        {
            LogHandlerExtensions.LogUserAction(Context, "Invoked Allar Joke");

            IFunServiceCollection FunServiceCollection = Context.Services.GetService(typeof(IFunServiceCollection)) as IFunServiceCollection;
            string Joke = FunServiceCollection.ChuckNorrisJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"Sorry {Context.Member}, the Allar joke service seems to be down.");
                return;
            }

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context, 1))
            {
                LogHandlerExtensions.LogUserAction(Context, "Failed charged for Allar Joke");
                return;
            }

            LogHandlerExtensions.LogUserAction(Context, "Successfully charged for Allar Joke");
            await Context.RespondAsync($"{Context.User.Mention} : {Joke}");
        }

        [Command("momma")]
        [Aliases("mama","mom", "mum")]
        [HelpCategory("Joke")]
        [Description("Fetches a Yo Momma joke")]
        [Cost(1.0f)]
        public async Task FetchMommaJoke(CommandContext Context)
        {
            LogHandlerExtensions.LogUserAction(Context, "Invoked mom joke.");

            IFunServiceCollection FunServiceCollection = Context.Services.GetService(typeof(IFunServiceCollection)) as IFunServiceCollection;
            string Joke = FunServiceCollection.YoMommaJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"Sorry {Context.Member}, the Yo Momma joke service seems to be down.");
                return;
            }

            if (!await DiscordHelpers.AttemptChargeDallarForCommand(Context, 1))
            {
                LogHandlerExtensions.LogUserAction(Context, "Failed charged for mom Joke");
                return;
            }

            LogHandlerExtensions.LogUserAction(Context, "Successfully charged for mom Joke");
            await Context.RespondAsync($"{Context.User.Mention}: {Joke}");
        }

        [Command("giveafuck")]
        [HelpCategory("Misc")]
        [Description("Emits the Emoji string GIVE A FUCK if user has permission to do so")]
        public async Task GiveAFuck(CommandContext Context)
        {
            if (DiscordHelpers.IsUserAdmin(Context) || DiscordHelpers.IsUserModerator(Context) || DiscordHelpers.IsUserDallarDevTeam(Context))
            {
                LogHandlerExtensions.LogUserAction(Context, "Invoked GiveAFuck");
                await Context.TriggerTypingAsync();
                await Context.RespondAsync(":regional_indicator_g: :regional_indicator_i: :regional_indicator_v: :regional_indicator_e: :a: :regional_indicator_f: :regional_indicator_u: :regional_indicator_c: :regional_indicator_k:");
            }
        }

        [Command("botinfo")]
        [HelpCategory("Misc")]
        [Description("Displays some general purpose information about the Dallar Bot")]
        public async Task BotIfno(CommandContext Context)
        {
            await Context.TriggerTypingAsync();
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithTitle("Dallar Bot Info");
            embedBuilder.AddField("Bot Statistics", $"{Context.Client.Guilds.Count} Server{(Context.Client.Guilds.Count > 1 ? "s" : "")} across {Context.Client.ShardCount} shard{(Context.Client.ShardCount > 1 ? "s" : "")}.");
            await DiscordHelpers.PromptUserToDeleteMessage(Context, embedBuilder.Build());
        }
    }
}
