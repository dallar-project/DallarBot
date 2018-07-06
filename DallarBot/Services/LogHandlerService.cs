using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;

namespace DallarBot.Services
{
    public class LogHandlerService
    {
        public LogHandlerService()
        {
        }

        // this method writes all of bot's log messages to debug output
        public static void DiscordLogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            string output = $"[{DateTime.Now.ToString()}] Log: [DISCORD][{e.Level}]: {e.Message}";
            Debug.WriteLine(output);
            System.IO.File.AppendAllText(Environment.CurrentDirectory + "/log.txt", output + Environment.NewLine);
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

        public static string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }

        public static async Task LogAsync(string log)
        {
            await System.IO.File.AppendAllTextAsync(Environment.CurrentDirectory + "/log.txt", log + Environment.NewLine);
        }

        public static void Log(string log)
        {
            System.IO.File.AppendAllText(Environment.CurrentDirectory + "/log.txt", log + Environment.NewLine);
        }

        public static async Task LogUserActionAsync(CommandContext Context, string log)
        {
            if (Context.Member == null)
            {
                await (LogAsync($"[{DateTime.Now.ToString()}] Log: [DIRECT][U: {Context.User.Id} ({Context.User.Username})]: {log}"));
            }
            else
            {
                await (LogAsync($"[{DateTime.Now.ToString()}] Log: [G: {Context.Guild.Name} ][U: {Context.Member.Id} ({Context.User.Username})]: {log}"));
            }
        }
    }
}
