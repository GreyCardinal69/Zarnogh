using System.Globalization;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

        [Command( "serverprofile" )]
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

            int i = 0;
            foreach ( var module in _moduleManager.LoadedModules )
            {
                enabledModules.Append( module.NameOfModule );
                if ( i < _moduleManager.LoadedModules.Count - 1 ) enabledModules.Append(", ");
                i++;
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