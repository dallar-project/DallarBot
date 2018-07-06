using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DallarBot.Services
{
    public class DiscordSettings
    {
        public SettingsToken Startup { get; set; }
        public SettingsRPC Rpc { get; set; }
        public SettingsTX TX { get; set; }
        public List<SettingsHelp> HelpCommands { get; set; }    
    }

    public class SettingsToken
    {
        public string Token { get; set; }
        public string TaskName { get; set; }
    }

    public class SettingsRPC
    {
        public string Ipaddress { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SettingsTX
    {
        public decimal Txfee { get; set; } = 0.0002M;
        public string FeeAccount { get; set; } = "txaccount";
    }

    public class SettingsHelp
    {
        public string Command { get; set; }
        public string Description { get; set; }
    }

    public class SettingsHandlerService
    {
        public DiscordSettings dallarSettings;

        public SettingsHandlerService()
        {
            if (System.IO.File.Exists(Environment.CurrentDirectory + "/settings.json"))
            {
                var loadedString = System.IO.File.ReadAllText(Environment.CurrentDirectory + "/settings.json");
                dallarSettings = JsonConvert.DeserializeObject<DiscordSettings>(loadedString);
            }
        }
    }
}