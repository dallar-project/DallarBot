﻿using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DallarBot.Classes
{
    class DallarHelpers
    {
        public static void GetTXFeeAndAccount(out decimal txfee, out string feeAccount)
        {
            txfee = Program.SettingsHandler.Dallar.Txfee;
            feeAccount = Program.SettingsHandler.Dallar.FeeAccount;
        }

        public static bool CanUserAffordTransactionAmount(DiscordUser User, decimal Amount)
        {
            decimal balance = Program.DaemonClient.GetRawAccountBalance(User.Id.ToString());
            decimal txfee = Program.SettingsHandler.Dallar.Txfee;

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
                decimal balance = Program.DaemonClient.GetRawAccountBalance(User.Id.ToString());
                decimal txfee = Program.SettingsHandler.Dallar.Txfee;

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

            if (!Program.DaemonClient.SendMinusFees(User.Id.ToString(), Program.DaemonClient.GetAccountAddress(Program.SettingsHandler.Dallar.FeeAccount), Amount, Program.SettingsHandler.Dallar.Txfee, Program.SettingsHandler.Dallar.FeeAccount))
            {
                return false;
            }

            return true;
        }
    }
}
