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

using DallarBot.Classes;
using DallarBot.Services;

namespace DallarBot.Modules
{
    public class MiscCommander : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalHandlerService global;
        private readonly SettingsHandlerService settings;

        public MiscCommander(GlobalHandlerService _global, SettingsHandlerService _settings)
        {
            global = _global;
            settings = _settings;
        }

        [Command("life")]
        public async Task GetLife()
        {
            long localTicks = Context.Guild.CreatedAt.ToLocalTime().Ticks;
            DateTime localDate = new DateTime(localTicks);
            await Context.Channel.SendMessageAsync("Server was created on: " + localDate.ToLongDateString() + "!");
        }

        [Command("count")]
        public async Task GetUserCount()
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("Member count is: " + Context.Guild.MemberCount);
        }

        [Command("ping")]
        public async Task PingPong()
        {
            await Context.Channel.SendMessageAsync("Pong!" + Environment.NewLine + "Discord Server responded in: " + global.discord.Latency.ToString() + "ms");
        }

        [Command("help")]
        public async Task GetHelp()
        {
            if (settings.dallarSettings.helpCommands != null)
            {
                string helpString = "```" + Environment.NewLine + "DALLAR COMMANDS" + Environment.NewLine;
                foreach (var item in settings.dallarSettings.helpCommands)
                {
                    helpString += item.command + " - " + item.description + Environment.NewLine;
                }
                helpString += "```";
                await Context.User.SendMessageAsync(helpString);
            }
            await Context.Message.DeleteAsync();
        }
    }
}
