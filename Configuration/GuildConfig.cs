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
    }
}