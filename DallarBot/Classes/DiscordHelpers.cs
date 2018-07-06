using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DallarBot.Classes
{
    class DiscordHelpers
    {
        static bool IsUserAdmin(CommandContext Context)
        {
            if (Context.Member == null)
            {
                return false;
            }

            if (Context.Member.IsOwner)
            {
                return true;
            }


            foreach (DiscordRole Role in Context.Member.Roles)
            {
                if (Role.Permissions.HasPermission(Permissions.Administrator))
                {
                    return true;
                }

                if (Role.Name == "Admin" || Role.Name == "Administrators")
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsUserModerator(CommandContext Context)
        {
            if (Context.Member == null)
            {
                return false;
            }

            foreach (DiscordRole Role in Context.Member.Roles)
            {
                if (Role.Name == "Mod" || Role.Name == "Moderator" || Role.Name == "Moderators")
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsUserDallarDevTeam(CommandContext Context)
        {
            if (Context.Member == null)
            {
                return false;
            }

            foreach (DiscordRole Role in Context.Member.Roles)
            {
                if (Role.Name == "Dallar Dev Team")
                {
                    return true;
                }
            }

            return false;
        }

        public static void DeleteNonPrivateMessage(CommandContext Context)
        {
            if (Context.Member == null)
            {
                Context.Channel.DeleteMessageAsync(Context.Message);
            }
        }

        public static async Task RespondAsDM(CommandContext Context, string Response)
        {
            if (Context.Member != null)
            {
                await Context.Member.SendMessageAsync(Response);
            }
            else
            {
                await Context.RespondAsync(Response);
            }
        }
    }
}
