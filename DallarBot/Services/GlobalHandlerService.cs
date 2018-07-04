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
using DallarBot.Exchanges;

namespace DallarBot.Services
{
    public class GlobalHandlerService
    {
        public DiscordSocketClient discord;
        public ConnectionManager client;
        public QRGenerator qr = new QRGenerator();
        private readonly SettingsHandlerService settings;

        public decimal usdValue;
        public DigitalPriceDallarInfo DallarInfo;
        protected DateTime lastFetchTime;

        public List<WithdrawManager> WithdrawlObjects = new List<WithdrawManager>();
        

        public GlobalHandlerService(DiscordSocketClient _discord, SettingsHandlerService _settings)
        {
            discord = _discord;
            settings = _settings;

            client = new ConnectionManager(settings.dallarSettings.rpc.ipaddress + ":" + settings.dallarSettings.rpc.port);
            client.credentials = new NetworkCredential(settings.dallarSettings.rpc.username, settings.dallarSettings.rpc.password);

            foreach(var guild in settings.dallarSettings.guilds)
            {
                string toWallet;
                client.GetWalletAddressFromUser(guild.tx.feeAccount, true, out toWallet);
            }

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

        public async Task FetchValueInfo()
        {
            if (DateTime.Compare(lastFetchTime, DateTime.Now.AddSeconds(-5.0d)) < 0)
            {
                Console.WriteLine("Fetching Dallar info from DigitalPrice.");

                var client = new WebClient();
                var jsonString = await client.DownloadStringTaskAsync("https://digitalprice.io/markets/get-currency-summary?currency=BALANCE_COIN_BITCOIN");
                var btcPrice = await client.DownloadStringTaskAsync("https://blockchain.info/tobtc?currency=USD&value=1");

                var DigitalPriceInfo = DigitalPriceDallarInfo.FromJson(jsonString);

                for (int i = 0; i < DigitalPriceInfo.Length; i++)
                {
                    if (DigitalPriceInfo[i].MiniCurrency == "dal-btc")
                    {
                        DallarInfo = DigitalPriceInfo[i];
                    }
                }

                usdValue = decimal.Round(DallarInfo.Price / Convert.ToDecimal(btcPrice), 8, MidpointRounding.AwayFromZero);
                lastFetchTime = DateTime.Now;
            }
        }
    }
}
