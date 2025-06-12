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

        [Command( "ServerProfile" )]
        [Description( "Responds with the server's configuration (profile)." )]

        [RequireUserPermissions( DSharpPlus.Permissions.ManageRoles )]
        public async Task Profile( CommandContext ctx )
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

            StringBuilder timedReminders = new StringBuilder();

            foreach ( TimedReminder item in profile.TimedReminders )
            {
                timedReminders.Append( $"{item.Name}: Will go off at: {DateTimeOffset.FromUnixTimeSeconds( item.ExpDate )} / <t:{item.ExpDate}> in Unix.\n\n" );
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
            string reminders = timedReminders.Length > 0 ? timedReminders.ToString() : "None";

            string welcomeRole = joinCfg.RoleId == 0 ? "`Null`" : ctx.Guild.GetRole(joinCfg.RoleId).Mention;
            string welcomeChannel = ctx.Guild.GetChannel(joinCfg.ChannelId).Mention;
            string welcomeMsg = profile.CustomWelcomeMessageEnabled ? $"`\"{joinCfg.Content}\"` at {welcomeChannel}, will give the following role: {welcomeRole}." : "Not Set";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for `{ctx.Guild.Name}`",
                Color = DiscordColor.DarkGreen,
                Description =
                new StringBuilder()
                    .Append(CultureInfo.InvariantCulture, $"Bot notifications are sent to: {notifications}.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Server profile created at: `{profile.ProfileCreationDate}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Bot instructed to delete response message after erase commands: `{profile.DeleteBotResponseAfterEraseCommands}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Enabled command modules: `{enabledModules.ToString()}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"The server has the following Timed Reminders queued:\n `{reminders}`\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Custom Welcome Message: {welcomeMsg}")
                    .ToString(),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = ctx.Client.CurrentUser.AvatarUrl,
                },
                Timestamp = DateTime.Now
            };

            embed = embed.WithThumbnail( ctx.Guild.IconUrl );

            await ctx.RespondAsync( embed );
        }
    }
}