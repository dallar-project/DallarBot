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
        DaemonClient DaemonClient { get; }

        void AttemptJoinChannel(string channel);
        void AttemptLeaveChannel(string channel);

        void SetAccountOverrider(IDallarAccountOverrider DallarAccountOverrider);

        event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;
    }

    public class TwitchBot : ITwitchBot
    {
        static DigitalPriceExchangeService DigitalPriceExchange;
        protected static DaemonClient _DaemonClient;
        TwitchClient client;

        protected IDallarSettingsCollection SettingsCollection;
        protected IDallarAccountOverrider AccountOverrider;

        public event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public TwitchBot(IDallarSettingsCollection DallarSettingsCollection)
        {
            SettingsCollection = DallarSettingsCollection;

            ConnectionCredentials credentials = new ConnectionCredentials(SettingsCollection.TwitchBot.Username, SettingsCollection.TwitchBot.AccessToken);

            DigitalPriceExchange = new DigitalPriceExchangeService();

            _DaemonClient = new DaemonClient(SettingsCollection.Daemon.IpAddress + ":" + SettingsCollection.Daemon.Port, SettingsCollection.Daemon.Username, SettingsCollection.Daemon.Password, SettingsCollection.Dallar.Txfee, new DallarAccount() { AccountId = SettingsCollection.Dallar.FeeAccount });

            client = new TwitchClient();
            client.Initialize(credentials, null, 'd', 'd');

            client.OnJoinedChannel += onJoinedChannel;
            client.OnConnected += Client_OnConnected;

            client.OnChatCommandReceived += onCommandReceived;

            client.Connect();
        }

        public DaemonClient DaemonClient
        {
            get { return _DaemonClient; }
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
            if (DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                bDisplayUSD = true;
            }

            DallarAccount account = GetChatCommandAccount(e);
            if (_DaemonClient.GetWalletAddressFromAccount(true, ref account))
            {
                decimal balance = _DaemonClient.GetRawAccountBalance(account);
                decimal pendingBalance = _DaemonClient.GetUnconfirmedAccountBalance(account);

                string pendingBalanceStr = pendingBalance != 0 ? $" with {pendingBalance} DAL Pending" : "";
                string resultStr = $"@{e.Command.ChatMessage.DisplayName}: Your balance is {balance} DAL";
                if (bDisplayUSD)
                {
                    resultStr += $" (${decimal.Round(balance * PriceInfo.USDValue.GetValueOrDefault(), 4)} USD){pendingBalanceStr}";
                }
                else
                {
                    resultStr += pendingBalanceStr;
                }

                //LogHandlerExtensions.LogUserAction(Context, $"Checked balance. {balance} DAL with {pendingBalance} DAL pending.");
                client.SendMessage(e.Command.ChatMessage.Channel, resultStr);
            }
            else
            {
                //LogHandlerExtensions.LogUserAction(Context, $"Failed to check balance. Getting wallet address failed.");
                client.SendMessage(e.Command.ChatMessage.Channel, $"@{e.Command.ChatMessage.DisplayName}: Failed to check balance. Getting wallet address failed. Please contact an Administrator.");
            }
        }

        public void GetDallarDeposit(OnChatCommandReceivedArgs e)
        {
            DallarAccount account = GetChatCommandAccount(e);
            if (_DaemonClient.GetWalletAddressFromAccount(true, ref account))
            {
                string resultStr = $"Your deposit address is {account.KnownAddress}. For help or more information, please visit http://dallar.tv/help";
                client.SendWhisper(e.Command.ChatMessage.Username, resultStr);
            }
            else
            {
                //await DiscordHelpers.RespondAsDM(Context, $"{Context.User.Mention}: Failed to fetch your wallet address. Please contact an Administrator.");
                //LogHandlerExtensions.LogUserAction(Context, $"Failed to fetch deposit info.");
            }

            //DiscordHelpers.DeleteNonPrivateMessage(Context);
        }
    }
}