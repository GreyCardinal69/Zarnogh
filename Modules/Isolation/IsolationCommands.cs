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

        [Command( "Isolate" )]
        [Description( "Isolates a user with specified information." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageRoles )]
        public async Task Isolate( CommandContext ctx, ulong userId, string timeLen, bool returnRoles, [RemainingText] string reason )
        {
            await ctx.TriggerTypingAsync();

            if ( ctx.Guild.GetMemberAsync( userId ) == null )
            {
                await ctx.RespondAsync( "Invalid user id." );
                return;
            }

            DiscordMember user = null;

            try
            {
                user = await ctx.Guild.GetMemberAsync( userId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Invalid user Id, aborting..." );
                return;
            }

            var profile = await _guildConfigManager.GetOrCreateGuildConfig(ctx.Guild.Id);

            foreach ( var entry in profile.IsolationConfiguration.ActiveIsolationEntries )
            {
                if ( entry.UserId == userId )
                {
                    await ctx.RespondAsync( $"The user {user.Mention} is already isolated, aborting..." );
                    return;
                }
            }

            if ( profile.IsolationConfiguration.IsolationChannelRolePairs.Count == 0 )
            {
                await ctx.RespondAsync( "No isolation channel-role pairs set, can not isolate, aborting..." );
                return;
            }

            if ( user == null )
            {
                await ctx.RespondAsync( "Invalid user Id, aborting..." );
                return;
            }

            var targetChannelRolePair = profile.IsolationConfiguration.GetFreeOrFirstIsolationPair();

            var now = DateTime.UtcNow;
            var releaseDate = DateTime.UtcNow;

            // we have timeLen as xd, aka x days.
            releaseDate = releaseDate.AddDays( Convert.ToInt32( timeLen[0] ) );

            ulong[] userRoles = new ulong[user.Roles.Count()];
            int i = 0;

            var discordRoles = user.Roles.ToList();

            foreach ( DiscordRole item in discordRoles )
            {
                userRoles[i] = item.Id;
                i++;
                await user.RevokeRoleAsync( item );
            }

            IsolationEntry newEntry = new IsolationEntry()
            {
                IsolationChannelId = targetChannelRolePair.Item1,
                IsolationCreationDate = now,
                IsolationReleaseDate = releaseDate,
                IsolationRoleId = targetChannelRolePair.Item2,
                Reason = reason,
                ReturnRolesOnRelease = returnRoles,
                UserId = userId,
                UserRolesUponIsolation = userRoles
            };

            profile.IsolationConfiguration.ActiveIsolationEntries.Add( newEntry );

            DiscordChannel isolationChannel = ctx.Guild.GetChannel( targetChannelRolePair.Item1 );

            await user.GrantRoleAsync( ctx.Guild.GetRole( targetChannelRolePair.Item2 ) );

            var userProfile = profile.UserProfiles[userId];

            var wereRolesReturn = returnRoles ? "the user's roles were given back upon release" : "the user's roles were not given back upon release";
            userProfile.IsolationEntries.Add( (now, $"For the following reason: `{newEntry.Reason}`, for `{timeLen[0]}` days, at {isolationChannel.Mention}, {wereRolesReturn}, isolated by {ctx.User.Mention}.") );

            await _guildConfigManager.SaveGuildConfigAsync( profile );

            var rolesStr = string.Join( ", ", discordRoles.Select( X => X.Mention ) );
            await ctx.RespondAsync( $"Isolated {user.Mention} at channel: {isolationChannel.Mention}, for `{timeLen[0]}` days. Removed the following roles: {rolesStr}. \nThe user will be released on: `{newEntry.IsolationReleaseDate}` +- 1-2 minutes. Will the revoked roles be given back on release? `{returnRoles}`." );
        }
    }
}