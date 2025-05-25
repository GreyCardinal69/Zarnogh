using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.General
{
    // This is a global command module, always enabled for all servers
    // Checks for whether the module is enabled for the server are omitted.
    public class GeneralCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public GeneralCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "ping" )]
        [Description( "Checks the bot's responsiveness." )]
        public async Task PingCommand( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Ping: {ctx.Client.Ping}ms." );
        }
    }
}
