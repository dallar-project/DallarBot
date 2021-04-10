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
using DSharpPlus.Interactivity.Extensions;

namespace DallarBot
{
    public class Program
    {
        static DiscordShardedClient DiscordClient;
        public static SettingsHandlerService SettingsHandler;
        public static DaemonClient DaemonClient;
        //public static DigitalPriceExchangeService DigitalPriceExchange;
        public static RandomManagerService RandomManager;
        public static YoMommaJokeService YoMommaJokes;

        static void Main(string[] args)
        {
            SettingsHandler = SettingsHandlerService.FromConfig();
            //DigitalPriceExchange = new DigitalPriceExchangeService();
            RandomManager = new RandomManagerService();
            YoMommaJokes = new YoMommaJokeService();

            DaemonClient = new DaemonClient(SettingsHandler.Daemon.IpAddress + ":" + SettingsHandler.Daemon.Port)
            {
                credentials = new NetworkCredential(SettingsHandler.Daemon.Username, SettingsHandler.Daemon.Password)
            };

            // Ensure fee account is already created
            DaemonClient.GetWalletAddressFromAccount(SettingsHandler.Dallar.FeeAccount, true, out string toWallet);

            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine(LogHandlerService.CenterString("▒█▀▀▄ █▀▀█ █░░ █░░ █▀▀█ █▀▀█ ▒█▀▀█ █▀▀█ ▀▀█▀▀ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█░▒█ █▄▄█ █░░ █░░ █▄▄█ █▄▄▀ ▒█▀▀▄ █░░█ ░░█░░ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█▄▄▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀░░▀ ▀░▀▀ ▒█▄▄█ ▀▀▀▀ ░░▀░░ "));

            Console.WriteLine();
            Console.WriteLine(LogHandlerService.CenterString("----------------------------"));
            Console.WriteLine();

            Console.WriteLine(LogHandlerService.CenterString("Initializing bot..."));

            DiscordClient = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = SettingsHandler.Discord.BotToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
            });

            //DiscordClient.DebugLogger.LogMessageReceived += LogHandlerService.DiscordLogMessageReceived;

            DiscordClient.MessageCreated += async (s, e) =>
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

            var Values = await DiscordClient.GetCommandsNextAsync();

            foreach (System.Collections.Generic.KeyValuePair<int, CommandsNextExtension> CommandsModule in Values)
            {
                CommandsModule.Value.RegisterCommands<HelpCommands>();
                CommandsModule.Value.RegisterCommands<TipCommands>();
                CommandsModule.Value.RegisterCommands<MiscCommands>();
                CommandsModule.Value.RegisterCommands<ExchangeCommands>();
                CommandsModule.Value.RegisterCommands<DallarCommands>();
                CommandsModule.Value.SetHelpFormatter<HelpFormatter>();
                CommandsModule.Value.CommandErrored += async (s, e) =>
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

                    //await e.Context.Client.GetCommandsNext().SudoAsync(e.Context.User, Channel, "d!help " + e.Command.Name);
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
