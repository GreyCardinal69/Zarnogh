using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Isolation
{
    public class IsolationCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public IsolationCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "AddIsolationPair" )]
        [Description( "Adds a channel and an appropriate role for isolation of users in that channel, with that role." )]
        public async Task PingCommand( CommandContext ctx, ulong channelId, ulong roleId )
        {
            await ctx.TriggerTypingAsync();

            DiscordChannel channel;
            DiscordRole role;

            try
            {
                channel = ctx.Guild.GetChannel( channelId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Invalid channel id, aborting..." );
                return;
            }

            try
            {
                role = ctx.Guild.GetRole( roleId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Invalid role id, aborting..." );
                return;
            }

            if ( channel == null || role == null )
            {
                await ctx.RespondAsync( "Invalid channel or role id, aborting..." );
                return;
            }

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id);

            var isolationConfig = profile.IsolationConfiguration;
            if ( isolationConfig.IsolationChannelRolePairs.ContainsKey( channelId ) && isolationConfig.IsolationChannelRolePairs.ContainsValue( roleId ) )
            {
                await ctx.RespondAsync( $"An isolation channel-role pair for: {channel.Mention}-{role.Mention} already exists, aborting..." );
                return;
            }

            profile.IsolationConfiguration.IsolationChannelRolePairs.Add( channelId, roleId );
            await _guildConfigManager.SaveGuildConfigAsync( profile );
            await ctx.RespondAsync( $"An isolation channel-role pair for: {channel.Mention}-{role.Mention} has been added." );
        }
    }
}