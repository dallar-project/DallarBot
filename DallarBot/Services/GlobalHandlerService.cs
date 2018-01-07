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
using DallarBot.Services;

namespace DallarBot.Services
{
    public class GlobalHandlerService
    {
        public DiscordSocketClient discord;
        private CommandService commands;
        private IServiceProvider provider;
        public ConnectionManager client;
        public SQLConnectionManager sql;
        private readonly SettingsHandlerService settings;

        public List<ConfirmQueryObject> queriesQueue = new List<ConfirmQueryObject>();
        public List<WithdrawManager> WithdrawlObjects = new List<WithdrawManager>();

        public async Task InitializeAsync(IServiceProvider _provider)
        {
            provider = _provider;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public GlobalHandlerService(IServiceProvider _provider, DiscordSocketClient _discord, CommandService _commands, SettingsHandlerService _settings)
        {
            discord = _discord;
            commands = _commands;
            provider = _provider;
            settings = _settings;

            client = new ConnectionManager(settings.settings.rpc.ipaddress + ":" + settings.settings.rpc.port);
            client.credentials = new NetworkCredential(settings.settings.rpc.username, settings.settings.rpc.password);

            discord.Connected += Connected;
            discord.Disconnected += Disconnected;
            discord.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            var Context = new SocketCommandContext(discord, message);
            if (Context.IsPrivate)
            {
                var withdrawl = WithdrawlObjects.First(x => x.guildUser.Id == message.Author.Id);
                if (withdrawl != null)
                {
                    if (message.Content == "cancel")
                    {
                        WithdrawlObjects.Remove(withdrawl);
                        await Context.User.SendMessageAsync("Withdrawl has been cancelled.");
                    }
                    else
                    {
                        string wallet = "";
                        bool success = true;

                        success = client.GetWalletAddressFromUser(socketMessage.Author.Id, true, out wallet);
                        if (success)
                        {
                            decimal balance = client.GetRawAccountBalance(Context.User.Id.ToString());
                            if (balance >= withdrawl.amount)
                            {
                                if (message.Content.StartsWith("D") && message.Content.Length < 46)
                                {
                                    client.SendToAddress(Context.User.Id.ToString(), message.Content, withdrawl.amount);
                                    await socketMessage.Author.SendMessageAsync("You have successfully withdrawn " + withdrawl.amount + "DAL!");
                                    WithdrawlObjects.Remove(withdrawl);
                                }
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync("Hmmm, looks like you don't have the funds to do this!");
                            }
                        }
                    }
                }
                else
                {
                    await socketMessage.Author.SendMessageAsync("I don't understand that command.");
                }
            }
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

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }

        public string GetUserName(SocketGuildUser user)
        {
            return (user.Nickname == null || user.Nickname == "" ? user.Username : user.Nickname);
        }
    }
}
