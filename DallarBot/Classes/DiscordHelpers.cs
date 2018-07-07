using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DallarBot.Classes
{
    class DiscordHelpers
    {
        public static bool IsUserAdmin(CommandContext Context)
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

        public static bool IsUserModerator(CommandContext Context)
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

        public static bool IsUserDallarDevTeam(CommandContext Context)
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
            if (Context.Member != null)
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

        public static async Task RespondAsDM(CommandContext Context, DiscordEmbed Response)
        {
            if (Context.Member != null)
            {
                await Context.Member.SendMessageAsync(embed: Response);
            }
            else
            {
                await Context.RespondAsync(embed: Response);
            }
        }

        public static bool HasMinimumStatus(DiscordPresence MemberPrescence, UserStatus MinimumStatus)
        {
            UserStatus MemberStatus = UserStatus.Offline;

            if (MemberPrescence != null) // Apparently offline means null prescence instead of offline prescence
            {
                MemberStatus = MemberPrescence.Status;
            }

            switch (MinimumStatus)
            {
                case UserStatus.Offline:
                    return true;
                case UserStatus.Online:
                    return MemberStatus == UserStatus.Online;
                case UserStatus.Idle:
                case UserStatus.DoNotDisturb:
                    return MemberStatus == UserStatus.Idle || MemberStatus == UserStatus.Online || MemberStatus == UserStatus.DoNotDisturb;
                case UserStatus.Invisible:
                    return MemberStatus != UserStatus.Offline;
                default:
                    return false;
            }
        }

        public static IEnumerable<DiscordMember> GetHumansInContextGuild(CommandContext Context, bool IgnoreContextUser = false, UserStatus MinimumStatus = UserStatus.Offline)
        {
            return Context.Guild.Members.Where(x =>
            {
                if (x.IsBot)
                {
                    return false;
                }

                if (IgnoreContextUser && Context.User.Id == x.Id)
                {
                    return false;
                }

                if (MinimumStatus != UserStatus.Offline && !HasMinimumStatus(x.Presence, MinimumStatus))
                {
                    return false;
                }

                return true;            
            }); 
        }
    }
}
