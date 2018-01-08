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
        public SettingsToken startup { get; set; }
        public SettingsRPC rpc { get; set; }
        public List<SettingsGuild> guilds { get; set; }
        public List<SettingsHelp> helpCommands { get; set; }
    }

    public class SettingsToken
    {
        public string token { get; set; }
        public string taskName { get; set; }
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
        public SettingsTX tx { get; set; }
    }

    public class SettingsTX
    {
        public decimal txfee { get; set; } = 0.0002M;
        public string feeAccount { get; set; } = "txaccount";
    }

    
    public class SettingsHelp
    {
        public string command { get; set; }
        public string description { get; set; }
    }

    public class SettingsHandlerService
    {
        public DiscordSocketClient discord;
        private CommandService commands;
        private IServiceProvider provider;
        public DiscordSettings dallarSettings;

        public async Task InitializeAsync(IServiceProvider _provider)
        {
            provider = _provider;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public SettingsHandlerService(IServiceProvider _provider, DiscordSocketClient _discord, CommandService _commands)
        {
            discord = _discord;
            commands = _commands;
            provider = _provider;

            if (System.IO.File.Exists(Environment.CurrentDirectory + "/settings.json"))
            {
                var loadedString = System.IO.File.ReadAllText(Environment.CurrentDirectory + "/settings.json");
                dallarSettings = JsonConvert.DeserializeObject<DiscordSettings>(loadedString);
            }
        }
    }
}