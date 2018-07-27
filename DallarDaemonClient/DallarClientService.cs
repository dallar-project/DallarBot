using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using BitcoinLib.Services.Coins.Base;
using BitcoinLib.Services.Coins.Dallar;
using System.Threading;
using BitcoinLib.Responses;

namespace Dallar.Services
{
    public interface IDallarClientService
    {
        decimal GetDifficulty();
        uint GetBlockCount();

        bool IsAddressValid(string Address);

        decimal GetAccountBalance(DallarAccount Account, int MinConfirmationBlocks = 6);
        decimal GetAccountPendingBalance(DallarAccount Account, int MinConfirmationBlocks = 6);

        bool ResolveDallarAccountAddress(ref DallarAccount Account);

        bool CanAffordTransaction(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount, bool bSubtractFeeFromAmount, out decimal TransactionFee);

        bool SendFromAccountToAccount(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount, bool bSubtractFeeFromAmount, out string TransactionID);
        bool MoveFromAccountToAccount(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount);

        bool TryParseAmountString(DallarAccount Account, string AmountStr, bool bSubtractTxFee, out decimal Amount);
    }

    public class DallarClientService : IDallarClientService
    {
        protected IDallarSettingsCollection SettingsCollection;
        protected ICoinService CoinService;
        protected static Mutex TransactionMutex = new Mutex();

        public DallarClientService(IDallarSettingsCollection SettingsCollection)
        {
            this.SettingsCollection = SettingsCollection;
            this.CoinService = new DallarService("http://" + SettingsCollection.Daemon.IpAddress + ":" + SettingsCollection.Daemon.Port, SettingsCollection.Daemon.Username, SettingsCollection.Daemon.Password, "", 5);
        }

        public decimal GetAccountBalance(DallarAccount Account, int MinConfirmationBlocks = 6)
        {
            return CoinService.GetBalance(Account.UniqueAccountName, MinConfirmationBlocks);
        }

        public decimal GetAccountPendingBalance(DallarAccount Account, int MinConfirmationBlocks = 6)
        {
            var Total = GetAccountBalance(Account, 0);
            return Total - GetAccountBalance(Account, MinConfirmationBlocks);
        }

        public decimal GetDifficulty()
        {
            return (decimal)CoinService.GetDifficulty();
        }

        public uint GetBlockCount()
        {
            return CoinService.GetBlockCount();
        }

        public bool ResolveDallarAccountAddress(ref DallarAccount Account)
        {
            if (Account.IsAddressKnown)
            {
                return true;
            }

            Account.KnownAddress = CoinService.GetAccountAddress(Account.UniqueAccountName);
            return true;
        }

        public bool SendFromAccountToAccount(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount, bool bSubtractFeeFromAmount, out string TransactionID)
        {
            TransactionID = "";
            if (Amount <= 0)
            {
                Console.WriteLine("Dallar Client Service can not send a zero or less Amount.");
                return false;
            }

            ResolveDallarAccountAddress(ref ToAccount);
            TransactionMutex.WaitOne();

            if (CanAffordTransaction(FromAccount, ToAccount, Amount, bSubtractFeeFromAmount, out decimal TransactionFee))
            { 
                TransactionID = CoinService.SendFrom(FromAccount.UniqueAccountName, ToAccount.KnownAddress, bSubtractFeeFromAmount ? Amount - TransactionFee : Amount);
            }
            else
            {
                Console.WriteLine($"Dallar Client Service could not send as {FromAccount.UniqueAccountName} can not afford {Amount} Dallar.");
                TransactionMutex.ReleaseMutex();
                return false;
            }

            TransactionMutex.ReleaseMutex();
            return true;
        }

        public bool MoveFromAccountToAccount(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount)
        {
            if (Amount <= 0)
            {
                Console.WriteLine("Dallar Client Service can not move a zero or less Amount.");
                return false;
            }

            TransactionMutex.WaitOne();

            decimal balance = GetAccountBalance(FromAccount, 6);
            decimal targetBalance = GetAccountBalance(ToAccount, 6);
            if (balance >= Amount)
            {
                if (!CoinService.Move(FromAccount.UniqueAccountName, ToAccount.UniqueAccountName, Amount, 6))
                {
                    Console.WriteLine("Dallar Client Service failed to move?");
                    TransactionMutex.ReleaseMutex();
                    return false;
                }
            }
            else
            {
                TransactionMutex.ReleaseMutex();
                return false;
            }

            TransactionMutex.ReleaseMutex();
            return true;
        }

        public bool IsAddressValid(string Address)
        {
            return CoinService.ValidateAddress(Address).IsValid;
        }

        public bool CanAffordTransaction(DallarAccount FromAccount, DallarAccount ToAccount, decimal Amount, bool bSubtractFeeFromAmount, out decimal TransactionFee)
        {
            ResolveDallarAccountAddress(ref ToAccount);

            var ds = CoinService as DallarService;
            TransactionFee = ds.GetEstimateFeeForSendToAddress(ToAccount.KnownAddress, Amount);
            if (bSubtractFeeFromAmount)
            {
                decimal UpdatedFee = ds.GetEstimateFeeForSendToAddress(ToAccount.KnownAddress, Amount - TransactionFee);
                TransactionFee = Math.Max(TransactionFee, UpdatedFee);
                Amount -= TransactionFee;
            }

            return GetAccountBalance(FromAccount) >= Amount + TransactionFee;
        }

        public bool TryParseAmountString(DallarAccount Account, string AmountStr, bool bSubtractTxFee, out decimal Amount)
        {
            if (AmountStr.Equals("all", StringComparison.InvariantCultureIgnoreCase))
            {
                Amount = GetAccountBalance(Account);

                if (!bSubtractTxFee)
                {
                    return true;
                }

                CanAffordTransaction(Account, Account, Amount, bSubtractTxFee, out decimal TransactionFee);
                Amount = Amount - TransactionFee;

                return true;
            }

            Amount = 0m;
            if (!decimal.TryParse(AmountStr, out Amount))
            {
                return false;
            }

            Amount = Math.Max(0, Amount);
            if (Amount == 0)
            {
                return false;
            }

            return true;
        }
    }
}
