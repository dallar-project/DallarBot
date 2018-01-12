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
        public DiscordSocketClient discord;
        private CommandService commands;
        private IServiceProvider provider;
        public ConnectionManager client;
        private readonly SettingsHandlerService settings;
        DateTime resetTime;

        public List<WithdrawManager> WithdrawlObjects = new List<WithdrawManager>();

        public async Task InitializeAsync(IServiceProvider _provider)
        {
            provider = _provider;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public RaffleHandlerService(IServiceProvider _provider, DiscordSocketClient _discord, CommandService _commands, SettingsHandlerService _settings)
        {
            discord = _discord;
            commands = _commands;
            provider = _provider;
            settings = _settings;
        }

        public void SetResetTime()
        {
            resetTime = DateTime.Today.AddHours(24);
        }
    }
}
