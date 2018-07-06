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

        public static string CenterString(string value)
        {
            return String.Format("{0," + ((Console.WindowWidth / 2) + ((value).Length / 2)) + "}", value);
        }

        public static async Task LogAsync(string log)
        {
            Debug.WriteLine(log);
            await System.IO.File.AppendAllTextAsync(Environment.CurrentDirectory + "/log.txt", log + Environment.NewLine);
        }

        public static void Log(string log)
        {
            Debug.WriteLine(log);
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
