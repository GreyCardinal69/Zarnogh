using System.Globalization;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Zarnogh.Configuration;

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
        public async Task ToggleEraseAutoDelete( CommandContext ctx, bool yn )
        {
            await ctx.TriggerTypingAsync();
            var profile = await _guildConfigManager.GetGuildConfig( ctx.Guild.Id);

            profile.DeleteBotResponseAfterEraseCommands = yn;

            await File.WriteAllTextAsync( Path.Combine( "GuildConfigs", $"{ctx.Guild.Id}.json" ), JsonConvert.SerializeObject( profile, Formatting.Indented ) );
            await ctx.RespondAsync( $"`Delete bot response message after erase commands` toggle set to: `{yn}`." );
        }

        [Command( "SetNotificationsChannel" )]
        [Description( "Set's the channel for the bot's notifications." )]
        public async Task SetNotificationsChannel( CommandContext ctx, ulong Id )
        {
            await ctx.TriggerTypingAsync();

            DiscordChannel channel = ctx.Guild.GetChannel( Id );

            if ( channel == null )
            {
                await ctx.RespondAsync( "Invalid channel ID, aborting..." );
                return;
            }

            GuildConfig profile = await _guildConfigManager.GetGuildConfig( ctx.Guild.Id );
            profile.BotNotificationsChannel = channel.Id;

            var newCtx = await _botState.CreateNewCommandContext(ctx.Guild.Id, channel.Id);
            await newCtx.RespondAsync( "<Test Notification>" );

            await File.WriteAllTextAsync( Path.Combine( "GuildConfigs", $"{ctx.Guild.Id}.json" ), JsonConvert.SerializeObject( profile, Formatting.Indented ) );
            await ctx.RespondAsync( $"Bot notifications channel set to: {channel.Mention}." );
        }

        [Command( "ServerProfile" )]
        [Description( "Responds with the server's configuration (profile)." )]
        public async Task Profile( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            GuildConfig profile = await _guildConfigManager.GetGuildConfig( ctx.Guild.Id );

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
                if ( i < _moduleManager.LoadedModules.Count - 1 ) enabledModules.Append( ", " );
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for `{ctx.Guild.Name}`",
                Color = DiscordColor.DarkGreen,
                Description =
                new StringBuilder()
                    .Append(CultureInfo.InvariantCulture, $"Bot notifications are sent to: {(notificationsChannel == null ? "`NOT SET`" : notificationsChannel.Mention)}.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Server profile created at: `{profile.ProfileCreationDate}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Bot instructed to delete response message after erase commands: `{profile.DeleteBotResponseAfterEraseCommands}`.\n\n")
                    .Append(CultureInfo.InvariantCulture, $"Enabled command modules: `{enabledModules.ToString()}`.\n\n")
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