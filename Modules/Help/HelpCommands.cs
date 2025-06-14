using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Zarnogh.Configuration;
using Zarnogh.Modules;

namespace Zarnogh.Modules.Help
{
    public class HelpCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public HelpCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }
         
    }
}