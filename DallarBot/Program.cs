using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DallarBot.Classes;
using DallarBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace DallarBot
{
    public class Program
    {
        static DiscordShardedClient DiscordClient;
        static Dictionary<int, CommandsNextModule> DiscordCommands;
        public static SettingsHandlerService SettingsHandler;
        public static DaemonService Daemon;
        public static DigitalPriceExchangeService DigitalPriceExchange;

        static void Main(string[] args)
        {
            SettingsHandler = new SettingsHandlerService();
            DigitalPriceExchange = new DigitalPriceExchangeService();

            Daemon = new DaemonService(Program.SettingsHandler.Daemon.IpAddress + ":" + Program.SettingsHandler.Daemon.Port)
            {
                credentials = new NetworkCredential(Program.SettingsHandler.Daemon.Username, Program.SettingsHandler.Daemon.Password)
            };

            // Ensure fee account is already created
            Daemon.GetWalletAddressFromUser(Program.SettingsHandler.Dallar.FeeAccount, true, out string toWallet);

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine(LogHandlerService.CenterString("▒█▀▀▄ █▀▀█ █░░ █░░ █▀▀█ █▀▀█ ▒█▀▀█ █▀▀█ ▀▀█▀▀ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█░▒█ █▄▄█ █░░ █░░ █▄▄█ █▄▄▀ ▒█▀▀▄ █░░█ ░░█░░ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█▄▄▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀░░▀ ▀░▀▀ ▒█▄▄█ ▀▀▀▀ ░░▀░░ "));

            Console.WriteLine();
            Console.WriteLine(LogHandlerService.CenterString("----------------------------"));
            Console.WriteLine();

            Console.WriteLine(LogHandlerService.CenterString("Initialising bot..."));

            DiscordClient = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = SettingsHandler.Discord.BotToken,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug
            });

            DiscordClient.DebugLogger.LogMessageReceived += LogHandlerService.DiscordLogMessageReceived;

            DiscordClient.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };

            DiscordCommands = (Dictionary<int, CommandsNextModule>)DiscordClient.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "d!",
                EnableDms = true,
                EnableMentionPrefix = true
            });

            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }
    }
}
