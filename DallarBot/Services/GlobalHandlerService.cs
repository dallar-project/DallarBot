using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

using DallarBot.Classes;

namespace DallarBot.Services
{
    public class GlobalHandlerService
    {
        public DiscordSocketClient _discord;
        private CommandService _commands;
        private IServiceProvider _provider;

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public GlobalHandlerService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.Connected += Connected;
            _discord.Disconnected += Disconnected;
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
            var guild = _discord.GetGuild(user.Guild.Id);
            var adminRole = guild.Roles.FirstOrDefault(x => x.Name == "Admin");
            return user.Roles.Contains(adminRole);
        }

        public bool isUserModerator(SocketGuildUser user)
        {
            var guild = _discord.GetGuild(user.Guild.Id);
            var moderatorRole = guild.Roles.FirstOrDefault(x => x.Name == "Moderator");
            return user.Roles.Contains(moderatorRole);
        }

        public async Task Say(ulong guildID, ulong channelID, string say)
        {
            var guild = _discord.GetGuild(guildID);
            var channel = guild.GetTextChannel(channelID);
            var message = await channel.SendMessageAsync(say);
            new MessageManager(message as IUserMessage, 90);
        }

        public async Task Say(ulong guildID, ulong channelID, string say, int time)
        {
            var guild = _discord.GetGuild(guildID);
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
