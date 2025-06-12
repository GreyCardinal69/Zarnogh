using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Zarnogh.Configuration;
using Zarnogh.Other;

namespace Zarnogh.Modules.Logging
{
    public class GuildEventLoggingService
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public GuildEventLoggingService( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        public async Task OnInviteDeleted( DiscordClient sender, InviteDeleteEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnInviteDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The invite was**\n{args.Invite}\n\n")
                    .Append( $"**The invite had:**\n{args.Invite.MaxUses} max uses, and {args.Invite.Uses} total uses.\n\n")
                    .Append( $"**The invite was created by** {args.Invite.Inviter.Mention} at: {args.Invite.CreatedAt.UtcDateTime}\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting Deleted Invite**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnGuildRoleUpdated( DiscordClient sender, GuildRoleUpdateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnGuildRoleDeleted( DiscordClient sender, GuildRoleDeleteEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnMessageDeleted( DiscordClient sender, MessageDeleteEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnMessageUpdated( DiscordClient sender, MessageUpdateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnChannelDeleted( DiscordClient sender, ChannelDeleteEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnChannelCreated( DiscordClient sender, ChannelCreateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnInviteCreated( DiscordClient sender, InviteCreateEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnInviteDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The invite is**\n{args.Invite}\n\n")
                    .Append( $"**The invite has:**\n{args.Invite.MaxUses} max uses, and {args.Invite.Uses} total uses.\n\n")
                    .Append( $"**The invite was created by** {args.Invite.Inviter.Mention} at: {args.Invite.CreatedAt.UtcDateTime}\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting Created Invite**\n\n\n",
                    Color = DiscordColor.Wheat,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnMessageCreated( DiscordClient sender, MessageCreateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnMessagesBulkDeleted( DiscordClient sender, MessageBulkDeleteEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnMessagesBulkDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting Purged Messages**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = $"{args.Messages.Count} Messages were deleted in {args.Channel.Mention}.\nAttaching purge archive below.",
                    Timestamp = DateTime.Now,
                };

                StringBuilder sb = new StringBuilder();

                var path = await HtmlArchiveService.ArchiveInputAsync( await _botState.CreateNewCommandContext( args.Guild.Id, args.Channel.Id ), args.Messages, args.Channel );

                using FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read );

                DiscordMessage msg = await new DiscordMessageBuilder()
                            .AddEmbed( embed )
                            .SendAsync( channel );
                // Separate message so that the zip file is below the log message.
                DiscordMessage file = await new DiscordMessageBuilder().AddFile( fs ).SendAsync(channel);
            }
            return;
        }

        public async Task OnGuildBanAdded( DiscordClient sender, GuildBanAddEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnGuildRoleCreated( DiscordClient sender, GuildRoleCreateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnGuildBanRemoved( DiscordClient sender, GuildBanRemoveEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnGuildMemberAdded( DiscordClient sender, GuildMemberAddEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnGuildMemberRemoved( DiscordClient sender, GuildMemberRemoveEventArgs args )
        {
            throw new NotImplementedException();
        }
    }


}