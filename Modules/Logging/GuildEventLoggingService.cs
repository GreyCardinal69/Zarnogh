using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Zarnogh.Configuration;
using Zarnogh.Other;
using Zarnogh.Services;
using Utilities = Zarnogh.Other.Utilities;

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

        public async Task OnInviteCreated( DiscordClient sender, InviteCreateEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnInviteCreated )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The invite is**\n{args.Invite}\n\n")
                    .Append( $"**The invite has:**\n`{args.Invite.MaxUses}` max uses, and `{args.Invite.Uses}` total uses.\n\n")
                    .Append( $"**The invite was created by** {args.Invite.Inviter.Mention} at: `{args.Invite.CreatedAt.UtcDateTime}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Created Invite**\n\n\n",
                    Color = DiscordColor.Wheat,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
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
                    .Append( $"**The invite had:**\n`{args.Invite.MaxUses}` max uses, and `{args.Invite.Uses}` total uses.\n\n")
                    .Append( $"**The invite was created by** {args.Invite.Inviter.Mention} at: `{args.Invite.CreatedAt.UtcDateTime}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Deleted Invite**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnGuildRoleDeleted( DiscordClient sender, GuildRoleDeleteEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnGuildRoleDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The role's name was:** `{args.Role.Name}`\n\n")
                    .Append( $"**Was the role mentionable?** `{args.Role.IsMentionable}`\n\n")
                    .Append( $"**The role's color was:** `{args.Role.Color}`\n\n")
                    .Append( $"**The role's id was:** `{args.Role.Id}`\n\n")
                    .Append( $"**The role was created at:** `{args.Role.CreationTimestamp.UtcDateTime}`\n\n")
                    .Append( $"**The role was deleted at:** `{DateTime.UtcNow}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Deleted Member Role**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnMessageDeleted( DiscordClient sender, MessageDeleteEventArgs args )
        {
            if ( args.Message.Timestamp < _botState.StartUpTime )
            {
                return;
            }
            if ( args.Message.Author.IsBot )
            {
                return;
            }

            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.ChannelsExcludedFromLogging.Contains( args.Channel.Id ) ) return;

            if ( profile.LoggingConfiguration.OnMessageDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The message was:**\n {args.Message.Content}\n\n")
                    .Append( $"**The message was deleted at:** {args.Message.Channel.Mention}\n\n")
                    .Append( $"**The message's author's was:** {args.Message.Author.Mention}\n\n")
                    .Append( $"**The message's ID was:** `{args.Message.Id}`\n\n")
                    .Append( $"**The Channel's ID is:** `{args.Channel.Id}`\n\n")
                    .Append( $"Attaching deleted attachements below:");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Deleted Message**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                    Author = new()
                    {
                        IconUrl = args.Message.Author.AvatarUrl,
                        Name = args.Message.Author.Username
                    }
                };

                await channel.SendMessageAsync( embed );

                int z = 0;
                if ( args.Message.Attachments != null )
                {
                    foreach ( var item in args.Message.Attachments )
                    {
                        string savePath = $"{AppDomain.CurrentDomain.BaseDirectory}\\Temp\\image{z}.{GetUrlType(item.Url)}";

                        try
                        {
                            using ( HttpClient client = new HttpClient() )
                            {
                                byte[] imageBytes = await client.GetByteArrayAsync( item.Url );
                                await File.WriteAllBytesAsync( savePath, imageBytes );
                            }
                        }
                        catch ( Exception ex )
                        {
                            Logger.LogError( $"Error downloading image: {ex.Message}" );
                        }
                        z++;
                    }
                }

                var saved = Utilities.GetAllFilesFromFolder( @$"{AppDomain.CurrentDomain.BaseDirectory}\Temp\", false );

                foreach ( var item in saved )
                {
                    using var fs = new FileStream( item, FileMode.Open, FileAccess.Read );
                    var msg = await new DiscordMessageBuilder().AddFile( item, fs ).SendAsync( channel );
                }
                foreach ( var item in saved )
                {
                    File.Delete( item );
                }
            }
            return;
        }

        private string GetUrlType( string url )
        {
            if ( url.Contains( ".jpg" ) || url.Contains( ".jpeg" ) ) return ".jpg";
            if ( url.Contains( ".mp4" ) ) return ".mp4";
            if ( url.Contains( ".mp3" ) ) return ".mp3";
            if ( url.Contains( ".png" ) ) return ".png";
            if ( url.Contains( ".gif" ) ) return ".gif";

            return ".jpg";
        }

        public async Task OnMessageUpdated( DiscordClient sender, MessageUpdateEventArgs args )
        {
            if ( args.Message.Timestamp < _botState.StartUpTime )
            {
                return;
            }
            if ( args.Message.Author.IsBot )
            {
                return;
            }

            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);

            if ( profile.LoggingConfiguration.ChannelsExcludedFromLogging.Contains( args.Channel.Id ) ) return;

            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnMessageUpdated )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The old message was:**\n `{args.MessageBefore.Content}`\n\n")
                    .Append( $"**The new message is:**\n `{args.Message.Content}`\n\n")
                    .Append( $"**Message updated at:** {args.Channel.Mention}\n\n")
                    .Append( $"[Message's Jump Link]({args.Message.JumpLink})\n\n")
                    .Append( $"**The user's ID is:** `{args.Author.Id}`\n")
                    .Append( $"**The message's ID is:** `{args.Message.Id}`\n")
                    .Append( $"**The Channel's ID is:** `{args.Channel.Id}`");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Edited Message**\n\n\n",
                    Color = DiscordColor.Gold,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                    Author = new()
                    {
                        IconUrl = args.Message.Author.AvatarUrl,
                        Name = args.Message.Author.Username
                    }
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnChannelDeleted( DiscordClient sender, ChannelDeleteEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnChannelDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The channel's name was:** `{args.Channel.Name}`\n\n")
                    .Append( $"**Was the channel marked as nsfw?** `{args.Channel.IsNSFW}`\n\n")
                    .Append( $"**The channel's type was:** `{args.Channel.Type}`\n\n")
                    .Append( $"**The channel's topic was:** `{(args.Channel.Topic == null ? "None" : args.Channel.Topic)}`\n\n")
                    .Append( $"**The channel's category was:** `{args.Channel.Parent.Name}`\n\n")
                    .Append( $"**The Channel's ID was:** `{args.Channel.Id}`\n\n")
                    .Append( $"**The channel was created at:** `{args.Channel.CreationTimestamp.UtcDateTime}`");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Deleted Channel**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnChannelCreated( DiscordClient sender, ChannelCreateEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnChannelCreated )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The channel's name is:** `{args.Channel.Name}`\n\n")
                    .Append( $"**Is the channel marked as nsfw?** `{args.Channel.IsNSFW}`\n\n")
                    .Append( $"**The channel's type is:** `{args.Channel.Type}`\n\n")
                    .Append( $"**The channel's topic is:** `{(args.Channel.Topic == null ? "None" : args.Channel.Topic)}`\n\n")
                    .Append( $"**The channel's category is:** `{args.Channel.Parent.Name}`\n\n")
                    .Append( $"**The Channel's ID is:** `{args.Channel.Id}`\n\n")
                    .Append( $"**The channel was created at:** `{args.Channel.CreationTimestamp.UtcDateTime}`");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Created Channel**\n\n\n",
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
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( !profile.LoggingConfiguration.OnMessageCreated )
            {
                return;
            }

            if ( profile.LoggingConfiguration.ChannelsExcludedFromLogging.Contains( args.Channel.Id ) ) return;

            if ( args.Message.Content.Contains( _botState.Client.CurrentUser.Mention ) )
            {
                CommandContext context = await _botState.CreateNewCommandContext( args.Guild.Id, args.Channel.Id );
                await context.RespondAsync( $"I am the spiritual inheritor of 14_P4_21, my prefix is: `.`" );
            }

            if ( args.Author.Id == _botState.Client.CurrentUser.Id || args.Author.Id == _botConfig.OwnerId )
            {
                return;
            }

            DiscordMember user = await args.Guild.GetMemberAsync( args.Message.Author.Id );

            // moderators are allowed to post certain messages that normal members can not.
            Permissions perms = user.Permissions;
            if ( perms.HasPermission( Permissions.Administrator ) ||
                 perms.HasPermission( Permissions.BanMembers ) ||
                 perms.HasPermission( Permissions.KickMembers ) ||
                 perms.HasPermission( Permissions.ManageChannels ) ||
                 perms.HasPermission( Permissions.ManageGuild ) ||
                 perms.HasPermission( Permissions.ManageMessages ) ||
                 perms.HasPermission( Permissions.ManageRoles ) ||
                 perms.HasPermission( Permissions.ManageEmojis ) )
            {
                return;
            }

            foreach ( string link in Constants.ScamLinks )
            {
                if ( args.Message.Content.Contains( link ) )
                {
                    var serverProfile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
                    CommandContext fakeContext = await _botState.CreateNewCommandContext( args.Guild.Id, profile.BotNotificationsChannel );
                    await fakeContext.TriggerTypingAsync();
                    // since the isolation command is called by the bot, we don't file it officially ( not included in the bot's user profile )
                    // isolates at a free or the first isolation channel.

                    var isolationPair = profile.IsolationConfiguration.GetFreeOrFirstIsolationPair();

                    await user.GrantRoleAsync( args.Guild.GetRole( isolationPair.Item2 ) );
                    await fakeContext.RespondAsync( $"Isolated user {user.Mention} at {args.Guild.GetChannel( isolationPair.Item1 ).Mention}. The user's message contained a discord scam link, the link was: `{link}`. " );
                    await fakeContext.RespondAsync( $"Revoked the following roles from the user: {string.Join( ", ", user.Roles.Select( x => x.Mention ).ToArray() )}." );

                    foreach ( DiscordRole role in user.Roles )
                    {
                        await user.RevokeRoleAsync( role );
                    }

                    await args.Message.DeleteAsync();
                }
            }
        }

        public async Task OnMessagesBulkDeleted( DiscordClient sender, MessageBulkDeleteEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.ChannelsExcludedFromLogging.Contains( args.Channel.Id ) ) return;

            if ( profile.LoggingConfiguration.OnMessagesBulkDeleted )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Purged Messages**\n\n\n",
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
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnGuildBanAdded )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                var roles = new string(string.Join( ", ", args.Member.Roles.Select( X => X.Mention ) ).ToArray());
                StringBuilder sb = new StringBuilder()
                    .Append( $"**The banned user was:** {args.Member.Mention}\n\n")
                    .Append( $"**The user's roles were:** {(roles == "" ? "`None`" : roles)}\n\n")
                    .Append( $"**The user joined at:** `{args.Member.JoinedAt.UtcDateTime}`\n\n")
                    .Append( $"**The user's ID is:** `{args.Member.Id}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Banned Member**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                    Author = new()
                    {
                        IconUrl = args.Member.AvatarUrl,
                        Name = args.Member.Username
                    }
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnGuildBanRemoved( DiscordClient sender, GuildBanRemoveEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnGuildBanRemoved )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The unbanned user is:** {args.Member.Mention}\n\n")
                    .Append( $"**The user's ID is:** `{args.Member.Id}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Unbanned Member**\n\n\n",
                    Color = DiscordColor.Wheat,
                    Description = sb.ToString(),
                    Timestamp = DateTime.Now,
                    Author = new()
                    {
                        IconUrl = args.Member.AvatarUrl,
                        Name = args.Member.Username
                    }
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnGuildMemberAdded( DiscordClient sender, GuildMemberAddEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);

            if ( profile.CustomWelcomeMessageEnabled )
            {
                if ( profile.WelcomeConfiguration.RoleId != 0 )
                {
                    await args.Member.GrantRoleAsync( args.Guild.GetRole( profile.WelcomeConfiguration.RoleId ) );
                }
                DiscordChannel main = args.Guild.GetChannel( profile.WelcomeConfiguration.ChannelId );
                await main.SendMessageAsync( profile.WelcomeConfiguration.Content.Replace( "MENTION", $"{args.Member.Mention}" ) );
            }

            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnGuildMemberAdded )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                StringBuilder sb = new StringBuilder()
                    .Append( $"**The user joined at:** `{args.Member.JoinedAt.UtcDateTime}`\n\n")
                    .Append( $"**The user's account was created at:** `{args.Member.CreationTimestamp}`\n\n")
                    .Append( $"**The user's ID is:** `{args.Member.Id}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Joined Member**\n\n\n",
                    Color = DiscordColor.SpringGreen,
                    Description = sb.ToString(),
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = args.Member.Username,
                        IconUrl = args.Member.AvatarUrl
                    },
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }

        public async Task OnGuildMemberRemoved( DiscordClient sender, GuildMemberRemoveEventArgs args )
        {
            var profile = await _guildConfigManager.GetOrCreateGuildConfig(args.Guild.Id);
            if ( !profile.EnabledModules.Contains( "Logging" ) ) return;

            if ( profile.LoggingConfiguration.OnGuildMemberRemoved )
            {
                DiscordChannel channel = args.Guild.GetChannel(profile.EventLoggingChannelId);

                var roles = new string(string.Join( ", ", args.Member.Roles.Select( X => X.Mention ) ).ToArray());
                StringBuilder sb = new StringBuilder()
                    .Append( $"**The user joined at:** `{args.Member.JoinedAt.UtcDateTime}`\n\n")
                    .Append( $"**The user's roles were:** {(roles == "" ? "`None`" : roles)}\n\n")
                    .Append( $"**The user's ID is:** `{args.Member.Id}`\n\n");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "**Reporting On Removed Member**\n\n\n",
                    Color = DiscordColor.Red,
                    Description = sb.ToString(),
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = args.Member.Username,
                        IconUrl = args.Member.AvatarUrl
                    },
                    Timestamp = DateTime.Now,
                };
                await channel.SendMessageAsync( embed );
            }
            return;
        }
    }
}