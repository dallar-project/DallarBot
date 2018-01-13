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
    public class RaffleCommander : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalHandlerService global;
        private readonly SettingsHandlerService settings;
        private readonly RaffleHandlerService raffle;

        public RaffleCommander(GlobalHandlerService _global, SettingsHandlerService _settings, RaffleHandlerService _raffle)
        {
            global = _global;
            settings = _settings;
            raffle = _raffle;
        }

        //[Command("drawRaffle")]
        //public async Task DrawRaffle()
        //{
        //    if (raffle.isRaffleRunning)
        //    {
        //        var user = raffle.DrawUser();
        //        if (user != null)
        //        {
        //            await Context.Channel.SendMessageAsync("Congratulations " + user.Mention + " you have won the raffle!");
        //        }
        //    }
        //    else
        //    {
        //        await Context.User.SendMessageAsync("No raffles are currently running");
        //    }
        //}

        //[Command("addAllUsersToRaffle")]
        //public async Task AddEveryone()
        //{
        //    var user = Context.User as SocketGuildUser;
        //    if (global.isUserAdmin(user) || global.isUserModerator(user) || global.isUserDevTeam(user))
        //    {
        //        foreach(var tempUser in Context.Guild.Users)
        //        {
        //            raffle.AddUserToRaffle(tempUser);
        //        }
        //        await Context.Channel.SendMessageAsync("everyone added");
        //    }
        //}
    }
}
