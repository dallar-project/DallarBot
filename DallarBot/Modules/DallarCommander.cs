using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System;

using DallarBot.Classes;
using DallarBot.Services;

namespace DallarBot.Modules
{
    public class DallarCommander : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalHandlerService global;
        private readonly SettingsHandlerService settings;

        public DallarCommander(GlobalHandlerService _global, SettingsHandlerService _settings)
        {
            global = _global;
            settings = _settings;
        }

        [Command("balance")]
        [Alias("bal")]
        public async Task GetAccountBalance()
        {
            string wallet = "";
            bool success = true;

            success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
            if (success)
            {
                decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                decimal pendingBalance = global.client.GetUnconfirmedAccountBalance(Context.User.Id.ToString());

                await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + balance + "DAL" +
                (pendingBalance != 0 ? " (" + pendingBalance + "DAL Pending)" : ""));
            }
            await Context.Message.DeleteAsync();
        }

        [Command("difficulty")]
        public async Task GetDallarDifficulty()
        {
            await Context.Channel.SendMessageAsync("Dallar difficulty is currently: " + global.client.GetDifficulty() + Environment.NewLine +
                "Block count: " + global.client.GetBlockCount() + Environment.NewLine +
                "Connections: " + global.client.GetConnectionCount());
        }

        [Command("withdraw")]
        public async Task WithdrawFromWallet(decimal amount)
        {
            string wallet = "";
            bool success = true;

            success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
            if (success)
            {
                if (amount > 0)
                {
                    amount = amount + settings.dallarSettings.rpc.txfee;
                    decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                    if (balance >= amount)
                    {
                        if (global.WithdrawlObjects.Any(x => x.guildUser.Id == Context.User.Id))
                        {
                            WithdrawManager withdrawlObject = global.WithdrawlObjects.First(x => x.guildUser.Id == Context.User.Id);
                            withdrawlObject.amount = amount;
                            await Context.User.SendMessageAsync("Please respond with your wallet addresss or `cancel` to cancel the withdraw.");
                        }
                        else
                        {
                            WithdrawManager withdrawlObject = new WithdrawManager(Context.User as SocketGuildUser, amount);
                            global.WithdrawlObjects.Add(withdrawlObject);
                            await Context.User.SendMessageAsync("Please respond with your wallet addresss or `cancel` to cancel the withdraw.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                }
            }
            await Context.Message.DeleteAsync();
        }

        [Command("withdraw")]
        public async Task WithdrawFromWalletInstant(decimal amount, string publicAddress)
        {
            string wallet = "";
            bool success = true;

            success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
            if (success)
            {
                if (amount > 0)
                {
                    amount = amount + settings.dallarSettings.rpc.txfee;
                    decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                    if (balance >= amount)
                    {
                        if (publicAddress.StartsWith("D") && publicAddress.Length < 46)
                        {
                            global.client.SendToAddress(Context.User.Id.ToString(), publicAddress, amount);
                            await Context.User.SendMessageAsync("You have successfully withdrawn " + amount + "DAL!");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have enough funds withdraw " + amount + "DAL!");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                }
            }
            await Context.Message.DeleteAsync();
        }

        [Command("withdraw")]
        public async Task WithdrawAllFromWallet(string amountString)
        {
            if (amountString == "all")
            {
                string wallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
                if (success)
                {
                    decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - settings.dallarSettings.rpc.txfee;
                    if (amount > 0)
                    {
                        if (global.WithdrawlObjects.Any(x => x.guildUser.Id == Context.User.Id))
                        {
                            WithdrawManager withdrawlObject = global.WithdrawlObjects.First(x => x.guildUser.Id == Context.User.Id);
                            withdrawlObject.amount = amount;
                            await Context.User.SendMessageAsync("Please respond with your wallet addresss or `cancel` to cancel the withdraw.");
                        }
                        else
                        {
                            WithdrawManager withdrawlObject = new WithdrawManager(Context.User as SocketGuildUser, amount);
                            global.WithdrawlObjects.Add(withdrawlObject);
                            await Context.User.SendMessageAsync("Please respond with your wallet addresss or `cancel` to cancel the withdraw.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                    }
                }
                await Context.Message.DeleteAsync();
            }
        }

        [Command("withdraw")]
        public async Task WithdrawAllFromWalletInstant(string amountString, string publicAddress)
        {
            if (amountString == "all")
            {
                string wallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
                if (success)
                {
                    decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - settings.dallarSettings.rpc.txfee;
                    if (amount > 0)
                    {
                        if (publicAddress.StartsWith("D") && publicAddress.Length < 46)
                        {
                            global.client.SendToAddress(Context.User.Id.ToString(), publicAddress, amount);
                            await Context.User.SendMessageAsync("You have successfully withdrawn " + amount + "DAL!");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                    }
                }
                await Context.Message.DeleteAsync();
            }
        }

        [Command("deposit")]
        public async Task GetDallarDeposit()
        {
            string wallet = "";
            bool success = true;

            success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out wallet);
            if (success)
            {
                await Context.User.SendMessageAsync("Hey! Deposit to this address to add funds to your DallarBot Wallet!" + Environment.NewLine + wallet);
            }
            await Context.Message.DeleteAsync();
        }

        [Command("send")]
        [Alias("give", "transfer")]
        public async Task SendDallarToUser(decimal amount, [Remainder]string remainingString)
        {
            if (Context.Message.MentionedUsers.Count > 1)
            {
                await Context.Channel.SendMessageAsync("Please only tag 1 user!");
                return;
            }
            SocketGuildUser guildUser = Context.Message.MentionedUsers.ToArray()[0] as SocketGuildUser;
            if (guildUser != null)
            {
                string fromWallet = "";
                string toWallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out fromWallet);
                if (success)
                {
                    success = global.client.GetWalletAddressFromUser(guildUser.Id, true, out toWallet);
                    if (success)
                    {
                        if (Context.User.Id != guildUser.Id)
                        {
                            if (amount > 0)
                            {
                                amount = amount + settings.dallarSettings.rpc.txfee;
                                decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                                if (balance >= amount)
                                {
                                    success = global.client.SendToAddress(Context.User.Id.ToString(), toWallet, amount);
                                    if (success)
                                    {
                                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + "DAL to " + guildUser.Mention + "!");
                                    }
                                    else
                                    {
                                        await Context.Channel.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                    }
                                }
                                else
                                {
                                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have enough funds to send " + amount + "DAL!");
                                }
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                            }
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync(Context.User.Mention + ", you cannot send DAL to yourself!");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                    }
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention + ", please tag the user you wish to send DAL to.");
            }
            await Context.Message.DeleteAsync();
        }

        [Command("send")]
        [Alias("give", "transfer")]
        public async Task SendAllToUser(string amountString, [Remainder]string remainingString)
        {
            if (amountString == "all")
            {
                if (Context.Message.MentionedUsers.Count > 1)
                {
                    await Context.Channel.SendMessageAsync("Please only tag 1 user!");
                    return;
                }
                SocketGuildUser guildUser = Context.Message.MentionedUsers.ToArray()[0] as SocketGuildUser;
                if (guildUser != null)
                {
                    string fromWallet = "";
                    string toWallet = "";
                    bool success = true;

                    success = global.client.GetWalletAddressFromUser(Context.User.Id, true, out fromWallet);
                    if (success)
                    {
                        success = global.client.GetWalletAddressFromUser(guildUser.Id, true, out toWallet);
                        if (success)
                        {
                            if (Context.User.Id != guildUser.Id)
                            {
                                decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - settings.dallarSettings.rpc.txfee;
                                if (amount > 0)
                                {
                                    success = global.client.SendToAddress(Context.User.Id.ToString(), toWallet, amount);
                                    if (success)
                                    {
                                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + "DAL to " + guildUser.Mention + "!");
                                    }
                                    else
                                    {
                                        await Context.Channel.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                    }
                                }
                                else
                                {
                                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                                }
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync(Context.User.Mention + ", you cannot send DAL to yourself!");
                            }
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                        }
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", please tag the user you wish to send DAL to.");
                }
                await Context.Message.DeleteAsync();
            }
        }
    }
}
