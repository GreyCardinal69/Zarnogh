using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Timing
{
    public class TimingCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public TimingCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "Pingee" )]
        [Description( "Checks the bot's responsiveness." )]
        public async Task Pingee( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Ping: {ctx.Client.Ping}ms." );
        }
    }
}