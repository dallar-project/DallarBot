using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Classes;
using Dallar;
using DallarBot.Services;
using Dallar.Services;

namespace DallarBot.Commands
{
    [RequireBotPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class DallarCommands : BaseCommandModule
    {
        [Command("difficulty")]
        [Aliases("diff", "block")]
        [Description("Gets the current block difficulty")]
        [HelpCategory("Dallar")]
        public async Task GetDallarDifficulty(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

            IDallarClientService DallarClientService = Context.Services.GetService(typeof(IDallarClientService)) as IDallarClientService;

            uint BlockCount = DallarClientService.GetBlockCount();
            decimal Difficulty = DallarClientService.GetDifficulty();

            LogHandlerExtensions.LogUserAction(Context, $"Fetched block details. {BlockCount} with difficulty {Difficulty}.");
            await Context.RespondAsync($"{Context.User.Mention}: Difficulty for block {BlockCount}: {Difficulty}");
        }
    }
}
