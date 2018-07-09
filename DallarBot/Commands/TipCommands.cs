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
    [RequireBotPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class TipCommands : BaseCommandModule
    {
        [Command("balance")]
        [Aliases("bal")]
        [Description("Displays your current Dallar balance")]
        [HelpCategory("Tipping")]
        public async Task GetAccountBalance(CommandContext Context)
        {
            await Context.TriggerTypingAsync();

            bool bDisplayUSD = false;
            if (Program.DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                bDisplayUSD = true;
            }

            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                decimal balance = Program.Daemon.GetRawAccountBalance(Context.User.Id.ToString());
                decimal pendingBalance = Program.Daemon.GetUnconfirmedAccountBalance(Context.User.Id.ToString());

                string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
                string resultStr = $"{Context.User.Mention}: Your balance is {balance} DAL";
                if (bDisplayUSD)
                {
                    resultStr += $" (${decimal.Round(balance * PriceInfo.USDValue.GetValueOrDefault(), 4)} USD){pendingBalanceStr}";
                }

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
        [HelpCategory("Tipping")]
        [Description("Sends information on how to deposit Dallar")]
        public async Task GetDallarDeposit(CommandContext Context)
        {
            if (Program.Daemon.GetWalletAddressFromUser(Context.User.Id.ToString(), true, out string Wallet))
            {
                DiscordEmbedBuilder EmbedBuilder = new DiscordEmbedBuilder();

                EmbedBuilder.WithTitle("Dallar Bot Depositing Help");
                EmbedBuilder.WithDescription("DallarBot is a Discord bot dedicated to allowing individual users on the server to easily tip each other in the chatbox. It generates a wallet for every Discord user, and you can withdraw into any address any time." + Environment.NewLine + Environment.NewLine + 
                    "Dallar Bot does not access anyone's wallets directly in order to protect everyone's privacy.");

                EmbedBuilder.AddField("Warning About Storage", "Dallar Bot should not be used as a long term storage for your Dallar. Dallar Bot is only accessible through Discord and if the Bot or Discord are down for any reason, you will *not* be able to access your stored Dallar.");
                EmbedBuilder.AddField("Dallar Bot Fees", $"All transactions with Dallar Bot incur a flat {Program.SettingsHandler.Dallar.Txfee} DAL fee to cover the Dallar blockchain transaction fees as well as funding and maintenance costs sent to the Dallar Bot server hoster.");
                EmbedBuilder.AddField("Blockchain Transactions", $"Dallar Bot uses the blockchain to keep track of its transactions, meaning your transactions will require 6 confirmation blocks before they are completed. This should take approximately 5 to 10 minutes under normal Dallar network conditions.");
                EmbedBuilder.AddField("Depositing", $"You can deposit Dallar into your Dallar Bot balance by sending Dallar to this address generated specifically for you: `{Wallet}`");

                EmbedBuilder.WithImageUrl($"https://api.qrserver.com/v1/create-qr-code/?data=dallar:{Wallet}&qzone=2");

                await LogHandlerService.LogUserActionAsync(Context, $"Fetched deposit info.");
                await DiscordHelpers.RespondAsDM(Context, EmbedBuilder.Build());
            }
            else
            {
                await DiscordHelpers.RespondAsDM(Context, $"{Context.User.Mention}: Failed to fetch your wallet address. Please contact an Administrator.");
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to fetch deposit info.");
            }

            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("withdraw")]
        [HelpCategory("Tipping")]
        [Description("Withdraws Dallar into given Dallar wallet address")]
        public async Task WithdrawFromWalletInstant(CommandContext Context, [Description("Amount of DAL to withdraw. Use 'all' for your entire balance")] string AmountStr, [Description("Dallar Wallet Address to withdraw Dallar to")] string PublicAddress)
        {
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

        [Command("send")]
        [Aliases("gift", "transfer", "give")]
        [HelpCategory("Tipping")]
        [Description("Sends Dallar to another Discord member")]
        public async Task SendDallarToUser(CommandContext Context, [Description("Amount of Dallar to send")] string AmountStr, [Description("Mention a discord member to send to i.e. `@Allar`")] DiscordMember Member)
        {
            await SendDallarToUserInternal(Context, AmountStr, Member, false);
        }

        [Command("send-random-here")]
        [Aliases("gift-random-here", "transfer-random-here", "give-random-here")]
        [HelpCategory("Tipping")]
        [Description("Sends Dallar to a random non-offline user in the server")]
        public async Task SendRandomUserHereOnly(CommandContext Context, [Description("Amount of Dallar to send")] decimal Amount)
        {
            await SendRandomUserInternal(Context, Amount, UserStatus.Idle);
        }

        [Command("send-random-online")]
        [Aliases("gift-random-online", "transfer-random-online", "give-random-online")]
        [HelpCategory("Tipping")]
        [Description("Sends Dallar to a random user who must be online in the server (no idle, no do not disturb)")]
        public async Task SendRandomUserOnlineOnly(CommandContext Context, [Description("Amount of Dallar to send")] decimal Amount)
        {
            await SendRandomUserInternal(Context, Amount, UserStatus.Online);
        }

        [Command("send-random")]
        [Aliases("gift-random", "transfer-random", "give-random")]
        [HelpCategory("Tipping")]
        [Description("Sends Dallar to a random user, including offline users")]
        public async Task SendRandomUser(CommandContext Context, [Description("Amount of Dallar to send")] decimal Amount)
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
                await SendDallarToUserInternal(Context, Amount.ToString(), Member, true);
            }
            else
            {   // failed to get random member?
                await LogHandlerService.LogUserActionAsync(Context, $"Failed to get a random user from the guild.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot has failed to get a random user from the guild. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
            }
        }

        // The 'real' send function that all sends should filter into
        public async Task SendDallarToUserInternal(CommandContext Context, [Description("Amount of Dallar to send")] string AmountStr, DiscordMember Member, bool IsRandomSend = false)
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
                bool bDisplayUSD = false;
                if (Program.DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
                {
                    bDisplayUSD = true;
                }

                await LogHandlerService.LogUserActionAsync(Context, $"Sent {Amount} DAL ${(IsRandomSend ? "randomly " : "")}to User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToWallet}.");
                string ReplyStr = $"{Context.User.Mention}: You have successfully ${(IsRandomSend ? "randomly " : "")}sent {Member.Mention} {Amount} DAL.";

                if (bDisplayUSD)
                {
                    ReplyStr += $" {decimal.Round(Amount * PriceInfo.USDValue.GetValueOrDefault(), 4)} USD)";
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
    }
}
