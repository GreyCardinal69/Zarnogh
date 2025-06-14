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
        public async Task HelpCommand( CommandContext ctx, [RemainingText] string category = null )
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
                    Timestamp = DateTime.UtcNow,
                };
                await ctx.RespondAsync( embed );
                return;
            }

            switch ( category.ToLowerInvariant() )
            {
                case "isolation commands":
                    StringBuilder isoCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}AddIsolationPair <ChannelID> <RoleID>`: ")
                        .Append($"Adds a channel and an appropriate role for isolation of users in that channel, with that role.\n\n")
                        .Append($"`{_botConfig.Prefix}Isolate <UserID> <Time> <ReturnRolesOnRelease> <Reason>`: ")
                        .Append($"Isolates the user at the first free Channel-Role isolation pair, if all are busy isolates at the first pair. ")
                        .Append($"`<Time>` is given as: `time_d`, where time can be both an integer and a double, f.e `0.5` for half a day. ")
                        .Append($"`<ReturnRolesOnRelease>` is a boolean, if set to true the bot will return the user's roles before the user was isolated.\n\n")
                        .Append($"`{_botConfig.Prefix}ReleaseUser <UserID>`: Releases the given user from isolation.");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Isolation Commands",
                        Color = Constants.ZarnoghPink,
                        Description = isoCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "debug commands":
                    StringBuilder debugCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}UpTime`: Responds with the bot's total uptime.\n\n")
                        .Append($"`{_botConfig.Prefix}ClearConsoleCache`: Clears the bot's console cache.\n\n")
                        .Append($"`{_botConfig.Prefix}DumpConsole <N>`: Responds with the last `N` lines of the console logs, upper limit of 1000.\n\n")
                        .Append($"`{_botConfig.Prefix}DumpConsole`: Responds with the last 20 lines of the console logs.\n\n")
                        .Append($"`{_botConfig.Prefix}Terminate`: Shuts down the bot.\n\n")
                        .Append($"`{_botConfig.Prefix}SetInternalTickLoopInterval`: Changes the bot's TickLoopIntervalMilliseconds config property.");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Debug Commands",
                        Color = Constants.ZarnoghPink,
                        Description = debugCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                        Footer = new DiscordEmbedBuilder.EmbedFooter()
                        {
                            Text = "Debug Commands require owner permissions to execute, except for: `UpTime`."
                        }
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "timed commands":
                    break;
                case "server management":
                    break;
                case "logging":
                    break;
                case "general commands":
                    break;
            }
        }
    }
}