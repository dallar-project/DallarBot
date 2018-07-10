using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dallar
{
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

    public class DiscordSettings
    {
        public string BotToken { get; set; }
        public string BotTaskName { get; set; }
    }

    public class TwitchSettings
    {

    }

    public class DallarSettingsCollection
    {
        public DiscordSettings Discord { get; set; }
        public DaemonSettings Daemon { get; set; }
        public DallarSettings Dallar { get; set; }

        public DallarSettingsCollection()
        {
        }

        public static bool FromConfig(string ConfigFilePath, out DallarSettingsCollection LoadedSettings)
        {
            if (System.IO.File.Exists(ConfigFilePath))
            {
                var loadedString = System.IO.File.ReadAllText(ConfigFilePath);
                LoadedSettings =  JsonConvert.DeserializeObject<DallarSettingsCollection>(loadedString);
                return true;
            }

            LoadedSettings = null;
            return false;
        }
    }
}