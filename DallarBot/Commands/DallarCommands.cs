using System.Linq;
using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Services;
using DallarBot.Classes;
using DSharpPlus.Entities;
using System.Collections.Generic;

namespace DallarBot.Commands
{
    public class DallarCommands
    {
        [Command("difficulty")]
        [Aliases("diff", "block")]
        public async Task GetDallarDifficulty(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

            int BlockCount = Program.Daemon.GetBlockCount();
            float Difficulty = Program.Daemon.GetDifficulty();

            await LogHandlerService.LogUserActionAsync(Context, $"Fetched block details. {BlockCount} with difficulty {Difficulty}.");
            await Context.RespondAsync($"{Context.User.Mention}: Difficulty for block {BlockCount}: {Difficulty}");
        }
    }
}
