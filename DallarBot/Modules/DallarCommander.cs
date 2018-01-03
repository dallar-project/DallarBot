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
    public class DallarCommander : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalHandlerService global;

        public DallarCommander(GlobalHandlerService _global)
        {
            global = _global;
        }

        [Command("get-info")]
        public async Task GetDallarInformation()
        {
            global.client.CreateNewAddressForUser();
            await Context.Channel.SendMessageAsync("Dallar difficulty is currently: ");
        }

        [Command("get-difficulty")]
        public async Task GetDallarDifficulty()
        {
            await Context.Channel.SendMessageAsync("Dallar difficulty is currently: " + global.client.GetDifficulty() + Environment.NewLine + "With " + global.client.GetConnectionCount() + " connections.");
        }
    }
}
