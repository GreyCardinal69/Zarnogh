using Zarnogh.Modules.Timing;

namespace Zarnogh.Configuration
{
    public class GuildConfig
    {
        public string GuildName { get; set; }
        public ulong GuildId { get; set; }
        public List<string> EnabledModules { get; set; }
        public DateTime ProfileCreationDate { get; init; }
        public bool DeleteBotResponseAfterEraseCommands { get; set; }
        public ulong BotNotificationsChannel { get; set; }
        public List<TimedReminder> TimedReminders { get; set; }
    }
}