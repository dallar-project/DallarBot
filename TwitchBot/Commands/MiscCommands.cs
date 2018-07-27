using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace Dallar.Bots
{
    public partial class TwitchBot
    {
        protected const decimal DadJokeCost = 1;
        protected const decimal AllarJokeCost = 1;
        protected const decimal MomJokeCost = 1;

        protected void DadJokeCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
            string Joke = FunServiceCollection.DadJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                return;
            }

            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();
            if (!DallarClientService.MoveFromAccountToAccount(Account, FeeAccount, DadJokeCost))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"Sorry, you do not have the {DadJokeCost} Dallar to spend to summon a dad joke.");
                return;
            }

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName} has spent {DadJokeCost} Dallar: {Joke}");
        }

        protected void AllarJokeCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
            string Joke = FunServiceCollection.ChuckNorrisJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                return;
            }

            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();
            if (!DallarClientService.MoveFromAccountToAccount(Account, FeeAccount, AllarJokeCost))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"Sorry, you do not have the {AllarJokeCost} Dallar to spend to summon an Allar joke.");
                return;
            }

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName} has spent {AllarJokeCost} Dallar: {Joke}");
        }

        protected void MomJokeCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
            string Joke = FunServiceCollection.YoMommaJokeService.GetJoke();
            if (string.IsNullOrEmpty(Joke))
            {
                return;
            }

            DallarAccount Account = InvokerTwitchAccount.GetDallarAccount();
            if (!DallarClientService.MoveFromAccountToAccount(Account, FeeAccount, MomJokeCost))
            {
                SendWhisper(InvokerTwitchAccount.Username, $"Sorry, you do not have the {MomJokeCost} Dallar to spend to summon a Yo Momma joke.");
                return;
            }

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName} has spent {MomJokeCost} Dallar: {Joke}");
        }
    }
}
