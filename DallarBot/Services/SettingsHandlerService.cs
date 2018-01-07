using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Runtime.Serialization;

using DallarBot.Classes;

namespace DallarBot.Services
{
    public class DiscordSettings
    {
        public SettingsRPC rpc { get; set; }
        public List<SettingsGuild> guilds { get; set; }
        public List<SettingsHelp> helpCommands { get; set; }
    }

    public class SettingsRPC
    {
        public string ipaddress { get; set; }
        public string port { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class SettingsGuild
    {
        public string displayName { get; set; }
        public ulong guildID { get; set; }
    }
    
    public class SettingsHelp
    {
        public string command { get; set; }
        public string description { get; set; }
    }

    public class SettingsHandlerService
    {
        public DiscordSocketClient _discord;
        private CommandService _commands;
        private IServiceProvider _provider;
        public DiscordSettings settings;

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public SettingsHandlerService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            if (System.IO.File.Exists(Environment.CurrentDirectory + "/settings.json"))
            {
                var loadedString = System.IO.File.ReadAllText(Environment.CurrentDirectory + "/settings.json");
                settings = JsonConvert.DeserializeObject<DiscordSettings>(loadedString);
            }
        }
    }
}