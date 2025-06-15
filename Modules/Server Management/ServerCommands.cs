using System.Globalization;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;
using Zarnogh.Modules.Timing;
using Zarnogh.Other;

namespace Zarnogh.Modules.ServerManagement
{
    public class ServerCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public ServerCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "ToggleEraseAutoDelete" )]
        [Description( "Responds with the server's configuration (profile)." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task ToggleEraseAutoDelete( CommandContext ctx, bool yn )
        {
            await ctx.TriggerTypingAsync();
            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id);

            profile.DeleteBotResponseAfterEraseCommands = yn;

            await _guildConfigManager.SaveGuildConfigAsync( profile );
            await ctx.RespondAsync( $"`Delete bot response message after erase commands` toggle set to: `{yn}`." );
        }

        [Command( "SetNotificationsChannel" )]
        [Description( "Set's the channel for the bot's notifications." )]
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
            profile.BotNotificationsChannel = channel.Id;

            var newCtx = await _botState.CreateNewCommandContext(ctx.Guild.Id, channel.Id);
            await newCtx.RespondAsync( "<Test Notification>" );

            await _guildConfigManager.SaveGuildConfigAsync( profile );
            await ctx.RespondAsync( $"Bot notifications channel set to: {channel.Mention}." );
        }

        [Command( "ToggleCommandModule" )]
        [Description( "Toggles a command module for the server." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task ToggleCommandModule( CommandContext ctx, [RemainingText] string module )
        {
            if ( !_moduleManager.CommandModuleExists( module ) )
            {
                await ctx.RespondAsync( "Invalid command module name, aborting..." );
                return;
            }

            var profile = await _guildConfigManager.GetOrCreateGuildConfig(ctx.Guild.Id);

            if ( profile.EnabledModules.Contains( module ) )
            {
                profile.EnabledModules.Remove( module );
                await ctx.RespondAsync( $"Disabled command module `\"{module}\"` for the server." );
                await _guildConfigManager.SaveGuildConfigAsync( profile );
                return;
            }

            profile.EnabledModules.Add( module );
            await ctx.RespondAsync( $"Enabled command module `\"{module}\"` for the server." );
            await _guildConfigManager.SaveGuildConfigAsync( profile );
            return;
        }

        [Command( "ListCommandModules" )]
        [Description( "Responds with the names of all command modules." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task ListCommandModules( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            StringBuilder sb = new StringBuilder();

            for ( int i = 0; i < _moduleManager.LoadedModules.Count; i++ )
            {
                sb.Append( $"{_moduleManager.LoadedModules[i].NameOfModule}" );
                if ( _moduleManager.LoadedModules[i].IsACoreModule ) sb.Append( " (Global)" );
                if ( i < _moduleManager.LoadedModules.Count - 1 ) sb.Append( ", " );
            }

            await ctx.Channel.SendMessageAsync( $"Listing all command modules: `{sb.ToString()}`." );
        }

        [Command( "DisableCustomWelcome" )]
        [Description( "Disables the custom welcome message for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task DisableCustomWelcome( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            await ctx.RespondAsync( "Custom welcome message disabled." );

            profile.CustomWelcomeMessageEnabled = false;
            await _guildConfigManager.SaveGuildConfigAsync( profile );
        }

        [Command( "SetCustomWelcome" )]
        [Description( "Sets a custom welcome message that the bot will execute for new users." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task SetCustomWelcome( CommandContext ctx, string message, ulong roleId, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            if ( message is null || channelId == 0 )
            {
                await ctx.RespondAsync( "Invalid message or channelId." );
                return;
            }

            await ctx.RespondAsync( "Custom welcome message set." );

            profile.WelcomeConfiguration = new UserWelcome( message, roleId, channelId );
            profile.CustomWelcomeMessageEnabled = true;
            await _guildConfigManager.SaveGuildConfigAsync( profile );
        }

        [Command( "CreateUserProfiles" )]
        [Description( "Creates user profiles for all the users in the server, who don't already have a registered profile." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ModerateMembers )]
        public async Task CreateUserProfiles( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            var users = await ctx.Guild.GetAllMembersAsync();
            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id);

            int n = 0;

            await ctx.Channel.SendMessageAsync( "This process might take a long time, especially for large guilds." );

            foreach ( var user in users )
            {
                if ( profile.UserProfileExists( user.Id ) ) continue;
                profile.AddUserProfile( user );
                n++;
            }

            await ctx.RespondAsync( $"Generated user profiles for `{n}` new users." );
            await _guildConfigManager.SaveGuildConfigAsync( profile );
        }

        [Command( "UserProfile" )]
        [Description( "Responds with the given user's profile." )]

        [RequireUserPermissions( DSharpPlus.Permissions.ModerateMembers )]
        public async Task UserProfile( CommandContext ctx, ulong id )
        {
            await ctx.TriggerTypingAsync();

            var profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            if ( !profile.UserProfileExists( id ) )
            {
                await ctx.RespondAsync( "User profile does not exist, either the id is invalid or the user is not registered, use `CreateUserProfiles` command first." );
                return;
            }

            var user = profile.UserProfiles[id];

            DiscordMember discordUser = null;

            try
            {
                discordUser = await ctx.Guild.GetMemberAsync( id );
            }
            catch ( Exception )
            {
            }

            StringBuilder userNotes = new StringBuilder();
            foreach ( var note in user.Notes )
            {
                userNotes.Append( $"**{note.Key}**: `{note.Value}`\n" );
            }

            int a = 1;
            StringBuilder userBans = new StringBuilder();
            foreach ( var note in user.BanEntries )
            {
                userBans.Append( $"**{a}**: The user was banned at: `{note.Item1}`. {note.Item2}\n" );
                a++;
            }

            a = 1;
            StringBuilder userKicks = new StringBuilder();
            foreach ( var note in user.KickEntries )
            {
                userKicks.Append( $"**{a}**: The user was kicked at: `{note.Item1}`. {note.Item2}\n" );
                a++;
            }

            a = 1;
            StringBuilder userIsolations = new StringBuilder();
            foreach ( var note in user.IsolationEntries )
            {
                userIsolations.Append( $"**{a}**: The user was isolated at: `{note.Item1}`. {note.Item2}\n" );
                a++;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"User Profile for `{user.UserName}`",
                Color = Constants.ZarnoghPink,
                Description =
                new StringBuilder()
                    .Append( $"The user's ID is: `{user.ID}`.\n")
                    .Append( $"The user's creation date is: `{user.CreationDate}`.\n\n")
                    .Append( $"The user has the following notes about him recorded:\n {(userNotes.Length == 0 ? "`None`" : userNotes)}\n\n")
                    .Append( $"The user has the following Isolation records:\n {(userIsolations.Length == 0 ? "`None`" : userIsolations)}\n\n")
                    .Append( $"The user has the following Ban records:\n {(userBans.Length == 0 ? "`None`" : userBans)}\n\n")
                    .Append( $"The user has the following Kick records:\n {(userKicks.Length == 0 ? "`None`" : userKicks)}\n\n")
                    .ToString(),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = discordUser == null ? "" : discordUser.AvatarUrl,
                    Name = user.UserName
                },
                Timestamp = DateTime.Now
            };

            await ctx.RespondAsync( embed );
        }

        [Command( "AddUserNote" )]
        [Description( "Adds a note to the given user's server profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ModerateMembers )]
        public async Task AddUserNote( CommandContext ctx, ulong id, int index, [RemainingText] string note )
        {
            await ctx.TriggerTypingAsync();

            if ( await ctx.Guild.GetMemberAsync( id ) == null )
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            var serverProfile = await _guildConfigManager.GetOrCreateGuildConfig(ctx.Guild.Id);
            var userProfile = serverProfile.UserProfiles[id];

            if ( userProfile.Notes.ContainsKey( index ) )
            {
                await ctx.RespondAsync( $"A user note with the index `{index}` already exists, aborting..." );
                return;
            }

            userProfile.Notes.Add( index, note );
            await _guildConfigManager.SaveGuildConfigAsync( serverProfile );

            await ctx.RespondAsync( "Note added." );
        }

        [Command( "RemoveUserNote" )]
        [Description( "Removes a note from the given user's server profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ModerateMembers )]
        public async Task RemoveUserNote( CommandContext ctx, ulong id, int index )
        {
            await ctx.TriggerTypingAsync();

            if ( await ctx.Guild.GetMemberAsync( id ) == null )
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            var serverProfile = await _guildConfigManager.GetOrCreateGuildConfig(ctx.Guild.Id);
            var userProfile = serverProfile.UserProfiles[id];

            if ( !userProfile.Notes.ContainsKey( index ) )
            {
                await ctx.RespondAsync( $"A user note with the index `{index}` does not exist, aborting..." );
                return;
            }

            userProfile.Notes.Remove( index );
            await _guildConfigManager.SaveGuildConfigAsync( serverProfile );

            await ctx.RespondAsync( "Note removed." );
        }

        [Command( "ServerProfile" )]
        [Description( "Responds with the server's configuration (profile)." )]

        [RequireUserPermissions( DSharpPlus.Permissions.ManageRoles )]
        public async Task ServerProfile( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            GuildConfig profile = await _guildConfigManager.GetOrCreateGuildConfig( ctx.Guild.Id );

            DiscordChannel notificationsChannel = null;

            try
            {
                notificationsChannel = ctx.Guild.GetChannel( profile.BotNotificationsChannel );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Bot notifications channel not set." );
            }

            StringBuilder enabledModules = new StringBuilder();

            foreach ( var module in profile.EnabledModules )
            {
                enabledModules.Append( module );
                enabledModules.Append( ", " );
            }

            for ( int i = 0; i < _moduleManager.LoadedGlobalModules.Count; i++ )
            {
                enabledModules.Append( _moduleManager.LoadedGlobalModules[i].NameOfModule ).Append( " (Global)" );
                if ( i < _moduleManager.LoadedGlobalModules.Count - 1 ) enabledModules.Append( ", " );
            }

            int a = 1;
            StringBuilder timedReminders = new StringBuilder();

            foreach ( TimedReminder item in profile.TimedReminders )
            {
                timedReminders.Append( $"**{a}**: `\"{item.Name}\" Will go off at: {DateTimeOffset.FromUnixTimeSeconds( item.ExpDate )} / <t:{item.ExpDate}> in Unix.\n`\n" );
                a++;
            }

            UserWelcome joinCfg = profile.WelcomeConfiguration;
            DiscordRole joinRole;

            try
            {
                joinRole = ctx.Guild.GetRole( joinCfg.RoleId );
            }
            catch ( NullReferenceException )
            {
            }

            string notifications = notificationsChannel == null ? "`NOT SET`" : notificationsChannel.Mention;
            string reminders = timedReminders.Length > 0 ? timedReminders.ToString()[..^1] : "None";

            string welcomeRole = joinCfg.RoleId == 0 ? "`Null`" : ctx.Guild.GetRole(joinCfg.RoleId).Mention;
            string welcomeChannel = ctx.Guild.GetChannel(joinCfg.ChannelId).Mention;
            string welcomeMsg = profile.CustomWelcomeMessageEnabled ? $"`\"{joinCfg.Content}\"` at {welcomeChannel}, will give the following role: {welcomeRole}." : "Not Set";

            StringBuilder enabledEvents = new StringBuilder();
            if ( profile.LoggingConfiguration.OnInviteDeleted ) enabledEvents.Append( "`OnInviteDeleted` " );
            if ( profile.LoggingConfiguration.OnGuildRoleDeleted ) enabledEvents.Append( "`OnGuildRoleDeleted` " );
            if ( profile.LoggingConfiguration.OnMessageDeleted ) enabledEvents.Append( "`OnMessageDeleted` " );
            if ( profile.LoggingConfiguration.OnMessageUpdated ) enabledEvents.Append( "`OnMessageUpdated` " );
            if ( profile.LoggingConfiguration.OnChannelDeleted ) enabledEvents.Append( "`OnChannelDeleted` " );
            if ( profile.LoggingConfiguration.OnChannelCreated ) enabledEvents.Append( "`OnChannelCreated` " );
            if ( profile.LoggingConfiguration.OnInviteCreated ) enabledEvents.Append( "`OnInviteCreated` " );
            if ( profile.LoggingConfiguration.OnMessageCreated ) enabledEvents.Append( "`OnMessageCreated` " );
            if ( profile.LoggingConfiguration.OnGuildBanAdded ) enabledEvents.Append( "`OnGuildBanAdded` " );
            if ( profile.LoggingConfiguration.OnGuildBanRemoved ) enabledEvents.Append( "`OnGuildBanRemoved` " );
            if ( profile.LoggingConfiguration.OnGuildMemberAdded ) enabledEvents.Append( "`OnGuildMemberAdded` " );
            if ( profile.LoggingConfiguration.OnGuildMemberRemoved ) enabledEvents.Append( "`OnGuildMemberRemoved` " );
            if ( profile.LoggingConfiguration.OnMessagesBulkDeleted ) enabledEvents.Append( "`OnMessagesBulkDeleted`" );

            StringBuilder excludedChannels = new StringBuilder();

            var exclusions = profile.LoggingConfiguration.ChannelsExcludedFromLogging;
            for ( int i = 0; i < exclusions.Count; i++ )
            {
                excludedChannels.Append( ctx.Guild.GetChannel( exclusions[i] ).Mention );
                excludedChannels.Append( ' ' );
            }

            string logNotifs = profile.EnabledModules.Contains("Logging") ? ctx.Guild.GetChannel(profile.EventLoggingChannelId).Mention : "`Logging Module not enabled`";

            int activeIsolations = profile.IsolationConfiguration.ActiveIsolationEntries.Count;

            a = 1;
            StringBuilder isolationPairs = new StringBuilder();
            foreach ( var pair in profile.IsolationConfiguration.IsolationChannelRolePairs )
            {
                isolationPairs.Append( $"**{a}**: {ctx.Guild.GetChannel( pair.Key ).Mention} - {ctx.Guild.GetRole( pair.Value ).Mention}\n" );
                a++;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for `{ctx.Guild.Name}`",
                Color = Constants.ZarnoghPink,
                Description =
                new StringBuilder()
                    .Append(CultureInfo.InvariantCulture, $"Bot notifications are sent to: {notifications}.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Bot server event logs are sent to: {logNotifs}.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Server profile created at: `{profile.ProfileCreationDate}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Bot instructed to delete response message after erase commands: `{profile.DeleteBotResponseAfterEraseCommands}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Enabled command modules: `{enabledModules.ToString()}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The server has the following Timed Reminders queued:\n {reminders}\n")
                    .Append(CultureInfo.InvariantCulture, $"Custom Welcome Message: {welcomeMsg}\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The following events are enabled for logging: {(enabledEvents.Length == 0 ? "`None`" : enabledEvents)}\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The following channels are excluded from logging: {(exclusions.Count == 0 ? "`None`" : excludedChannels)}\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The server has `{profile.UserProfiles.Count}` user profiles registered, current member count is: `{ctx.Guild.MemberCount}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The server has the following isolation Channel-Role pairs:\n {(isolationPairs.Length == 0 ? "`None`" : isolationPairs)}\n")
                    .Append(CultureInfo.InvariantCulture, $"The server has `{activeIsolations}` active isolation entries.")
                    .ToString(),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = ctx.Client.CurrentUser.AvatarUrl,
                },
                Timestamp = DateTime.Now
            };

            await ctx.RespondAsync( embed.WithThumbnail( ctx.Guild.IconUrl ) );
        }
    }
}