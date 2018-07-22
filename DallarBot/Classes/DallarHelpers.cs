using Dallar;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DallarBot.Classes
{
    class DallarHelpers
    {
        public static void GetTXFeeAndAccount(out decimal txfee, out string feeAccount)
        {
            txfee = Program.SettingsCollection.Dallar.Txfee;
            feeAccount = Program.SettingsCollection.Dallar.FeeAccount;
        }

        public static bool CanUserAffordTransactionAmount(DiscordUser User, decimal Amount)
        {
            decimal balance = Program.DaemonClient.GetRawAccountBalance(DiscordHelpers.DallarAccountFromDiscordUser(User));
            decimal txfee = Program.SettingsCollection.Dallar.Txfee;

            if (Amount + txfee > balance)
            {
                return false;
            }

            return true;
        }

        public static bool TryParseUserAmountString(DiscordUser User, string AmountStr, out decimal Amount)
        {
            if (AmountStr == "all")
            {
                decimal balance = Program.DaemonClient.GetRawAccountBalance(DiscordHelpers.DallarAccountFromDiscordUser(User));
                decimal txfee = Program.SettingsCollection.Dallar.Txfee;

                Amount = balance - txfee;
                return true;
            }

            Amount = 0m;
            return decimal.TryParse(AmountStr, out Amount);
        }

        public static bool UserPayAmountToFeeAccount(DiscordUser User, decimal Amount)
        {
            
            if (!CanUserAffordTransactionAmount(User, Amount))
            {
                return false;
            }

            DallarAccount Account = DiscordHelpers.DallarAccountFromDiscordUser(User);
            if (!Program.DaemonClient.SendMinusFees(Account, Program.DaemonClient.FeeAccount, Amount, true))
            {
                return false;
            }

            return true;
        }
    }
}
