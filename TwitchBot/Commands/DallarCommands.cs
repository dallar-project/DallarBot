using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Events;

namespace Dallar.Bots
{
    public partial class TwitchBot
    {
        protected void DifficultyCommand(TwitchUserAccountContext InvokerTwitchAccount)
        {
            uint BlockCount = DallarClientService.GetBlockCount();
            decimal Difficulty = DallarClientService.GetDifficulty();

            Reply(InvokerTwitchAccount, $"@{InvokerTwitchAccount.DisplayName}: Block difficulty for block {BlockCount} is {Difficulty}.");
        }
    }
}
