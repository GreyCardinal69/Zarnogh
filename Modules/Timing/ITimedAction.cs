using Newtonsoft.Json;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Timing
{
    public interface ITimedAction
    {
        [JsonIgnore] ZarnoghState BotState { get; set; }
        [JsonIgnore] GuildConfigManager GuildConfigManager { get; set; }
        ulong GuildId { get; set; }
        void Inject( GuildConfigManager mgr, ZarnoghState state, ulong guildId );
        Task BotCoreTickAsync( BotCore state, DateTimeOffset fireDate );
    }
}