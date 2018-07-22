using System;
using System.IO;
using Newtonsoft.Json;

namespace Dallar
{
    public class DaemonSettings
    {
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AccountPrefix { get; set; }
    }

    public class DallarSettings
    {
        public decimal Txfee { get; set; } = 0.0002M;
        public string FeeAccount { get; set; } = "txaccount";
    }

    public class DiscordBotSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Token { get; set; }
        public string TaskName { get; set; }
        public string DallarAccountPrefix { get; set; }
    }

    public class TwitchAuthSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string[] Scopes { get; set; }
    }

    public class TwitchBotSettings
    {
        public string Username { get; set; }
        public string AccessToken { get; set; }
        public string DallarAccountPrefix { get; set; }
    }

    public interface IDallarSettingsCollection
    {
        DaemonSettings Daemon { get; set; }
        DallarSettings Dallar { get; set; }
        DiscordBotSettings Discord { get; set; }
        TwitchAuthSettings TwitchAuth { get; set; }
        TwitchBotSettings TwitchBot { get; set; }
    }

    public class DallarSettingsCollection : IDallarSettingsCollection
    {
        public DaemonSettings Daemon { get; set; }
        public DallarSettings Dallar { get; set; }
        public DiscordBotSettings Discord { get; set; }
        public TwitchAuthSettings TwitchAuth { get; set; }
        public TwitchBotSettings TwitchBot { get; set; }

        private DallarSettingsCollection()
        {
        }

        public static DallarSettingsCollection FromConfig(string ConfigFilePath = "settings.json")
        {
            if (System.IO.File.Exists(ConfigFilePath))
            {
                var loadedString = System.IO.File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<DallarSettingsCollection>(loadedString);
            }
            else
            {
                throw new FileNotFoundException("Could not load configuration file.", ConfigFilePath);
            }
        }
    }
}