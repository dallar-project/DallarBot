using System;
using System.Diagnostics;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;

namespace DallarBot.Services
{
    public static class LogHandlerExtensions
    {
        // this method writes all of bot's log messages to debug output
        public static void DiscordLogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            string output = $"[{DateTime.Now.ToString()}] Log: [DISCORD][{e.Level}]: {e.Message}";
            Debug.WriteLine(output);
            System.IO.File.AppendAllText(Environment.CurrentDirectory + "/log.txt", output + Environment.NewLine);
        }

        public static void LogUserAction(CommandContext Context, string log)
        {
            if (Context.Member == null)
            {
                Dallar.LogHandlerService.Log($"[{DateTime.Now.ToString()}] Log: [DIRECT][U: {Context.User.Id} ({Context.User.Username})]: {log}");
            }
            else
            {
                Dallar.LogHandlerService.Log($"[{DateTime.Now.ToString()}] Log: [G: {Context.Guild.Name} ][U: {Context.Member.Id} ({Context.User.Username})]: {log}");
            }
        }
    }
}
