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

//using DallarBot.Modules;

namespace DallarBot.Classes
{
    class MessageManager
    {
        IUserMessage Message = null;
        Timer MessageTimer = new Timer(1000);
        int TimePassedThreshold = 90;
        DateTime TimePosted = DateTime.Now;

        public MessageManager(IUserMessage _Message)
        {
            Message = _Message;

            MessageTimer.Elapsed += MessageTimer_Elapsed;
            MessageTimer.Enabled = true;
            MessageTimer.Start();
        }

        public MessageManager(IUserMessage _Message, int time)
        {
            Message = _Message;

            TimePassedThreshold = time;
            MessageTimer.Elapsed += MessageTimer_Elapsed;
            MessageTimer.Enabled = true;
            MessageTimer.Start();
        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Message != null)
            { 
                if ((DateTime.Now - TimePosted).TotalSeconds >= TimePassedThreshold)
                {
                    MessageTimer.Stop();
                    Message.DeleteAsync();
                }
            }
            else
            {
                MessageTimer.Stop();
            }
        }
    }
}
