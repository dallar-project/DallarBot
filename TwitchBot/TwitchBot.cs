using Dallar.Exchange;
using Dallar.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Dallar.Bots
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTwitchBot(
            this IServiceCollection services)
        {
            services.AddSingleton<ITwitchBot, TwitchBot>();
            return services;
        }
    }

    public class OnConnectionStatusChangedEventArgs : EventArgs
    {
        public bool bConnected { get; set; }
    }

    public interface ITwitchBot
    {
        void AttemptJoinChannel(string channel);
        void AttemptLeaveChannel(string channel);

        void SetAccountOverrider(IDallarAccountOverrider DallarAccountOverrider);

        event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;
    }

    public class TwitchBot : ITwitchBot
    {
        protected IDallarClientService DallarClientService;
        protected IDallarPriceProviderService DallarPriceProviderService; 
        TwitchClient client;

        protected IDallarSettingsCollection SettingsCollection;
        protected IDallarAccountOverrider AccountOverrider;

        public event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public TwitchBot(IDallarSettingsCollection DallarSettingsCollection, IDallarClientService DallarClientService, IDallarPriceProviderService DallarPriceProviderService)
        {
            SettingsCollection = DallarSettingsCollection;
            this.DallarClientService = DallarClientService;

            ConnectionCredentials credentials = new ConnectionCredentials(SettingsCollection.TwitchBot.Username, SettingsCollection.TwitchBot.AccessToken);

            this.DallarPriceProviderService = DallarPriceProviderService;

            client = new TwitchClient();
            client.Initialize(credentials, null, 'd', 'd');

            client.OnJoinedChannel += onJoinedChannel;
            client.OnConnected += Client_OnConnected;

            client.OnChatCommandReceived += onCommandReceived;

            client.Connect();
        }

        public void SetAccountOverrider(IDallarAccountOverrider DallarAccountOverrider)
        {
            AccountOverrider = DallarAccountOverrider;
        }

        public void AttemptJoinChannel(string channel)
        {
            client.JoinChannel(channel);
        }

        public void AttemptLeaveChannel(string channel)
        {
            client.LeaveChannel(channel);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
            OnConnectionStatusChanged?.Invoke(this, new OnConnectionStatusChangedEventArgs()
            {
                bConnected = true
            });
        }

        private void onJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
            client.SendMessage(e.Channel, "Hey guys! I am a bot connected via TwitchLib!");
        }


        private void onCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText)
            {
                case "!bal":
                    CheckBalance(e);
                    break;
                case "!deposit":
                    GetDallarDeposit(e);
                    break;
                default:
                    break;
            }
        }

        protected static string TwitchAccountPrefix = "twitch_";

        protected DallarAccount GetChatCommandAccount(OnChatCommandReceivedArgs e)
        {
            DallarAccount TwitchAccount = new DallarAccount()
            {
                AccountId = e.Command.ChatMessage.UserId,
                AccountPrefix = TwitchAccountPrefix
            };

            if (AccountOverrider != null)
            {
                AccountOverrider.OverrideDallarAccountIfNeeded(ref TwitchAccount);
            }

            return TwitchAccount;
        }

        private void CheckBalance(OnChatCommandReceivedArgs e)
        {
            bool bDisplayUSD = false;
            if (DallarPriceProviderService.GetPriceInfo(out DallarPriceInfo PriceInfo, out bool bPriceStale))
            {
                bDisplayUSD = true;
            }

            DallarAccount Account = GetChatCommandAccount(e);
            decimal balance = DallarClientService.GetAccountBalance(Account);
            decimal pendingBalance = DallarClientService.GetAccountPendingBalance(Account);

            string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
            string resultStr = $"@{e.Command.ChatMessage.DisplayName}: Your balance is {balance} DAL";
            if (bDisplayUSD)
            {
                resultStr += $" (${decimal.Round(balance * PriceInfo.PriceInUSD, 4)} USD){pendingBalanceStr}";
            }
            else
            {
                resultStr += pendingBalanceStr;
            }

            client.SendMessage(e.Command.ChatMessage.Channel, resultStr);
        }

        public void GetDallarDeposit(OnChatCommandReceivedArgs e)
        {
            DallarAccount Account = GetChatCommandAccount(e);
            if (!DallarClientService.ResolveDallarAccountAddress(ref Account))
            {
                Console.WriteLine("ERROR: Failed to resolve Dallar account?");
            }

            client.SendWhisper(e.Command.ChatMessage.Username, $"Your deposit address is {Account.KnownAddress}. For help or more information, please visit http://dallar.tv/help");
        }
    }
}