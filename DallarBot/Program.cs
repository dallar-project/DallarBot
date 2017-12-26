using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using DallarBot.Services;

namespace DallarBot
{ 
    class Program : ModuleBase<SocketCommandContext>
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
    
        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            Console.WriteLine(CenterString("▒█▀▀▄ █▀▀█ █░░ █░░ █▀▀█ █▀▀█ ▒█▀▀█ █▀▀█ ▀▀█▀▀ "));
            Console.WriteLine(CenterString("▒█░▒█ █▄▄█ █░░ █░░ █▄▄█ █▄▄▀ ▒█▀▀▄ █░░█ ░░█░░ "));
            Console.WriteLine(CenterString("▒█▄▄▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀░░▀ ▀░▀▀ ▒█▄▄█ ▀▀▀▀ ░░▀░░ "));

            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");

            Console.WriteLine(CenterString("Initialising bot..."));
        
            _client = new DiscordSocketClient();

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandlerService>().InitializeAsync(services);
            await services.GetRequiredService<GlobalHandlerService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, "Mzk1MTUzMTE1NzUxNjQ1MTk0.DSOuiw.M74BFkuqdgBQwnQOyKx-uv7-EIA");
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<GlobalHandlerService>()
                // Add additional services here...
                .BuildServiceProvider();
        }

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }
    }
}
