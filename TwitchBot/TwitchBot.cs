using Dallar.Exchange;
using Dallar.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Models.v5.Users;
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

    public class TwitchUserAccountContext
    {
        public string Username { get; protected set; }
        public string DisplayName { get; protected set; }
        public string UserId { get; protected set; }
        public bool IsFromWhisper { get; protected set; }
        public string CurrentChannel { get; protected set; }
        public bool IsBroadcaster { get; protected set; }
        public bool IsMod { get; protected set; }

        protected DallarAccount DallarAccount;
        public DallarAccount GetDallarAccount() { return DallarAccount; }

        public TwitchUserAccountContext(OnChatCommandReceivedArgs e, IDallarAccountOverrider AccountOverrider)
        {
            Username = e.Command.ChatMessage.Username;
            DisplayName = e.Command.ChatMessage.DisplayName;
            UserId = e.Command.ChatMessage.UserId;
            IsFromWhisper = false;
            CurrentChannel = e.Command.ChatMessage.Channel;
            IsBroadcaster = e.Command.ChatMessage.IsBroadcaster;
            IsMod = e.Command.ChatMessage.IsModerator;

            DallarAccount = new DallarAccount()
            {
                AccountPrefix = "twitch_", // @TODO: Not hardcode this?
                AccountId = UserId
            };

            AccountOverrider?.OverrideDallarAccountIfNeeded(ref DallarAccount);
        }

        public TwitchUserAccountContext(OnWhisperCommandReceivedArgs e, IDallarAccountOverrider AccountOverrider)
        {
            Username = e.Command.WhisperMessage.Username;
            DisplayName = e.Command.WhisperMessage.DisplayName;
            UserId = e.Command.WhisperMessage.UserId;
            IsFromWhisper = true;
            CurrentChannel = null;
            IsBroadcaster = false;
            IsMod = false;

            DallarAccount = new DallarAccount()
            {
                AccountPrefix = "twitch_", // @TODO: Not hardcode this?
                AccountId = UserId
            };

            AccountOverrider?.OverrideDallarAccountIfNeeded(ref DallarAccount);
        }
    }

    public interface ITwitchBot
    {
        void AttemptJoinChannel(string channel);
        void AttemptLeaveChannel(string channel);

        void SetAccountOverrider(IDallarAccountOverrider DallarAccountOverrider);

        event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        void SendChannelMessage(string channel, string message);
        void SendWhisper(string user, string message);

        void Reply(TwitchUserAccountContext TwitchUserAccountContext, string Message);
    }

    public partial class TwitchBot : ITwitchBot
    {
        protected DallarAccount FeeAccount = new DallarAccount()
        {
            AccountId = "Bot Fee Account"
        };

        protected string BotURL = "https://bot.dallar.org";

        protected Regex UserNameRegex = new Regex(@"^@([\w]{1,25})$", RegexOptions.IgnoreCase);

        protected IDallarClientService DallarClientService;
        protected IDallarPriceProviderService DallarPriceProviderService; 
        protected IDallarSettingsCollection SettingsCollection;
        protected IFunServiceCollection FunServiceCollection;
        protected IDallarAccountOverrider AccountOverrider;

        private static readonly HttpClient HttpClient = new HttpClient();
        TwitchClient client;
        TwitchAPI api;

        public event EventHandler<OnConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public TwitchBot(IDallarSettingsCollection DallarSettingsCollection, IDallarClientService DallarClientService, IFunServiceCollection FunServiceCollection, IDallarPriceProviderService DallarPriceProviderService)
        {
            SettingsCollection = DallarSettingsCollection;
            this.DallarClientService = DallarClientService;

            ConnectionCredentials credentials = new ConnectionCredentials(SettingsCollection.TwitchBot.Username, SettingsCollection.TwitchBot.AccessToken);

            this.DallarPriceProviderService = DallarPriceProviderService;
            this.FunServiceCollection = FunServiceCollection;

            var authResponse = HttpClient.PostAsync($"https://id.twitch.tv/oauth2/token?client_id={SettingsCollection.TwitchAuth.ClientId}&client_secret={SettingsCollection.TwitchAuth.ClientSecret}&grant_type=client_credentials", null).GetAwaiter().GetResult();
            var responseString = JObject.Parse(authResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            string accesstoken = responseString["access_token"].ToString();

            api = new TwitchAPI();
            api.Settings.ClientId = SettingsCollection.TwitchAuth.ClientId;
            api.Settings.AccessToken = accesstoken;

            client = new TwitchClient();
            client.Initialize(credentials, null, 'd', 'd');

            client.OnJoinedChannel += onJoinedChannel;
            client.OnConnected += Client_OnConnected;

            client.OnChatCommandReceived += onCommandReceived;
            client.OnWhisperCommandReceived += onWhisperCommandReceived;

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
            Console.WriteLine("Joined channel " + e.Channel);
            client.SendMessage(e.Channel, "Dallar Bot has just entered your channel or recently has been restarted.");
        }

        private void onCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            TwitchUserAccountContext c = new TwitchUserAccountContext(e, AccountOverrider);
            RouteCommand(c, e.Command.CommandText, e.Command.ArgumentsAsList);
        }


        private void onWhisperCommandReceived(object sender, OnWhisperCommandReceivedArgs e)
        {
            TwitchUserAccountContext c = new TwitchUserAccountContext(e, AccountOverrider);
            RouteCommand(c, e.Command.CommandText, e.Command.ArgumentsAsList);            
        }

        protected void RouteCommand(TwitchUserAccountContext c, string CommandText, List<string> CommandArgs)
        {
            switch (CommandText)
            {
                // Misc Commands
                case "!dad":
                    Task.Run(() => DadJokeCommand(c));
                    break;
                case "!allar":
                    Task.Run(() => AllarJokeCommand(c));
                    break;
                case "!mom":
                case "!momma":
                case "!mum":
                    Task.Run(() => MomJokeCommand(c));
                    break;
                // Tipping Commands
                case "!bal":
                case "!balance":
                    Task.Run(() => BalanceCommand(c));
                    break;
                case "!deposit":
                    Task.Run(() => DepositCommand(c));
                    break;
                case "!withdraw":
                    Task.Run(() => WithdrawCommand(c, CommandArgs));
                    break;
                case "!send":
                case "!give":
                case "!transfer":
                    SendCommand(c, CommandArgs); //async
                    break;
                // Dallar Commands
                case "!block":
                case "!difficulty":
                case "!diff":
                    Task.Run(() => DifficultyCommand(c));
                    break;
                // Exchange Commands
                case "!usd":
                case "!usd-dal":
                    Task.Run(() => USDCommand(c, CommandArgs));
                    break;
                case "!btc":
                case "!btc-dal":
                    Task.Run(() => BTCCommand(c, CommandArgs));
                    break;
                case "!dal":
                case "!dal-btc":
                case "!dal-usd":
                    Task.Run(() => DALCommand(c, CommandArgs));
                    break;
                default:
                    break;
            }
        }

        public void SendChannelMessage(string channel, string message)
        {
            JoinedChannel jc = client.GetJoinedChannel(channel);
            if (jc == null)
            {
                return;
            }

            client.SendMessage(jc, message);
        }

        public void SendWhisper(string user, string message)
        {
            client.SendWhisper(user, message);
        }

        public void Reply(TwitchUserAccountContext TwitchUserAccountContext, string Message)
        {
            if (TwitchUserAccountContext.IsFromWhisper)
            {
                SendWhisper(TwitchUserAccountContext.Username, Message);
            }
            else if (!string.IsNullOrEmpty(TwitchUserAccountContext.CurrentChannel))
            {
                SendChannelMessage(TwitchUserAccountContext.Username, Message);
            }
        }

        public class ResolvedTwitchUserName
        {
            public string TwitchUserId;
            public DallarAccount DallarAccount;
        }

        public async Task<ResolvedTwitchUserName> TryResolveTwitchUserNameCommandArg(string CommandArg)
        {
            Match isValidName = UserNameRegex.Match(CommandArg);
            if (!isValidName.Success)
            {
                return null;
            }

            string TwitchUserName = isValidName.Groups[1].ToString();
            try
            {
                Users Users = await api.Users.v5.GetUserByNameAsync(TwitchUserName);
                if (Users.Total != 1)
                {
                    return null;
                }

                ResolvedTwitchUserName resolved = new ResolvedTwitchUserName
                {
                    TwitchUserId = Users.Matches[0].Id,
                    DallarAccount = new DallarAccount()
                    {
                        AccountPrefix = "twitch_", // @TODO: Not hardcode?
                        AccountId = Users.Matches[0].Id
                    }
                };

                AccountOverrider?.OverrideDallarAccountIfNeeded(ref resolved.DallarAccount);

                return resolved;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}