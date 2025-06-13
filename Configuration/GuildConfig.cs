using DSharpPlus.Entities;
using Zarnogh.Modules.Logging;
using Zarnogh.Modules.Timing;
using Zarnogh.Other;

namespace Zarnogh.Configuration
{
    public class GuildConfig
    {
        public string GuildName { get; init; }
        public ulong GuildId { get; init; }
        public List<string> EnabledModules { get; set; }
        public DateTime ProfileCreationDate { get; init; }
        public bool DeleteBotResponseAfterEraseCommands { get; set; }
        public ulong BotNotificationsChannel { get; set; }
        public List<TimedReminder> TimedReminders { get; set; }
        public UserWelcome WelcomeConfiguration { get; set; }
        public bool CustomWelcomeMessageEnabled { get; set; }
        public ulong EventLoggingChannelId { get; set; }
        public LogConfig LoggingConfiguration { get; set; }
        public List<UserProfile> UserProfiles { get; set; }

        public void AddUserProfile( DiscordUser user )
        {
            if ( UserProfileExists( user.Id ) ) return;

            UserProfile profile = new UserProfile(user.Id, user.CreationTimestamp);
            UserProfiles.Add( profile );
        }

        public bool UserProfileExists( ulong id )
        {
            if ( UserProfiles.Count == 0 ) return false;
            foreach ( var profile in UserProfiles )
            {
                if ( profile.ID == id ) return true;
            }
            return false;
        }
    }
}