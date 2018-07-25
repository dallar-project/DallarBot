using System;
using System.Threading.Tasks;
using Dallar.Exchange;
using Dallar.Services;
using DallarBot.Classes;
using DallarBot.Commands;
using DallarBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace Dallar.Bots
{
    public class DiscordBot
    {
        static DiscordShardedClient DiscordClient;
        public static DallarSettingsCollection SettingsCollection;
        public static RandomManagerService RandomManager;

        protected IDallarSettingsCollection DallarSettingsCollection;
        protected IDallarPriceProviderService DallarPriceProviderService;
        protected IDallarClientService DallarClientService;
        protected IFunServiceCollection FunServiceCollection;

        public DiscordBot(IDallarSettingsCollection DallarSettingsCollection, IDallarClientService DallarClientService, IDallarPriceProviderService DallarPriceProviderService, IFunServiceCollection FunServiceCollection)
        {
            this.DallarSettingsCollection = DallarSettingsCollection;
            this.DallarClientService = DallarClientService;
            this.DallarPriceProviderService = DallarPriceProviderService;
            this.FunServiceCollection = FunServiceCollection;

            RandomManager = new RandomManagerService();

            Console.WriteLine(LogHandlerService.CenterString("▒█▀▀▄ █▀▀█ █░░ █░░ █▀▀█ █▀▀█ ▒█▀▀█ █▀▀█ ▀▀█▀▀ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█░▒█ █▄▄█ █░░ █░░ █▄▄█ █▄▄▀ ▒█▀▀▄ █░░█ ░░█░░ "));
            Console.WriteLine(LogHandlerService.CenterString("▒█▄▄▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀░░▀ ▀░▀▀ ▒█▄▄█ ▀▀▀▀ ░░▀░░ "));

            Console.WriteLine();
            Console.WriteLine(LogHandlerService.CenterString("----------------------------"));
            Console.WriteLine();

            Console.WriteLine(LogHandlerService.CenterString("Initialising bot..."));

            DiscordClient = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = SettingsCollection.Discord.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug
            });

            DiscordClient.DebugLogger.LogMessageReceived += LogHandlerExtensions.DiscordLogMessageReceived;

            DiscordClient.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };

            StartUpBot();
        }

        protected async void StartUpBot()
        {
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
                    LogHandlerExtensions.LogUserAction(e.Context, "Failed to invoke " + e.Command.ToString());

                    DiscordChannel Channel = e.Context.Channel;
                    if (e.Context.Member != null)
                    {
                        Channel = await e.Context.Member.CreateDmChannelAsync();
                    }

                    await e.Context.Client.GetCommandsNext().SudoAsync(e.Context.User, Channel, "d!help " + e.Command.Name);
                    DiscordHelpers.DeleteNonPrivateMessage(e.Context);
                };
            }

            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }
    }
}
