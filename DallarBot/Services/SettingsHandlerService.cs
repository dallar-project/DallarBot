using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DallarBot.Services
{
    public class DiscordSettings
    {
        public string BotToken { get; set; }
        public string BotTaskName { get; set; }
    }

    public class DaemonSettings
    {
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class DallarSettings
    {
        public decimal Txfee { get; set; } = 0.0002M;
        public string FeeAccount { get; set; } = "txaccount";
    }

    public class SettingsHandlerService
    {
        public DiscordSettings Discord { get; set; }
        public DaemonSettings Daemon { get; set; }
        public DallarSettings Dallar { get; set; }

        public SettingsHandlerService()
        {
        }

        public static SettingsHandlerService FromConfig()
        {
            if (System.IO.File.Exists(Environment.CurrentDirectory + "/settings.json"))
            {
                var loadedString = System.IO.File.ReadAllText(Environment.CurrentDirectory + "/settings.json");
                return JsonConvert.DeserializeObject<SettingsHandlerService>(loadedString);
            }

            LogHandlerService.Log("Unable to load config file.");
            return new SettingsHandlerService();
        }
    }
}