using System.Linq;
using System.Threading.Tasks;
using System;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Services;
using DallarBot.Classes;
using DSharpPlus.Entities;

namespace DallarBot.Modules
{
    public class DallarCommander
    {
        [Command("balance")]
        [Aliases("bal")]
        public async Task GetAccountBalance(CommandContext Context)
        {
            await Program.DigitalPriceExchange.FetchValueInfo();

            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                decimal balance = Program.Daemon.GetRawAccountBalance(Context.User.Id.ToString());
                decimal pendingBalance = Program.Daemon.GetUnconfirmedAccountBalance(Context.User.Id.ToString());

                string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
                string resultStr = $"{Context.User.Mention}: {balance} DAL (${decimal.Round(balance * Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 4)} USD){pendingBalanceStr}";

                await LogHandlerService.LogUserActionAsync(Context, $"Checked balance. {balance} DAL with {pendingBalance} DAL pending.");
                await Context.RespondAsync(resultStr);
            }
            else
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to check balance. Getting wallet address failed.");
                await Context.RespondAsync($"{Context.User.Mention}: Failed to check balance. Getting wallet address failed. Please contact an Administrator.");
            }

            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("difficulty")]
        [Aliases("diff", "block")]
        public async Task GetDallarDifficulty(CommandContext Context)
        {
            int BlockCount = Program.Daemon.GetBlockCount();
            float Difficulty = Program.Daemon.GetDifficulty();

            await LogHandlerService.LogUserActionAsync(Context, $"Fetched block details. {BlockCount} with difficulty {Difficulty}.");
            await Context.RespondAsync($"{Context.User.Mention}: Difficulty for block {BlockCount}: {Difficulty}");
        }

        [Command("deposit")]
        public async Task GetDallarDeposit(CommandContext Context)
        {
            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                await DiscordHelpers.RespondAsDM(Context, "DallarBot is a Discord bot dedicated to allowing individual users on the server to easily tip each other in the chatbox. Please note that it is hosted on an external server, which means that theoretically DallarBot is cross-platform!" + Environment.NewLine +
                    Environment.NewLine + "Please note however that DallarBot does not access anyone's wallets directly to protect everyone's privacy: it introduces an additional step by creating an additional DallarBot - specific wallet for your unique user ID, and you must deposit / receive into it as a first secure layer before those dallars can access anyone's personal wallet." + Environment.NewLine +
                    "Your DallarBot wallet should not be used to permanently store your funds!" + Environment.NewLine +
                    Environment.NewLine + "Another important notice is that all Dallar transactions, whether it be through the DallarBot or through your personal wallet, incurs a flat combined fee of 0.0002 DAL(transaction fee + DallarBot fund fee.) The transaction fee is charged by the mining pool itself to validate your transactions, much akin to the default behavior of your Dallar wallet. The DallarBot fund fee is sampled from the leftover difference, to host the necessary external server to run DallarBot itself." + Environment.NewLine +
                    Environment.NewLine + "Every transaction needs to be verified by the blockchain for 6 blocks(5 - 10 minute period approximate.) If you check your balances right after a transaction, it will notify your transaction as " + '\u0022' + "pending" + '\u0022' + " Any pending funds will not be available until that period has been completed." + Environment.NewLine +
                    Environment.NewLine + "To deposit Dallar into your DallarBot account, please send funds from your wallet to the following address: " +
                    Environment.NewLine + "`" + Wallet + "`");

                // This is causing a rate limit and/or crash?
                //await Context.User.SendFileAsync(global.qr.GenerateQRBitmap("dallar:" + wallet), "Wallet.png");

                await LogHandlerService.LogUserActionAsync(Context, $"Fetched deposit info.");
            }
            else
            {
                await DiscordHelpers.RespondAsDM(Context, $"{Context.User.Mention}: Failed to fetch your wallet address. Please contact an Administrator.");
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to fetch deposit info.");
            }

            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("withdraw")]
        public async Task WithdrawFromWalletInstant(CommandContext Context, string AmountStr, string PublicAddress)
        {
            if (!Program.Daemon.IsAddressValid(PublicAddress))
            {
                // handle invalid public address
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw but PublicAddress ({PublicAddress}) is invalid.");
                await Context.RespondAsync($"{Context.User.Mention}: seems like you tried withdrawing Dallar to an invalid Dallar address. You supplied: {PublicAddress}");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            DallarHelpers.GetTXFeeAndAccount(out decimal txfee, out string feeAccount);
            decimal balance = Program.Daemon.GetRawAccountBalance(Context.User.Id.ToString());

            if (!DallarHelpers.TryParseUserAmountString(Context.User, AmountStr, out decimal Amount))
            {
                // handle amount parse fail
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but value could not be parsed.");
                await Context.RespondAsync($"{Context.User.Mention}: The amount you tried to withdraw can not be parsed as a number. You tried withdrawing {Amount} DAL.");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Make sure Amount is greater than zero.
            if (Amount <= 0)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but value is invalid.");
                await Context.RespondAsync($"{Context.User.Mention}: You can not withdraw 0 or less Dallar. You tried withdrawing {Amount} DAL.");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Verify user has requested balance to withdraw
            if (!DallarHelpers.CanUserAffordTransactionAmount(Context.User, Amount))
            {
                // user can not afford requested withdraw amount
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but has insufficient funds. ({balance})");
                await Context.RespondAsync($"{Context.User.Mention}: Looks like you don't have enough funds withdraw {Amount} DAL! Remember, there is a {txfee} DAL fee for performing bot transactions.");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Amount should be guaranteed a good value to withdraw
            // Fetch user's wallet
            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                if (Program.Daemon.SendMinusFees(Context.User.Id.ToString(), PublicAddress, feeAccount, txfee, Amount))
                {
                    // Successfully withdrew
                    await LogHandlerService.LogUserActionAsync(Context, $"Successfully withdrew {Amount} from wallet ({Wallet}).");
                    await Context.RespondAsync($"You have successfully withdrawn {Amount} DAL" + (Context.Member == null ? "." : $" to address {PublicAddress}."));
                }
                else
                {   // unable to send dallar
                    await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} from wallet ({Wallet}) but daemon failed to send transaction.");
                    await Context.RespondAsync("Something went wrong trying to send your Dallar through the Dallar daemon. (Please contact the Administrators!)");
                    DiscordHelpers.DeleteNonPrivateMessage(Context);
                    return;
                }
            }
            else
            {   // unable to fetch user's wallet
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but bot could not determine user's wallet.");
                await Context.RespondAsync("Something went wrong trying to get your DallarBot Dallar Address. (Please contact the Administrators!)");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // After withdraw success
            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        public async Task SendDallarToUser(CommandContext Context, DiscordMember Member, decimal Amount)
        {
            await SendDallarToUser(Context, Amount, Member);
        }

        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        public async Task SendDallarToUser(CommandContext Context, DiscordMember Member, string AmountStr)
        {
            if (!DallarHelpers.TryParseUserAmountString(Context.User, AmountStr, out decimal Amount))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but could not parse amount: {AmountStr}");
                await Context.RespondAsync($"{Context.User.Mention}: Could not parse amount.");
                return;
            }

            await SendDallarToUser(Context, Amount, Member);
        }

        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        public async Task SendDallarToUser(CommandContext Context, decimal Amount, DiscordMember Member)
        {
            if (Context.Member == null)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but user not in a guild.");
                await Context.RespondAsync($"{Context.User.Mention}: You must be in a Discord server with DallarBot to give to others.");
                return;
            }

            if (Context.User.Id == Member.Id)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar to themselves.");
                await Context.RespondAsync($"{Context.User.Mention}: You can not send Dallar to yourself.");
                return;
            }

            if (!Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string FromWallet))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but can not get sender's wallet.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot failed to get your wallet. Please contact an Administrator.");
                return;
            }

            if (!Program.Daemon.GetWalletAddressFromUser(Member.Id.ToString(), true, out string ToWallet))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but can not get receiver's wallet. Receiver: {Member.Id.ToString()} ({Member.Username.ToString()})");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot failed to get your wallet. Please contact an Administrator.");
                return;
            }

            if (Amount <= 0)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but sender requested something less than or equal to zero.");
                await Context.RespondAsync($"{Context.User.Mention}: You can not send negative or zero Dallars.");
                return;
            }

            await Program.DigitalPriceExchange.FetchValueInfo();

            if (!DallarHelpers.CanUserAffordTransactionAmount(Context.User, Amount))
            {

            }


                decimal txfee;
                string feeAccount;
                global.GetTXFeeAndAccount(Context, out txfee, out feeAccount);

                decimal balance = global.client.GetRawAccountBalance(Context.User.Id.ToString());
                if (balance >= amount + txfee)
                {
                    success = global.client.SendMinusFees(Context.User.Id.ToString(), toWallet, feeAccount, txfee, amount);
                    if (success)
                    {
                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + " DAL ($" + decimal.Round(amount * global.usdValue, 4) + "USD) to " + user.Mention + "!");
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


            await Context.Message.DeleteAsync();
        }
        

        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        public async Task SendDallarToUser(string amountString, IUser user)
        {
            if (!Context.IsPrivate)
            {
                if (amountString == "all")
                {
                    await global.FetchValueInfo();

                    string fromWallet = "";
                    string toWallet = "";
                    bool success = true;

                    success = global.client.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out fromWallet);
                    if (success)
                    {
                        success = global.client.GetWalletAddressFromUser(user.Id.ToString(), true, out toWallet);
                        if (success)
                        {
                            if (Context.User.Id != user.Id)
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
                                        await Context.Channel.SendMessageAsync("Success! " + Context.User.Mention + " has sent " + amount + " DAL ($" + decimal.Round(amount * global.usdValue, 4) + "USD) to " + user.Mention + "!");
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
        [Aliases("gift-random", "transfer-random", "give-random")]
        public async Task SendRandomUser()
        {
            await Context.User.SendMessageAsync("Please specify the amount of DAL to give.");
        }

        [Command("send-random")]
        [Aliases("gift-random", "transfer-random", "give-random")]
        public async Task SendRandomUser(decimal amount)
        {
            if (amount > 0)
            {
                int randomIndex = global.RandomManager.GetRandomInteger(0, Context.Guild.Users.Count - 1);
                var user = Context.Guild.Users.Where(x => x.IsBot == false).ToList()[randomIndex];
                if (user != null)
                {
                    await SendDallarToUser(amount, user);
                }
            }
            else
            {
				await Context.User.SendMessageAsync("Please specify a valid amount of DAL to give.");
            }
        }
    }
}
