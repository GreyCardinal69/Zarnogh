using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Logging
{
    [RequireModuleEnabled( "Logging" )]
    public class LoggingCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public LoggingCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "SetLoggingChannel" )]
        [Description( "Set's the channel for the bot's event logging messages." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task SetNotificationsChannel( CommandContext ctx, ulong Id )
        {
            await ctx.TriggerTypingAsync();

            DiscordChannel channel = ctx.Guild.GetChannel( Id );

            if ( channel == null )
            {
                await ctx.RespondAsync( "Invalid channel ID, aborting..." );
                return;
            }

            GuildConfig profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );
            profile.EventLoggingChannelId = channel.Id;

            var newCtx = await _botState.CreateNewCommandContext(ctx.Guild.Id, channel.Id);
            await newCtx.RespondAsync( "<Test Notification (Logging)>" );

            await _guildConfigManager.SaveGuildConfigAsync( profile );
            await ctx.RespondAsync( $"Bot event logging channel set to: {channel.Mention}." );
        }
    }
}