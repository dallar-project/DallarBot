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
        private readonly SettingsHandlerService settings;

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

            client = new ConnectionManager(settings.dallarSettings.rpc.ipaddress + ":" + settings.dallarSettings.rpc.port);
            client.credentials = new NetworkCredential(settings.dallarSettings.rpc.username, settings.dallarSettings.rpc.password);

            foreach(var guild in settings.dallarSettings.guilds)
            {
                string toWallet;
                client.GetWalletAddressFromUser(guild.tx.feeAccount, true, out toWallet);
            }

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
                var withdrawl = WithdrawlObjects.First(x => x.guildUser.Id == socketMessage.Author.Id);
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

                        success = client.GetWalletAddressFromUser(socketMessage.Author.Id.ToString(), true, out wallet);
                        if (success)
                        {
                            decimal txfee;
                            string feeAccount;
                            GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                            decimal balance = client.GetRawAccountBalance(Context.User.Id.ToString());
                            if (balance >= withdrawl.amount + txfee)
                            {
                                if (client.isAddressValid(message.Content))
                                {
                                    success = client.SendMinusFees(Context.User.Id.ToString(), message.Content, feeAccount, txfee, withdrawl.amount);
                                    if (success)
                                    {
                                        await Context.User.SendMessageAsync("You have successfully withdrawn " + withdrawl.amount + "DAL!");
                                    }
                                    else
                                    {
                                        await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                    }
                                    WithdrawlObjects.Remove(withdrawl);
                                }
                                else
                                {
                                    await Context.User.SendMessageAsync(Context.User.Mention + ", seems that address isn't quite right.");
                                }
                            }
                            else
                            {
                                await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                                WithdrawlObjects.Remove(withdrawl);
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

        public bool isUserDevTeam(SocketGuildUser user)
        {
            var guild = discord.GetGuild(user.Guild.Id);
            var devRole = guild.Roles.FirstOrDefault(x => x.Name == "Dallar Dev Team");
            return user.Roles.Contains(devRole);
        }

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }

        public string GetUserName(SocketGuildUser user)
        {
            return (user.Nickname == null || user.Nickname == "" ? user.Username : user.Nickname);
        }

        public void GetTXFeeAndAccount(SocketCommandContext context, out decimal txfee, out string feeAccount)
        {
            if (context.Guild != null)
            {
                var guild = settings.dallarSettings.guilds.First(x => x.guildID == context.Guild.Id);
                if (guild != null)
                {
                    txfee = guild.tx.txfee;
                    feeAccount = guild.tx.feeAccount;
                    return;
                }
            }

            txfee = 0.0002M;
            feeAccount = "txaccount";
        }

        float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
    }
}
