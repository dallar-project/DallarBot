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

            success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
            if (success)
            {
                decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                decimal pendingBalance = global.client.GetUnconfirmedAccountBalance(Context.User.Id.ToString());

                await Context.Channel.SendMessageAsync(Context.User.Mention + ": " + balance + "DAL" +
                (pendingBalance != 0 ? " (" + pendingBalance + "DAL Pending)" : ""));
            }
            else
            {
                await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
            }
            await Context.Message.DeleteAsync();
        }

        [Command("difficulty")]
        [Alias("diff", "block")]
        public async Task GetDallarDifficulty()
        {
            await Context.Channel.SendMessageAsync("Difficulty for block: " + global.client.GetBlockCount() + " is " + global.client.GetDifficulty());
        }

        [Command("deposit")]
        public async Task GetDallarDeposit()
        {
            if (!Context.IsPrivate || Context.IsPrivate)
            {
                string wallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
                if (success)
                {
                    await Context.User.SendMessageAsync("DallarBot is a Discord bot dedicated to allowing individual users on the server to easily tip each other in the chatbox. Please note that it is hosted on an external server, which means that theoretically DallarBot is cross-platform!" + Environment.NewLine +
                        Environment.NewLine + "Please note however that DallarBot does not access anyone's wallets directly to protect everyone's privacy: it introduces an additional step by creating an additional DallarBot - specific wallet for your unique user ID, and you must deposit / receive into it as a first secure layer before those dallars can access anyone's personal wallet." + Environment.NewLine +
                        "Your DallarBot wallet should not be used to permanently store your funds!" + Environment.NewLine +
                        Environment.NewLine + "Another important notice is that all Dallar transactions, whether it be through the DallarBot or through your personal wallet, incurs a flat combined fee of 0.0002 DAL(transaction fee + DallarBot fund fee.) The transaction fee is charged by the mining pool itself to validate your transactions, much akin to the default behavior of your Dallar wallet. The DallarBot fund fee is sampled from the leftover difference, to host the necessary external server to run DallarBot itself." + Environment.NewLine +
                        Environment.NewLine + "Every transaction needs to be verified by the blockchain for 6 blocks(5 - 10 minute period approximate.) If you check your balances right after a transaction, it will notify your transaction as " + '\u0022' + "pending" + '\u0022' + " Any pending funds will not be available until that period has been completed." + Environment.NewLine +
                        Environment.NewLine + "To deposit Dallar into your DallarBot account, please send funds from your wallet to the following address: " + Environment.NewLine);
                    await Context.User.SendMessageAsync(Environment.NewLine + "`" + wallet + "`" + Environment.NewLine);
                    await Context.User.SendMessageAsync(Environment.NewLine + "```!balance\n!give\n!withdraw```" + Environment.NewLine +
                        Environment.NewLine + "PS: On our current roadmap, we are investigating being able to send dallars directly as message reactions, so that we can tip people a small fixed amount for a specific highly appreciated post.");
                }
                else
                {
                    await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                }
                await Context.Message.DeleteAsync();
            }
        }

        [Command("withdraw")]
        public async Task WithdrawFromWallet(decimal amount)
        {
            if (!Context.IsPrivate)
            {
                string wallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
                if (success)
                {
                    if (amount > 0)
                    {
                        decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());

                        decimal txfee;
                        string feeAccount;
                        global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                        if (balance >= amount + txfee)
                        {
                            if (global.WithdrawlObjects.Any(x => x.guildUser.Id == Context.User.Id))
                            {
                                WithdrawManager withdrawlObject = global.WithdrawlObjects.First(x => x.guildUser.Id == Context.User.Id);
                                withdrawlObject.amount = amount;
                                await Context.User.SendMessageAsync("Please respond with your wallet addresss to withdraw " + amount + "DAL or `cancel` to cancel the withdraw.");
                            }
                            else
                            {
                                WithdrawManager withdrawlObject = new WithdrawManager(Context.User as SocketGuildUser, amount);
                                global.WithdrawlObjects.Add(withdrawlObject);
                                await Context.User.SendMessageAsync("Please respond with your wallet addresss to withdraw " + amount + "DAL or `cancel` to cancel the withdraw.");
                            }
                        }
                        else
                        {
                            await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                        }
                    }
                    else
                    {
                        await Context.User.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                    }
                }
                else
                {
                    await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, for now you have to use withdraw commands in a server. We're working on this.");
            }
        }

        [Command("withdraw")]
        public async Task WithdrawFromWalletInstant(decimal amount, string publicAddress)
        {
            if (!Context.IsPrivate)
            {
                string wallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
                if (success)
                {
                    if (amount > 0)
                    {
                        decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());

                        decimal txfee;
                        string feeAccount;
                        global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                        if (balance >= amount + txfee)
                        {
                            if (global.client.isAddressValid(publicAddress))
                            {
                                success = global.client.SendMinusFees(Context.User.Id.ToString(), publicAddress, feeAccount, txfee, amount);
                                if (success)
                                {
                                    await Context.User.SendMessageAsync("You have successfully withdrawn " + amount + "DAL!");
                                }
                                else
                                {
                                    await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                }
                            }
                            else
                            {
                                await Context.User.SendMessageAsync(Context.User.Mention + ", seems that address isn't quite right.");
                            }
                        }
                        else
                        {
                            await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have enough funds withdraw " + amount + "DAL!");
                        }
                    }
                    else
                    {
                        await Context.User.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                    }
                }
                else
                {
                    await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, for now you have to use withdraw commands in a server. We're working on this.");
            }
        }

        [Command("withdraw")]
        public async Task WithdrawAllFromWallet(string amountString)
        {
            if (!Context.IsPrivate)
            {
                if (amountString == "all")
                {
                    string wallet = "";
                    bool success = true;

                    success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
                    if (success)
                    {
                        decimal txfee;
                        string feeAccount;
                        global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                        decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - txfee;
                        await WithdrawFromWallet(amount);
                    }
                }
                else
                {
                    await Context.User.SendMessageAsync(Context.User.Mention + ", you forgot to specify the amount." + Environment.NewLine + "!withdraw <amount> <address>");
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, for now you have to use withdraw commands in a server. We're working on this.");
            }
        }

        [Command("withdraw")]
        public async Task WithdrawAllFromWalletInstant(string amountString, string publicAddress)
        {
            if (!Context.IsPrivate)
            {
                if (amountString == "all")
                {
                    string wallet = "";
                    bool success = true;

                    success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out wallet);
                    if (success)
                    {
                        decimal txfee;
                        string feeAccount;
                        global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                        decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - txfee;
                        if (amount > 0)
                        {
                            if (global.client.isAddressValid(publicAddress))
                            {
                                success = global.client.SendMinusFees(Context.User.Id.ToString(), publicAddress, feeAccount, txfee, amount);
                                if (success)
                                {
                                    await Context.User.SendMessageAsync("You have successfully withdrawn " + amount + "DAL!");
                                }
                                else
                                {
                                    await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                }
                            }
                            else
                            {
                                await Context.User.SendMessageAsync(Context.User.Mention + ", seems that address isn't quite right.");
                            }
                        }
                        else
                        {
                            await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have enough funds withdraw " + amount + "DAL!");
                        }
                    }
                    else
                    {
                        await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                    }
                }
                else
                {
                    await Context.User.SendMessageAsync(Context.User.Mention + ", please use the correct syntax." + Environment.NewLine + "!withdraw <amount> <address>");
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, for now you have to use withdraw commands in a server. We're working on this.");
            }
        }

        [Command("send")]
        [Alias("gift", "transfer", "give")]
        public async Task SendDallarToUser(SocketUser guildUser, decimal amount)
        {
            await SendDallarToUser(amount, guildUser);
        }

        [Command("send")]
        [Alias("gift", "transfer", "give")]
        public async Task SendDallarToUser(SocketUser guildUser, string amountString)
        {
            await SendDallarToUser(amountString, guildUser);
        }

        [Command("send")]
        [Alias("gift", "transfer", "give")]
        public async Task SendDallarToUser(decimal amount, SocketUser guildUser)
        {
            if (!Context.IsPrivate)
            {
                string fromWallet = "";
                string toWallet = "";
                bool success = true;

                success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out fromWallet);
                if (success)
                {
                    success = global.client.GetWalletAddressFromUser(guildUser.Id.ToString(), true, out toWallet);
                    if (success)
                    {
                        if (Context.User.Id != guildUser.Id)
                        {
                            if (amount > 0)
                            {
                                decimal txfee;
                                string feeAccount;
                                global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                                decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                                if (balance >= amount + txfee)
                                {
                                    success = global.client.SendMinusFees(Context.User.Id.ToString(), toWallet, feeAccount, txfee, amount);
                                    if (success)
                                    {
                                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + "DAL to " + guildUser.Mention + "!");
                                    }
                                    else
                                    {
                                        await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                    }
                                }
                                else
                                {
                                    await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have enough funds to send " + amount + "DAL!");
                                }
                            }
                            else
                            {
                                await Context.User.SendMessageAsync(Context.User.Mention + ", please enter a value above 0!");
                            }
                        }
                        else
                        {
                            await Context.User.SendMessageAsync(Context.User.Mention + ", you cannot send DAL to yourself!");
                        }
                    }
                    else
                    {
                        await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                    }
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, you cannot do this command here.");
            }
        }

        [Command("send")]
        [Alias("gift", "transfer", "give")]
        public async Task SendDallarToUser(string amountString, SocketUser guildUser)
        {
            if (!Context.IsPrivate)
            {
                if (amountString == "all")
                {
                    string fromWallet = "";
                    string toWallet = "";
                    bool success = true;

                    success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out fromWallet);
                    if (success)
                    {
                        success = global.client.GetWalletAddressFromUser(guildUser.Id.ToString(), true, out toWallet);
                        if (success)
                        {
                            if (Context.User.Id != guildUser.Id)
                            {
                                decimal txfee;
                                string feeAccount;
                                global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                                decimal amount = global.client.GetRawAccountBalance(Context.User.Id.ToString()) - txfee;
                                if (amount > 0)
                                {
                                    success = global.client.SendMinusFees(Context.User.Id.ToString(), toWallet, feeAccount, txfee, amount);
                                    if (success)
                                    {
                                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + "DAL to " + guildUser.Mention + "!");
                                    }
                                    else
                                    {
                                        await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                                    }
                                }
                                else
                                {
                                    await Context.User.SendMessageAsync(Context.User.Mention + ", looks like you don't have the funds to do this!");
                                }
                            }
                            else
                            {
                                await Context.User.SendMessageAsync(Context.User.Mention + ", you cannot send DAL to yourself!");
                            }
                        }
                        else
                        {
                            await Context.User.SendMessageAsync("Something went wrong! (Please contact an Administrator)");
                        }
                    }
                }
                else
                {
                    await Context.User.SendMessageAsync(Context.User.Mention + ", please use the correct syntax." + Environment.NewLine + "!send <amount> <tag user>");
                }
                await Context.Message.DeleteAsync();
            }
            else
            {
                await Context.User.SendMessageAsync("Sorry, you cannot do this command here.");
            }
        }

        [Command("send-random")]
        [Alias("gift-random", "transfer-random", "give-random")]
        public async Task SendRandomUser(decimal amount)
        {
            int randomIndex = new RandomManager(0, Context.Guild.Users.Count - 1).result;
            SocketGuildUser user = Context.Guild.Users.ToArray()[randomIndex];
            if (user != null)
            {
                await SendDallarToUser(amount, user);
            }
        }
    }
}
