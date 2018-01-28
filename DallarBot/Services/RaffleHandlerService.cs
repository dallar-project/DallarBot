using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

using DallarBot.Classes;
using DallarBot.Services;

namespace DallarBot.Services
{
    public class RaffleHandlerService
    {
        public ConnectionManager client;
        public DiscordSocketClient discord;
        private readonly SettingsHandlerService settings;

        List<SocketGuildUser> raffleList = new List<SocketGuildUser>();
        public bool isRaffleRunning = false;
        DateTime resetTime;

        public List<WithdrawManager> WithdrawlObjects = new List<WithdrawManager>();
        

        public RaffleHandlerService(DiscordSocketClient _discord, SettingsHandlerService _settings)
        {
            discord = _discord;
            settings = _settings;
        }

        public void AddUserToRaffle(SocketGuildUser user)
        {
            if (!raffleList.Contains(user))
            {
                raffleList.Add(user);
            }
        }

        public SocketGuildUser DrawUser()
        {
            RandomManager manager = new RandomManager(0, raffleList.Count - 1);
            if (manager.result < raffleList.Count)
            {
                var user = raffleList[manager.result];
                var guild = discord.GetGuild(user.Guild.Id);
                if (guild.Users.Contains(user))
                {
                    return user;
                }
            }
            return null;
        }

        public void SetResetTime()
        {
            resetTime = DateTime.Today.AddHours(24);
        }
    }
}
