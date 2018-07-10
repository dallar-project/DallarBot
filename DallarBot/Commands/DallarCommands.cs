using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Classes;
using Dallar;
using DallarBot.Services;

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

            int BlockCount = Program.DaemonClient.GetBlockCount();
            float Difficulty = Program.DaemonClient.GetDifficulty();

            LogHandlerExtensions.LogUserAction(Context, $"Fetched block details. {BlockCount} with difficulty {Difficulty}.");
            await Context.RespondAsync($"{Context.User.Mention}: Difficulty for block {BlockCount}: {Difficulty}");
        }
    }
}
