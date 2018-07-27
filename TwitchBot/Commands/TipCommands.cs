using Dallar.Exchange;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Dallar.Bots
{
    public partial class TwitchBot
    {
        protected void BalanceCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
           DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();
            decimal bal = DallarClientService.GetAccountBalance(Account);

            string USD = "";
            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo DallarPriceInfo, out _))
            {
                USD = $" (${String.Format("{0:#,##0.00}", bal * DallarPriceInfo.PriceInUSD)} USD)";
            }

            decimal pending = DallarClientService.GetAccountPendingBalance(Account);
            string PendingStr = "";
            if (pending > 0)
            {
                PendingStr = $" {String.Format("{0:#,##0.0#######}", pending)} DAL pending.";
            }

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName} has {String.Format("{0:#,##0.0#######}", bal)} Dallar.{USD}{PendingStr}");
        }

        protected void DepositCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();
            if (!DallarClientService.ResolveDallarAccountAddress(ref Account))
            {
                Console.WriteLine("ERROR: Failed to resolve Dallar account?");
            }

            SendWhisper(InvokerTwitchAccount.Username, $"Your deposit address is {Account.KnownAddress}. For help, or a scannable QR code, please visit {BotURL}");
        }

        protected void WithdrawCommand(TwitchUserAccountContext InvokerTwitchAccount, List<string> Args)
        {
            if (Args.Count != 2)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the withdraw command incorrectly. The syntax is 'd!withdraw amount address'. If you would like to withdraw using the easier web interface, please visit {BotURL}");
                return;
            }

            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();

            // Unable to parse amount
            if (!DallarClientService.TryParseAmountString(Account, Args[0], false, out decimal Amount) || Amount <= 0)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the withdraw command with an invalid amount. The syntax is 'd!withdraw amount address'. If you would like to withdraw using the easier web interface, please visit {BotURL}");
                return;
            }

            // Invalid Address
            if (!DallarClientService.IsAddressValid(Args[1]))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the withdraw command with an invalid address. The syntax is 'd!withdraw amount address'. If you would like to withdraw using the easier web interface, please visit {BotURL}");
                return;
            }

            DallarAccount WithdrawAccount = new DallarAccount()
            {
                KnownAddress = Args[1]
            };

            // Verify user has requested balance to withdraw
            if (!DallarClientService.CanAffordTransaction(Account, WithdrawAccount, Amount, true, out decimal TransactionFee))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried withdrawing an amount you do not have. If you would like to withdraw using the easier web interface, please visit {BotURL}");
                return;
            }

            // Amount should be guaranteed a good value to withdraw
            if (DallarClientService.SendFromAccountToAccount(Account, WithdrawAccount, Amount, true, out string TxID))
            {
                Reply(InvokerTwitchAccount, $"You have successfully withdrawn {String.Format("{0:#,##0.0#######}", Amount)} DAL" + (!InvokerTwitchAccount.IsFromWhisper ? "" : $" to address '{WithdrawAccount.KnownAddress}'."));
                return;
            }
            else
            {   // unable to send dallar
                SendWhisper(InvokerTwitchAccount.Username, $"Something went wrong trying to perform your withdraw. Maybe you no longer have enough? If you would like to withdraw using the easier web interface, please visit {BotURL}");
            }
        }

        protected async void SendCommand(TwitchUserAccountContext InvokerTwitchAccount, List<string> Args)
        {
            if (InvokerTwitchAccount.IsFromWhisper)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"The send command can only be performed within a channel. For more help please visit {BotURL}");
                return;
            }

            if (Args.Count != 2)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the send command incorrectly. The syntax is 'd!send amount @user'. For more help please visit {BotURL}");
                return;
            }

            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();

            // Unable to parse amount
            if (!DallarClientService.TryParseAmountString(Account, Args[0], false, out decimal Amount) || Amount <= 0)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the send command with an invalid amount. The syntax is 'd!send amount @user'. For more help please visit {BotURL}");
                return;
            }

            if (DallarClientService.GetAccountBalance(Account) < Amount)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You can not afford to send anyone {Amount} Dallar. For more help please visit {BotURL}");
                return;
            }

            if (("@"+InvokerTwitchAccount.Username).Equals(Args[1], StringComparison.InvariantCultureIgnoreCase))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You can not send Dallar to yourself. For more help please visit {BotURL}");
                return;
            }

            ResolvedTwitchUserName resolved = await TryResolveTwitchUserNameCommandArg(Args[1]);
            if (resolved == null)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You have tried to use the send command with an invalid user. The syntax is 'd!send amount @user'. For more help please visit {BotURL}");
                return;
            }

            if (resolved.TwitchUserId == InvokerTwitchAccount.UserId)
            {
                SendWhisper(InvokerTwitchAccount.Username, $"You can not send Dallar to yourself. For more help please visit {BotURL}");
                return;
            }

            if (!DallarClientService.MoveFromAccountToAccount(Account, resolved.DallarAccount, Amount))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"Dallar Bot failed to send {Amount} to {Args[1]}. Perhaps you can no longer afford it? For more help please visit {BotURL}");
                return;
            }

            string USD = "";
            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo DallarPriceInfo, out _))
            {
                USD = $" (${Amount * DallarPriceInfo.PriceInUSD} USD)";
            }

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName} has sent {Args[1]} {Amount} Dallar.{USD}");
        }
    }
}
