using System.Linq;
using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Services;
using DallarBot.Classes;
using DSharpPlus.Entities;
using System.Collections.Generic;

namespace DallarBot.Commands
{
    public class TipCommands
    {
        [Command("balance")]
        [Aliases("bal")]
        public async Task GetAccountBalance(CommandContext Context)
        {
            await Context.TriggerTypingAsync();
            await Program.DigitalPriceExchange.FetchValueInfo();

            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                decimal balance = Program.Daemon.GetRawAccountBalance(Context.User.Id.ToString());
                decimal pendingBalance = Program.Daemon.GetUnconfirmedAccountBalance(Context.User.Id.ToString());

                string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
                string resultStr = $"{Context.User.Mention}: Your balance is {balance} DAL (${decimal.Round(balance * Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 4)} USD){pendingBalanceStr}";

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

        [Command("deposit")]
        public async Task GetDallarDeposit(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

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
            await Context.TriggerTypingAsync();

            // Make sure supplied address is a valid Dallar address
            if (!Program.Daemon.IsAddressValid(PublicAddress))
            {
                // handle invalid public address
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw but PublicAddress ({PublicAddress}) is invalid.");
                await Context.RespondAsync($"{Context.User.Mention}: Seems like you tried withdrawing Dallar to an invalid Dallar address. You supplied: {PublicAddress}");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Try to interpret the user's amount input as a sane value
            if (!DallarHelpers.TryParseUserAmountString(Context.User, AmountStr, out decimal Amount))
            {
                // handle amount parse fail
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but value could not be parsed.");
                await Context.RespondAsync($"{Context.User.Mention}: The amount you tried to withdraw can not be parsed as a number. You tried withdrawing {Amount} DAL.");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Make sure Amount is greater than zero
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
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to withdraw {Amount} but has insufficient funds. ({Program.Daemon.GetRawAccountBalance(Context.User.Id.ToString())})");
                await Context.RespondAsync($"{Context.User.Mention}: Looks like you don't have enough funds withdraw {Amount} DAL! Remember, there is a {Program.SettingsHandler.Dallar.Txfee} DAL fee for performing bot transactions.");
                DiscordHelpers.DeleteNonPrivateMessage(Context);
                return;
            }

            // Amount should be guaranteed a good value to withdraw
            // Fetch user's wallet
            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                if (Program.Daemon.SendMinusFees(Context.User.Id.ToString(), PublicAddress, Amount))
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

        // The 'real' send function that all sends should filter into
        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        public async Task SendDallarToUser(CommandContext Context, string AmountStr, DiscordMember Member, bool IsRandomSend = false)
        {
            await Context.TriggerTypingAsync();

            string RandomUserString = IsRandomSend ? " to a random user" : "";
            // Error out if this is a private message
            if (Context.Member == null)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar{RandomUserString} but user not in a guild.");
                await Context.RespondAsync($"{Context.User.Mention}: You must be in a Discord server with DallarBot to give to others.");
                return;
            }

            if (!DallarHelpers.TryParseUserAmountString(Context.User, AmountStr, out decimal Amount))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar but could not parse amount: {AmountStr}");
                await Context.RespondAsync($"{Context.User.Mention}: Could not parse amount.");
                return;
            }

            // Error out if trying to send an invalid amount
            if (Amount <= 0)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar{RandomUserString} but sender requested something less than or equal to zero.");
                await Context.RespondAsync($"{Context.User.Mention}: You can not send negative or zero Dallars.");
                _ = Context.Message.DeleteAsync();
                return;
            }

            // Error out if user is trying to send to themselves
            if (Context.User.Id == Member.Id)
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar to themselves.");
                await Context.RespondAsync($"{Context.User.Mention}: You can not send Dallar to yourself.");
                _ = Context.Message.DeleteAsync();
                return;
            }

            // Failed to get senders wallet?
            if (!Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string FromWallet))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar{RandomUserString} but can not get sender's wallet.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot failed to get your wallet. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
                return;
            }

            // Failed to get receiver's wallet?
            if (!Program.Daemon.GetWalletAddressFromUser(Member.Id.ToString(), true, out string ToWallet))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send Dallar{RandomUserString} but can not get receiver's wallet. Receiver: {Member.Id.ToString()} ({Member.Username.ToString()})");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot failed to get your wallet. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
                return;
            }

            // Can user afford transaction?
            if (!DallarHelpers.CanUserAffordTransactionAmount(Context.User, Amount))
            {
                await LogHandlerService.LogUserActionAsync(Context, $"Tried to send {Amount} DAL{RandomUserString} but could not afford it.");
                await Context.RespondAsync($"{Context.User.Mention}: You do not have {Amount} DAL to send.");
                _ = Context.Message.DeleteAsync();
                return;
            }

            // Were we able to successfully send the transaction?
            if (Program.Daemon.SendMinusFees(Context.User.Id.ToString(), ToWallet, Amount))
            {
                await Program.DigitalPriceExchange.FetchValueInfo();

                if (IsRandomSend)
                {
                    await LogHandlerService.LogUserActionAsync(Context, $"Sent {Amount} DAL randomly to User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToWallet}.");
                    await Context.RespondAsync($"{Context.User.Mention}: You have successfully randomly sent {Member.Mention} {Amount} DAL. ($ {decimal.Round(Amount * Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 4)} USD)");
                }
                else
                {
                    await LogHandlerService.LogUserActionAsync(Context, $"Sent {Amount} DAL User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToWallet}.");
                    await Context.RespondAsync($"{Context.User.Mention}: You have successfully sent {Member.Mention} {Amount} DAL. ($ {decimal.Round(Amount * Program.DigitalPriceExchange.DallarInfo.USDValue.GetValueOrDefault(), 4)} USD)");
                }
                
                _ = Context.Message.DeleteAsync();
                return;
            }
            else
            {   // sending failed?
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to have daemon send{RandomUserString} {Amount} DAL to User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToWallet}.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot has failed to send{RandomUserString} {Amount} DAL. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
                return;
            }
        }

        [Command("send-random-here")]
        [Aliases("gift-random-here", "transfer-random-here", "give-random-here")]
        public async Task SendRandomUserHereOnly(CommandContext Context, decimal Amount)
        {
            await SendRandomUserInternal(Context, Amount, UserStatus.Idle);
        }

        [Command("send-random-online")]
        [Aliases("gift-random-online", "transfer-random-online", "give-random-online")]
        public async Task SendRandomUserOnlineOnly(CommandContext Context, decimal Amount)
        {
            await SendRandomUserInternal(Context, Amount, UserStatus.Online);
        }

        [Command("send-random")]
        [Aliases("gift-random", "transfer-random", "give-random")]
        public async Task SendRandomUser(CommandContext Context, decimal Amount)
        {
            await SendRandomUserInternal(Context, Amount);
        }

        public async Task SendRandomUserInternal(CommandContext Context, decimal Amount, UserStatus MinimumStatus = UserStatus.Offline)
        {
            await Context.TriggerTypingAsync();

            await LogHandlerService.LogUserActionAsync(Context, $"Invoked sending {Amount} to a random user with minimum status {MinimumStatus.ToString()}.");

            IEnumerable<DiscordMember> Members = DiscordHelpers.GetHumansInContextGuild(Context, true, MinimumStatus);
            int randomIndex = Program.RandomManager.GetRandomInteger(0, Members.Count() - 1);

            DiscordMember Member = Members.ElementAt(randomIndex);
            if (Member != null)
            {
                await SendDallarToUser(Context, Amount.ToString(), Member, true);
            }
            else
            {   // failed to get random member?
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to get a random user from the guild.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot has failed to get a random user from the guild. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
            }
        }
    }
}
