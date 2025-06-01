using Zarnogh.Configuration;

namespace Zarnogh.Modules.Timing
{
    public interface ITimedAction
    {
        ZarnoghState BotState { get; set; }
        GuildConfigManager GuildConfigManager { get; set; }
        ulong GuildId { get; set; }
        void Inject( GuildConfigManager mgr, ZarnoghState state, ulong guildId );
        Task BotCoreTickAsync( BotCore state, DateTimeOffset fireDate );
    }
}