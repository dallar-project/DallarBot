using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

using DallarBot.Classes;

namespace DallarBot.Services
{
    public class LogHandlerService
    {
        public DiscordSocketClient discord;

        public LogHandlerService(DiscordSocketClient _discord)
        {
            discord = _discord;

            discord.Connected += Connected;

            discord.Ready += Ready;

            discord.Disconnected += Disconnected;

            discord.Log += Log;
        }

        private async Task Log(LogMessage arg)
        {
            await System.IO.File.AppendAllTextAsync(Environment.CurrentDirectory + "/log.txt", arg.Message + Environment.NewLine);
        }

        private Task Disconnected(Exception ex)
        {
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            Console.WriteLine(CenterString("Disconnected."));
            Console.WriteLine("");
            Console.WriteLine(CenterString("Something went wrong..."));
            Console.WriteLine("");
            Console.WriteLine(CenterString(ex.Message));
            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            return Task.Delay(0);
        }

        private Task Connected()
        {
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            Console.WriteLine(CenterString("Connected."));
            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            return Task.Delay(0);
        }

        private Task Ready()
        {
            Console.WriteLine(CenterString("----------------------------"));
            Console.WriteLine("");
            Console.WriteLine(CenterString("Bot is readied."));
            Console.WriteLine("");
            Console.WriteLine(CenterString("----------------------------"));
            return Task.Delay(0);
        }

        public string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }
    }
}
