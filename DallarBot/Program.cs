using System;
using System.Net;
using System.Threading.Tasks;
using DallarBot.Commands;
using DallarBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace DallarBot
{
    public class Program
    {
        static DiscordShardedClient DiscordClient;
        public static SettingsHandlerService SettingsHandler;
        public static DaemonService Daemon;
        public static DigitalPriceExchangeService DigitalPriceExchange;
        public static RandomManagerService RandomManager;
        public static YoMommaJokeService YoMommaJokes;

        static void Main(string[] args)
        {
            SettingsHandler = SettingsHandlerService.FromConfig();
            DigitalPriceExchange = new DigitalPriceExchangeService();
            RandomManager = new RandomManagerService();
            YoMommaJokes = new YoMommaJokeService();

            Daemon = new DaemonService(SettingsHandler.Daemon.IpAddress + ":" + SettingsHandler.Daemon.Port)
            {
                credentials = new NetworkCredential(SettingsHandler.Daemon.Username, SettingsHandler.Daemon.Password)
            };

            // Ensure fee account is already created
            Daemon.GetWalletAddressFromUser(SettingsHandler.Dallar.FeeAccount, true, out string toWallet);

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

            await DiscordClient.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "d!" },
                EnableDms = true,
                EnableMentionPrefix = true
            });



            foreach (CommandsNextExtension CommandsModule in DiscordClient.GetCommandsNext().Values)
            {
                CommandsModule.RegisterCommands<TipCommands>();
                CommandsModule.RegisterCommands<MiscCommands>();
                CommandsModule.RegisterCommands<ExchangeCommands>();
                CommandsModule.RegisterCommands<DallarCommands>();
                CommandsModule.SetHelpFormatter<HelpFormatter>();
            }

            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }
    }
}
