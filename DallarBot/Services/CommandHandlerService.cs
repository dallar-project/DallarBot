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
    public class CommandHandlerService
    {
        public DiscordSocketClient _discord;
        private CommandService _commands;
        private IServiceProvider _provider;

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public CommandHandlerService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += HandleCommandAsync;

            _discord.SetGameAsync("Dallar");
        }

        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;

            if (message == null)
            {
                return;
            }

            var context = new SocketCommandContext(_discord, message);                       
            int argPos = 0;

            if (message.HasCharPrefix('.', ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                if (result.Error == CommandError.UnknownCommand)
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}