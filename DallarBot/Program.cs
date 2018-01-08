using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DallarBot.Services;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace DallarBot
{ 
    class Program : ModuleBase<SocketCommandContext>
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
    
        private DiscordSocketClient client;

        public async Task MainAsync()
        {
            Console.WriteLine(CenterString("▒█▀▀▄ █▀▀█ █░░ █░░ █▀▀█ █▀▀█ ▒█▀▀█ █▀▀█ ▀▀█▀▀ "));
            Console.WriteLine(CenterString("▒█░▒█ █▄▄█ █░░ █░░ █▄▄█ █▄▄▀ ▒█▀▀▄ █░░█ ░░█░░ "));
            Console.WriteLine(CenterString("▒█▄▄▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀░░▀ ▀░▀▀ ▒█▄▄█ ▀▀▀▀ ░░▀░░ "));

            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");

            Console.WriteLine(CenterString("Initialising bot..."));

            client = new DiscordSocketClient();

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandlerService>().InitializeAsync(services);
            await services.GetRequiredService<GlobalHandlerService>().InitializeAsync(services);
            await services.GetRequiredService<SettingsHandlerService>().InitializeAsync(services);

            await client.LoginAsync(TokenType.Bot, services.GetService<SettingsHandlerService>().dallarSettings.startup.token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<GlobalHandlerService>()
                .AddSingleton<SettingsHandlerService>()
                // Add additional services here...
                .BuildServiceProvider();
        }

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }
    }
}
