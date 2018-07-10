using System;
using System.Net;
using System.Threading.Tasks;
using Dallar;
using DallarBot.Classes;
using DallarBot.Commands;
using DallarBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace DallarBot
{
    public class Program
    {
        static DiscordShardedClient DiscordClient;
        public static DallarSettingsCollection SettingsCollection;
        public static DaemonClient DaemonClient;
        public static DigitalPriceExchangeService DigitalPriceExchange;
        public static RandomManagerService RandomManager;
        public static YoMommaJokeService YoMommaJokes;

        static void Main(string[] args)
        {
            DallarSettingsCollection.FromConfig(Environment.CurrentDirectory + "/settings.json", out SettingsCollection);
            DigitalPriceExchange = new DigitalPriceExchangeService();
            RandomManager = new RandomManagerService();
            YoMommaJokes = new YoMommaJokeService();

            DaemonClient = new DaemonClient(SettingsCollection.Daemon.IpAddress + ":" + SettingsCollection.Daemon.Port)
            {
                credentials = new NetworkCredential(SettingsCollection.Daemon.Username, SettingsCollection.Daemon.Password)
            };

            // Ensure fee account is already created
            DaemonClient.GetWalletAddressFromAccount(SettingsCollection.Dallar.FeeAccount, true, out string toWallet);

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
                Token = SettingsCollection.Discord.BotToken,
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
                EnableMentionPrefix = true,
                EnableDefaultHelp = false
            });

            foreach (CommandsNextExtension CommandsModule in DiscordClient.GetCommandsNext().Values)
            {
                CommandsModule.RegisterCommands<HelpCommands>();
                CommandsModule.RegisterCommands<TipCommands>();
                CommandsModule.RegisterCommands<MiscCommands>();
                CommandsModule.RegisterCommands<ExchangeCommands>();
                CommandsModule.RegisterCommands<DallarCommands>();
                CommandsModule.SetHelpFormatter<HelpFormatter>();
                CommandsModule.CommandErrored += async e =>
                {
                    // first command failure on boot throws a null exception. Not sure why?
                    // Afterwards, this event logic always seems to work okay without error

                    if (e.Exception is ChecksFailedException)
                    {
                        return;
                    }

                    if (e.Command == null)
                    {
                        return;
                    }
                    await LogHandlerService.LogUserActionAsync(e.Context, "Failed to invoke " + e.Command.ToString());

                    DiscordChannel Channel = e.Context.Channel;
                    if (e.Context.Member != null)
                    {
                        Channel = await e.Context.Member.CreateDmChannelAsync();
                    }

                    await e.Context.Client.GetCommandsNext().SudoAsync(e.Context.User, Channel, "d!help " + e.Command.Name);
                    DiscordHelpers.DeleteNonPrivateMessage(e.Context);
                };

            }


            await DiscordClient.UseInteractivityAsync(new InteractivityConfiguration
            {

            });

            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }
    }
}
