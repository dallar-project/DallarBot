using System.Linq;
using System.Threading.Tasks;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DallarBot.Services;
using DallarBot.Classes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using Dallar;
using Dallar.Exchange;
using Dallar.Services;

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

            IDallarPriceProviderService PriceService = Context.Services.GetService(typeof(IDallarPriceProviderService)) as IDallarPriceProviderService;
            IDallarClientService DallarClientService = Context.Services.GetService(typeof(IDallarClientService)) as IDallarClientService;

            bool bDisplayUSD = false;
            if (PriceService.GetPriceInfo(out DallarPriceInfo PriceInfo, out bool bPriceStale))
            {
                bDisplayUSD = true;
            }

            DallarAccount Account = DiscordHelpers.DallarAccountFromDiscordUser(Context.User);

            decimal balance = DallarClientService.GetAccountBalance(Account);
            decimal pendingBalance = DallarClientService.GetAccountPendingBalance(Account);

            string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
            string resultStr = $"{Context.User.Mention}: Your balance is {balance} DAL";
            if (bDisplayUSD)
            {
                resultStr += $" (${decimal.Round(balance * PriceInfo.PriceInUSD, 4)} USD){pendingBalanceStr}";
            }
            else
            {
                resultStr += pendingBalanceStr;
            }

            LogHandlerExtensions.LogUserAction(Context, $"Checked balance. {balance} DAL with {pendingBalance} DAL pending.");
            await Context.RespondAsync(resultStr);


            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("deposit")]
        [HelpCategory("Tipping")]
        [Description("Sends information on how to deposit Dallar")]
        public async Task GetDallarDeposit(CommandContext Context)
        {
            IDallarClientService DallarClientService = Context.Services.GetService(typeof(IDallarClientService)) as IDallarClientService;

            DallarAccount Account = DiscordHelpers.DallarAccountFromDiscordUser(Context.User);
            if (DallarClientService.ResolveDallarAccountAddress(ref Account))
            {
                DiscordEmbedBuilder EmbedBuilder = new DiscordEmbedBuilder();

                EmbedBuilder.WithTitle("Dallar Bot Depositing Help");
                EmbedBuilder.WithDescription("Dallar Bot is a bot dedicated to allowing users to send and receive Dallar from another for both Discord servers and Twitch channels. It generates a wallet for every Discord user, and you can withdraw into any address any time." + Environment.NewLine + Environment.NewLine + 
                    "Dallar Bot does not access anyone's wallets directly in order to protect everyone's privacy.");

                EmbedBuilder.AddField("Warning About Storage", "Dallar Bot should not be used as a long term storage for your Dallar. Dallar Bot is only accessible through Discord, Twitch, and the Dallar Bot Website. If the Bot and/or Discord are down for any reason, there is a good chance you will *not* be able to access your stored Dallar.");
                EmbedBuilder.AddField("Blockchain Transactions", $"Dallar Bot uses the blockchain to keep track of its transactions, meaning your transactions will require 6 confirmation blocks before they are completed. This should take approximately 5 to 10 minutes under normal Dallar network conditions.");
                EmbedBuilder.AddField("Depositing", $"You can deposit Dallar into your Dallar Bot balance by sending Dallar to this address generated specifically for you: `{Account.KnownAddress}`");

                EmbedBuilder.WithImageUrl($"https://api.qrserver.com/v1/create-qr-code/?data=dallar:{Account.KnownAddress}&qzone=2");

                LogHandlerExtensions.LogUserAction(Context, $"Fetched deposit info.");
                await DiscordHelpers.RespondAsDM(Context, EmbedBuilder.Build());
            }
            else
            {
                await DiscordHelpers.RespondAsDM(Context, $"{Context.User.Mention}: Failed to fetch your wallet address. Please contact an Administrator.");
                LogHandlerExtensions.LogUserAction(Context, $"Failed to fetch deposit info.");
            }

            DiscordHelpers.DeleteNonPrivateMessage(Context);
        }

        [Command("withdraw")]
        [HelpCategory("Tipping")]
        [Description("Withdraws Dallar into given Dallar wallet address")]
        public async Task WithdrawFromWalletInstant(CommandContext Context, [Description("Amount of DAL to withdraw. Use 'all' for your entire balance")] string AmountStr, [Description("Dallar Wallet Address to withdraw Dallar to")] string PublicAddress)
        {
            IDallarClientService DallarClientService = Context.Services.GetService(typeof(IDallarClientService)) as IDallarClientService;

            // Make sure supplied address is a valid Dallar address
            if (!DallarClientService.IsAddressValid(PublicAddress))
            {
                // handle invalid public address
                LogHandlerExtensions.LogUserAction(Context, $"Tried to withdraw but PublicAddress ({PublicAddress}) is invalid.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: Seems like you tried withdrawing Dallar to an invalid Dallar address. You supplied: {PublicAddress}");
                return;
            }

            DallarAccount WithdrawAccount = new DallarAccount()
            {
                KnownAddress = PublicAddress
            };

            DallarAccount Account = DiscordHelpers.DallarAccountFromDiscordUser(Context.User);
            // Try to interpret the user's amount input as a sane value
            if (!DallarClientService.TryParseAmountString(Account, AmountStr, false, out decimal Amount))
            {
                // handle amount parse fail
                LogHandlerExtensions.LogUserAction(Context, $"Tried to withdraw {Amount} but value could not be parsed.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: The amount you tried to withdraw can not be parsed as a number. You tried withdrawing {Amount} DAL.");
                return;
            }

            // Make sure Amount is greater than zero
            if (Amount <= 0)
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to withdraw {Amount} but value is invalid.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: You can not withdraw 0 or less Dallar. You tried withdrawing {Amount} DAL.");
                return;
            }

            // Verify user has requested balance to withdraw
            if (!DallarClientService.CanAffordTransaction(Account, Account, Amount, true, out decimal TransactionFee))
            {
                // user can not afford requested withdraw amount
                LogHandlerExtensions.LogUserAction(Context, $"Tried to withdraw {Amount} but has insufficient funds. ({DallarClientService.GetAccountBalance(Account)} DAL)");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: Looks like you don't have enough funds withdraw {Amount} DAL!");
                return;
            }

            // Amount should be guaranteed a good value to withdraw
            if (DallarClientService.SendFromAccountToAccount(Account, WithdrawAccount, Amount, true, out _))
            {
                // Successfully withdrew
                LogHandlerExtensions.LogUserAction(Context, $"Successfully withdrew {Amount} to address ({WithdrawAccount.KnownAddress}).");
                if (Context.Member != null)
                {
                    _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"You have successfully withdrawn {Amount} DAL.");
                }
                else
                {
                    await Context.RespondAsync($"You have successfully withdrawn {Amount} DAL to address ({WithdrawAccount.KnownAddress}).");
                }   
            }
            else
            {   // unable to send dallar
                LogHandlerExtensions.LogUserAction(Context, $"Tried to withdraw {Amount} from wallet ({Account.KnownAddress}) but daemon failed to send transaction.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, "Something went wrong trying to send your Dallar through the Dallar daemon. (Please contact the Administrators!)");
                return;
            }
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
            LogHandlerExtensions.LogUserAction(Context, $"Invoked sending {Amount} to a random user with minimum status {MinimumStatus.ToString()}.");

            IFunServiceCollection FunServiceCollection = Context.Services.GetService(typeof(IFunServiceCollection)) as IFunServiceCollection;

            IEnumerable<DiscordMember> Members = DiscordHelpers.GetHumansInContextGuild(Context, true, MinimumStatus);
            int randomIndex = FunServiceCollection.RandomManagerService.GetRandomInteger(0, Members.Count() - 1);

            DiscordMember Member = Members.ElementAt(randomIndex);
            if (Member != null)
            {
                await SendDallarToUserInternal(Context, Amount.ToString(), Member, true);
            }
            else
            {   // failed to get random member?
                LogHandlerExtensions.LogUserAction(Context, $"Failed to get a random user from the guild.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: DallarBot has failed to get a random user from the guild. Please contact an Administrator.");
            }
        }

        // The 'real' send function that all sends should filter into
        public async Task SendDallarToUserInternal(CommandContext Context, [Description("Amount of Dallar to send")] string AmountStr, DiscordMember Member, bool IsRandomSend = false)
        {
            IDallarClientService DallarClientService = Context.Services.GetService(typeof(IDallarClientService)) as IDallarClientService;

            await Context.TriggerTypingAsync();

            string RandomUserString = IsRandomSend ? " to a random user" : "";
            // Error out if this is a private message
            if (Context.Member == null)
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to send Dallar{RandomUserString} but user not in a guild.");
                await Context.RespondAsync($"{Context.User.Mention}: You must be in a Discord server with DallarBot to give to others.");
                return;
            }

            DallarAccount Account = DiscordHelpers.DallarAccountFromDiscordUser(Context.User);
            if (!DallarClientService.TryParseAmountString(Account, AmountStr, false, out decimal Amount))
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to send Dallar but could not parse amount: {AmountStr}");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: Could not parse amount.");
                return;
            }

            // Error out if trying to send an invalid amount
            if (Amount <= 0)
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to send Dallar{RandomUserString} but sender requested something less than or equal to zero.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: You can not send negative or zero Dallars.");
                return;
            }

            // Error out if user is trying to send to themselves
            if (Context.User.Id == Member.Id)
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to send Dallar to themselves.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: You can not send Dallar to yourself.");
                return;
            }

            // Can user afford transaction?
            if (DallarClientService.GetAccountBalance(Account) < Amount)
            {
                LogHandlerExtensions.LogUserAction(Context, $"Tried to send {Amount} DAL{RandomUserString} but could not afford it.");
                _ = DiscordHelpers.PromptUserToDeleteMessage(Context, $"{Context.User.Mention}: You do not have {Amount} DAL to send.");
                return;
            }

            DallarAccount ToAccount = DiscordHelpers.DallarAccountFromDiscordUser(Member);

            // Were we able to successfully send the transaction?
            if (DallarClientService.MoveFromAccountToAccount(Account, ToAccount, Amount))
            {
                bool bDisplayUSD = false;
                IDallarPriceProviderService DallarPriceProvider = Context.Services.GetService(typeof(IDallarPriceProviderService)) as IDallarPriceProviderService;
                if (DallarPriceProvider.GetPriceInfo(out DallarPriceInfo PriceInfo, out bool bPriceStale))
                {
                    bDisplayUSD = true;
                }

                LogHandlerExtensions.LogUserAction(Context, $"Sent {Amount} DAL ${(IsRandomSend ? "randomly " : "")}to User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToAccount.KnownAddress}.");
                string ReplyStr = $"{Context.User.Mention}: You have successfully {(IsRandomSend ? "randomly " : "")}sent {Member.Mention} {Amount} DAL.";

                if (bDisplayUSD)
                {
                    ReplyStr += $" {decimal.Round(Amount * PriceInfo.PriceInUSD, 4)} USD)";
                }

                await Context.RespondAsync(ReplyStr);
                _ = Context.Message.DeleteAsync();
                return;
            }
            else
            {   // sending failed?
                LogHandlerExtensions.LogUserAction(Context, $"Failed to have daemon send{RandomUserString} {Amount} DAL to User {Member.Id.ToString()} ({Member.Username.ToString()}) with address {ToAccount.KnownAddress}.");
                await Context.RespondAsync($"{Context.User.Mention}: DallarBot has failed to send{RandomUserString} {Amount} DAL. Please contact an Administrator.");
                _ = Context.Message.DeleteAsync();
                return;
            }
        }
    }
}
