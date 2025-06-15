using DSharpPlus.SlashCommands;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.SlashCommands
{
    internal class SlashCommands : ApplicationCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public SlashCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        // Don't feel like slash commands are required. They are also a pain to work with.
        // Regardless the module exists.

        [SlashCommand( "Ping", "Checks the bot's responsiveness." )]
        public async Task Ping( InteractionContext ctx )
        {
            await ctx.CreateResponseAsync( $"Ping: {ctx.Client.Ping}ms." );
        }
    }
}