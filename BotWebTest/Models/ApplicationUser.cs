using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dallar;
using Microsoft.AspNetCore.Identity;

namespace BotWebTest.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public bool AddedToTwitchChannel { get; set; }

        public string DiscordAccountId { get; set; }
        public string DiscordUserName { get; set; }

        public string TwitchAccountId { get; set; }
        public string TwitchChannel { get; set; }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(DiscordAccountId))
                {
                    return DiscordUserName;
                }
                else if (!string.IsNullOrEmpty(TwitchAccountId))
                {
                    return TwitchChannel;
                }
                else
                {
                    return null;
                }
            }
        }

        public DallarAccount DallarAccount()
        {
            DallarAccount acc = new DallarAccount();
            if (!string.IsNullOrEmpty(DiscordAccountId))
            {
                acc.AccountId = DiscordAccountId;
                acc.AccountPrefix = "";
            }
            else if (!string.IsNullOrEmpty(TwitchAccountId))
            {
                acc.AccountId = TwitchAccountId;
                acc.AccountPrefix = "twitch_"; //@TODO: Somehow not hardcode
            }
            else
            {
                return null;
            }

            return acc;
        }

        public DallarAccount DallarAccount(string forceProvider)
        {
            DallarAccount acc = new DallarAccount();

            if (forceProvider == "Discord")
            {
                acc.AccountId = DiscordAccountId;
                acc.AccountPrefix = "";
                return acc;
            }
            else if (forceProvider == "Twitch")
            {
                acc.AccountId = TwitchAccountId;
                acc.AccountPrefix = "twitch_";
                return acc;
            }

            return null;
        }
    }
}
