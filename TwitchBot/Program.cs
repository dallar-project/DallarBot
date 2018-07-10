using Dallar;
using System;
using System.Net;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot bot = new Bot();
            Console.ReadLine();
        }
    }

    class Bot
    {
        static DigitalPriceExchangeService DigitalPriceExchange;
        static DallarSettingsCollection SettingsCollection;
        static DaemonClient DaemonClient;
        TwitchClient client;

        public Bot()
        {
            DallarSettingsCollection.FromConfig(Environment.CurrentDirectory + "/settings.json", out SettingsCollection);

            ConnectionCredentials credentials = new ConnectionCredentials(SettingsCollection.Twitch.Username, SettingsCollection.Twitch.AccessToken);

            DigitalPriceExchange = new DigitalPriceExchangeService();

            DaemonClient = new DaemonClient(SettingsCollection.Daemon.IpAddress + ":" + SettingsCollection.Daemon.Port)
            {
                credentials = new NetworkCredential(SettingsCollection.Daemon.Username, SettingsCollection.Daemon.Password)
            };

            client = new TwitchClient();
            client.Initialize(credentials, "awesomeallar", 'd', 'd');

            client.OnJoinedChannel += onJoinedChannel;
            client.OnConnected += Client_OnConnected;

            client.OnChatCommandReceived += onCommandReceived;

            client.Connect();
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
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

        private void CheckBalance(OnChatCommandReceivedArgs e)
        {
            bool bDisplayUSD = false;
            if (DigitalPriceExchange.GetPriceInfo(out DigitalPriceCurrencyInfo PriceInfo, out bool bPriceStale))
            {
                bDisplayUSD = true;
            }

            if (DaemonClient.GetWalletAddressFromAccount("twitch_" + e.Command.ChatMessage.UserId, true, out string Wallet))
            {
                decimal balance = DaemonClient.GetRawAccountBalance("twitch_" + e.Command.ChatMessage.UserId);
                decimal pendingBalance = DaemonClient.GetUnconfirmedAccountBalance("twitch_" + e.Command.ChatMessage.UserId);

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
            if (DaemonClient.GetWalletAddressFromAccount("twitch_" + e.Command.ChatMessage.UserId, true, out string Wallet))
            {
                string resultStr = $"Your deposit address is {Wallet}. For help or more information, please visit http://dallar.tv/help";
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