using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Zarnogh.Configuration;
using Zarnogh.Services;

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
        [RequireUserPermissions( DSharpPlus.Permissions.ManageGuild )]
        public async Task UpTime( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            var uptime = DateTime.Now - _botState.StartUpTime;
            await ctx.RespondAsync( $"Uptime: {Math.Abs( uptime.Days )} Day(s), {Math.Abs( uptime.Hours )} hour(s), {Math.Abs( uptime.Minutes )} minute(s)." );
        }

        [Command( "DumpConsole" )]
        [Description( "Dumps the last N lines of the console." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageGuild )]
        public async Task DumpConsole( CommandContext ctx, int n )
        {
            await ctx.TriggerTypingAsync();

            if ( n > 1000 )
            {
                await ctx.RespondAsync( "Console cache restricted to 1000 lines only, truncating..." );
                n = 1000;
            }

            await ctx.RespondAsync( $"Dumping console...{Logger.GetConsoleLines( ctx, n )}" );
        }

        [Command( "DumpConsole" )]
        [Description( "Dumps the last 100 lines of the console." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageGuild )]
        public async Task DumpConsole( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"No line count specified, dumping last 20 console lines...{Logger.GetConsoleLines( ctx, 100 )}" );
        }
    }
}
