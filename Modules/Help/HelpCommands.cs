using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;
using Zarnogh.Other;

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

        [Command( "Help" )]
        [Description( "Responds with information on available command modules or module commands." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task HelpCommand( CommandContext ctx, string category = null )
        {
            await ctx.TriggerTypingAsync();
            DiscordEmbedBuilder embed;

            StringBuilder categories =  new StringBuilder();

            for ( int i = 0; i < _moduleManager.LoadedModules.Count; i++ )
            {
                categories.Append( $"`{_moduleManager.LoadedModules[i].NameOfModule}" );
                if ( _moduleManager.LoadedModules[i].IsACoreModule ) categories.Append( " (Global)`" );
                else categories.Append( '`' );
                if ( i < _moduleManager.LoadedModules.Count - 1 ) categories.Append( '\n' );
            }
            categories.Append( '.' );

            if ( string.IsNullOrEmpty( category ) )
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Modules:",
                    Color = Constants.ZarnoghPink,
                    Description =
                    $"Listing command modules. \n Type `{_botConfig.Prefix}help <module>` to get more info on the specified module. \n\n **Modules**\n{categories.ToString()}\n\nDon't include `(Global)` in your help command.",
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        IconUrl = ctx.Member.AvatarUrl,
                    },
                    Timestamp = DateTime.Now,
                };
                await ctx.RespondAsync( embed );
                return;
            }

            // TO DO COMMAND MODULE HELP
        }
    }
}