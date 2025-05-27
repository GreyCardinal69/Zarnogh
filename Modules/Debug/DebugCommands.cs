using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Debug
{
    public class DebugCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public DebugCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "UpTime" )]
        [Description( "Displays the bot's current operational uptime." )]
        public async Task UpTime( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            var uptime = DateTime.Now - _botState.StartUpTime;
            await ctx.RespondAsync( $"Uptime: {Math.Abs( uptime.Days )} Day(s), {Math.Abs( uptime.Hours )} hour(s), {Math.Abs( uptime.Minutes )} minute(s)." );
        }
    }
}
