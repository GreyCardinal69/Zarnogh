using DSharpPlus;
using DSharpPlus.EventArgs;
using Zarnogh.Configuration;

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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public async Task OnMessageCreated( DiscordClient sender, MessageCreateEventArgs args )
        {
            throw new NotImplementedException();
        }

        public async Task OnMessagesBulkDeleted( DiscordClient sender, MessageBulkDeleteEventArgs args )
        {
            throw new NotImplementedException();
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