using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Timing
{
    [RequireModuleEnabled( "Timed Commands" )]
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

        [Command( "AddTimedReminder" )]
        [Description( "Adds a timed reminder for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task AddTimedReminder( CommandContext ctx, string name, string content, bool repeat, string dateType, string date )
        {
            await ctx.TriggerTypingAsync();
            GuildConfig cfg = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            name = name.Replace( '_', ' ' );

            for ( int i = 0; i < cfg.TimedReminders.Count; i++ )
            {
                if ( string.Equals( name, cfg.TimedReminders[i].Name, StringComparison.Ordinal ) )
                {
                    await ctx.RespondAsync( $"Timed reminder with ID: \"{name}\" already exists." );
                    return;
                }
            }

            TimedReminder reminder = new TimedReminder( name, content.Replace( '_', ' ' ), repeat, dateType, date );
            reminder.Inject( _guildConfigManager, _botState, ctx.Guild.Id );

            _botState.BotCore.TickAsync += reminder.BotCoreTickAsync;

            cfg.TimedReminders.Add( reminder );
            await ctx.RespondAsync( $"Timed Reminder: `{name}` successfully added.\nThe reminder will go off at: <t:{reminder.ExpDate}>." );
            await _guildConfigManager.SaveGuildConfigAsync( cfg );
        }

        [Command( "ListTimedReminders" )]
        [Description( "Lists all timed reminders registered for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task ListTimedReminders( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            if ( profile.TimedReminders.Count <= 0 )
            {
                await ctx.RespondAsync( "No timed reminders registered." );
                return;
            }

            StringBuilder sb = new StringBuilder("```");

            int i = 1;
            foreach ( TimedReminder item in profile.TimedReminders )
            {
                sb.Append( $"Reminder #{i}:\nName ( ID format ): \t{item.Name.Replace( ' ', '_' )}\n" )
                  .Append( $"Content: \t{item.Content}\n" )
                  .Append( $"The Reminder will go off at: \t<t:{item.ExpDate}>\n\n" );
                i++;
            }
            sb.Append( "```" );
            await ctx.RespondAsync( sb.ToString() );
        }

        [Command( "PurgeTimedReminders" )]
        [Description( "Removes all registered timed reminders for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task PurgeTimedReminders( CommandContext ctx, string name )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            if ( profile.TimedReminders.Count <= 0 )
            {
                await ctx.RespondAsync( "No timed reminders registered, aborting..." );
                return;
            }

            profile.TimedReminders.Clear();
            await ctx.RespondAsync( $"All timed reminders successfully removed." );
            await _guildConfigManager.SaveGuildConfigAsync( profile );
        }

        [Command( "RemoveTimedReminder" )]
        [Description( "Removes a registered timed reminder for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task RemoveTimedReminder( CommandContext ctx, string name )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            if ( profile.TimedReminders.Count <= 0 )
            {
                await ctx.RespondAsync( "No timed reminders registered, aborting..." );
                return;
            }

            var clean = name.Replace( '_', ' ' );

            for ( int i = 0; i < profile.TimedReminders.Count; i++ )
            {
                if ( string.Equals( profile.TimedReminders[i].Name, clean, StringComparison.Ordinal ) )
                {
                    profile.TimedReminders.RemoveAt( i );
                    await ctx.RespondAsync( $"Timed Reminder: `{clean}` successfully removed." );
                    await _guildConfigManager.SaveGuildConfigAsync( profile );
                    return;
                }
            }
            await ctx.RespondAsync( $"Timed reminder with ID: {clean} not found." );
        }
    }
}