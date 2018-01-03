using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

using DallarBot.Classes;

namespace DallarBot.Services
{
    public class GlobalHandlerService
    {
        public DiscordSocketClient discord;
        private CommandService commands;
        private IServiceProvider provider;
        public DallarConnectionManager client;

        public async Task InitializeAsync(IServiceProvider _provider)
        {
            provider = _provider;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public GlobalHandlerService(IServiceProvider _provider, DiscordSocketClient _discord, CommandService _commands)
        {
            discord = _discord;
            commands = _commands;
            provider = _provider;

            client = new DallarConnectionManager("http://127.0.0.1:20133");
            client.credentials = new NetworkCredential("kanga", "testpass");

            //var p = _client.GetDifficulty();
            //Console.WriteLine(p);

            discord.Connected += Connected;
            discord.Disconnected += Disconnected;
        }

        private Task Disconnected(Exception ex)
        {
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            Console.WriteLine(CenterString("Something went wrong..."));
            Console.WriteLine("");
            Console.WriteLine(CenterString(ex.Message));
            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            return Task.Delay(0);
        }

        private Task Connected()
        {            
            Console.WriteLine(CenterString("Bot is online!"));
            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            return Task.Delay(0);
        }

        public bool isUserAdmin(SocketGuildUser user)
        {
            var guild = discord.GetGuild(user.Guild.Id);
            var adminRole = guild.Roles.FirstOrDefault(x => x.Name == "Admin");
            return user.Roles.Contains(adminRole);
        }

        public bool isUserModerator(SocketGuildUser user)
        {
            var guild = discord.GetGuild(user.Guild.Id);
            var moderatorRole = guild.Roles.FirstOrDefault(x => x.Name == "Moderator");
            return user.Roles.Contains(moderatorRole);
        }

        public async Task Say(ulong guildID, ulong channelID, string say)
        {
            var guild = discord.GetGuild(guildID);
            var channel = guild.GetTextChannel(channelID);
            var message = await channel.SendMessageAsync(say);
            new MessageManager(message as IUserMessage, 90);
        }

        public async Task Say(ulong guildID, ulong channelID, string say, int time)
        {
            var guild = discord.GetGuild(guildID);
            var channel = guild.GetTextChannel(channelID);
            var message = await channel.SendMessageAsync(say);
            new MessageManager(message as IUserMessage, time);
        }

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }
    }
}
