using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Timers;
using System;

namespace DallarBot.Classes
{
    public class WithdrawManager
    {
        Timer MessageTimer = new Timer(1000);
        int TimePassedThreshold = 90;
        DateTime TimePosted = DateTime.Now;

        public SocketGuildUser guildUser;
        public decimal amount;

        public WithdrawManager(SocketGuildUser _guildUser, decimal _amount)
        {
            guildUser = _guildUser;
            amount = _amount;

            MessageTimer.Elapsed += MessageTimer_Elapsed;
            MessageTimer.Enabled = true;
            MessageTimer.Start();
        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((DateTime.Now - TimePosted).TotalSeconds >= TimePassedThreshold)
            {
                MessageTimer.Stop();
                guildUser = null;
            }
        }
    }
}
