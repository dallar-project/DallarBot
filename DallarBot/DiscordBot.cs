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
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace Dallar.Bots
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordBot(
            this IServiceCollection services)
        {
            services.AddSingleton<IDiscordBot, DiscordBot>();
            return services;
        }
    }

    public interface IDiscordBot
    {
        
    }

    public class DiscordBot : IDiscordBot
    {
        static DiscordShardedClient DiscordClient;

        protected IDallarSettingsCollection DallarSettingsCollection;
        protected IDallarPriceProviderService DallarPriceProviderService;
        protected IDallarClientService DallarClientService;
        protected IFunServiceCollection FunServiceCollection;
        protected IRandomManagerService RandomManagerService;
        protected IDallarAccountOverrider DallarAccountOverrider;

        protected IServiceProvider CommandServiceProvider;

        public DiscordBot(IDallarSettingsCollection DallarSettingsCollection, IDallarClientService DallarClientService, IDallarPriceProviderService DallarPriceProviderService, IFunServiceCollection FunServiceCollection, IRandomManagerService RandomManagerService, IDallarAccountOverrider AccountOverrider)
        {
            this.DallarSettingsCollection = DallarSettingsCollection;
            this.DallarClientService = DallarClientService;
            this.DallarPriceProviderService = DallarPriceProviderService;
            this.FunServiceCollection = FunServiceCollection;
            this.RandomManagerService = RandomManagerService;
            this.DallarAccountOverrider = AccountOverrider;

            Console.WriteLine("Initialising Discord bot...");

            DiscordClient = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = DallarSettingsCollection.Discord.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug
            });

            DiscordClient.DebugLogger.LogMessageReceived += LogHandlerExtensions.DiscordLogMessageReceived;

            CommandServiceProvider = new ServiceCollection()
                .AddSingleton(this.DallarSettingsCollection)
                .AddSingleton(this.DallarClientService)
                .AddSingleton(this.DallarPriceProviderService)
                .AddSingleton(this.FunServiceCollection)
                .AddSingleton(this.RandomManagerService)
                .AddSingleton(this.DallarAccountOverrider)
                .BuildServiceProvider();

            StartUpBot();
        }

        protected async void StartUpBot()
        {
            await DiscordClient.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "d!" },
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = false,
                Services = CommandServiceProvider
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
                        await e.Context.Message.DeleteAsync();
                    }

                    await e.Context.Client.GetCommandsNext().SudoAsync(e.Context.User, Channel, "d!help " + e.Command.Name);
                };
            }

            await DiscordClient.UseInteractivityAsync(new InteractivityConfiguration()
            {
                PaginationBehavior = TimeoutBehaviour.DeleteMessage,
                PaginationTimeout = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(30)
            });

            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }

        public DallarAccount DallarAccountFromDiscordUser(DiscordUser User)
        {
            var acc = new DallarAccount()
            {
                AccountId = User.Id.ToString(),
                AccountPrefix = "" // @TODO: Make config driven
            };

            DallarAccountOverrider?.OverrideDallarAccountIfNeeded(ref acc);
            return acc;
        }
    }
}
