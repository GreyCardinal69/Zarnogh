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
    }
}