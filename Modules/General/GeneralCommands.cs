using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Zarnogh.Configuration;
using Zarnogh.Services;

namespace Zarnogh.Modules.General
{
    // This is a global command module, always enabled for all servers
    // Checks for whether the module is enabled for a server are omitted.
    public class GeneralCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public GeneralCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "Ping" )]
        [Description( "Checks the bot's responsiveness." )]
        public async Task PingCommand( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Ping: {ctx.Client.Ping}ms." );
        }

        [Command( "BotTalk" )]
        [Description( "Command for talking as the bot." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task BotTalk( CommandContext ctx, ulong guildId, ulong channelId, ulong threadId, bool thread, params string[] rest )
        {
            await ctx.TriggerTypingAsync();
            CommandContext fakeContext = await _botState.CreateNewCommandContext( guildId, channelId );

            if ( !thread )
            {
                await fakeContext.RespondAsync( string.Join( " ", rest ) );
            }
            else
            {
                DiscordChannel channel = fakeContext.Guild.GetChannel( channelId );

                if ( channel == null )
                {
                    await ctx.RespondAsync( "Invalid thread ID." );
                    return;
                }

                foreach ( DiscordThreadChannel item in channel.Threads )
                {
                    if ( item.Id == threadId ) await fakeContext.RespondAsync( string.Join( " ", rest ) );
                }
            }
        }

        [Command( "Kick" )]
        [Description( "Kicks a user from the server, with an optional reason." )]
        [RequireUserPermissions( DSharpPlus.Permissions.KickMembers )]
        public async Task Kick( CommandContext ctx, ulong userId, string reason = "" )
        {
            await ctx.TriggerTypingAsync();
            DiscordMember member;

            try
            {
                member = await ctx.Guild.GetMemberAsync( userId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Invalid user ID, aborting..." );
                return;
            }

            await ctx.RespondAsync( $"Are you sure you want to kick {member.Mention}? Respond with \"yes\" to confirm." );

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals( xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase ),
                TimeSpan.FromSeconds( 20 )
            );

            if ( !msg.TimedOut && msg.Result.Author.Id == ctx.User.Id )
            {
                await member.RemoveAsync();
                await ctx.RespondAsync( $"{member.Mention} has been kicked from the server. Reason: {reason ?? "No reason provided"}." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "Ban" )]
        [Description( "Bans a specified user from the server, with an option to delete last X messages." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Ban( CommandContext ctx, ulong userId, int deleteAmount = 0, string reason = "" )
        {
            await ctx.TriggerTypingAsync();
            DiscordMember member;

            try
            {
                member = await ctx.Guild.GetMemberAsync( userId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Invalid user ID, aborting..." );
                return;
            }

            await ctx.RespondAsync( $"Are you sure you want to ban {member.Mention}? Respond with yes to confirm." );

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals( xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase ),
                TimeSpan.FromSeconds( 20 )
            );

            if ( !msg.TimedOut && msg.Result.Author.Id == ctx.User.Id )
            {
                member = await ctx.Guild.GetMemberAsync( userId );

                await ctx.Guild.BanMemberAsync( userId, deleteAmount, reason );
                await ctx.RespondAsync( $"Banned {member.Mention}, deleted last {deleteAmount} messages with \"{reason}\" as reason for the ban." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "Unban" )]
        [Description( "Unbans a user." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Unban( CommandContext ctx, ulong userId )
        {
            await ctx.TriggerTypingAsync();

            DiscordBan ban;

            try
            {
                ban = await ctx.Guild.GetBanAsync( userId );
            }
            catch ( NotFoundException )
            {
                var member = await ctx.Guild.GetMemberAsync( userId );
                await ctx.RespondAsync( $"Member {member.Mention} is not banned, aborting..." );
                return;
            }

            await ctx.Guild.UnbanMemberAsync( userId );
            await ctx.RespondAsync( $"Unbanned user: {ban.User.Mention}." );
        }

        [Command( "SetStatus" )]
        [Description( "Sets the bot's status." )]
        [RequireOwner]
        public async Task SetActivity( CommandContext ctx, int type, [RemainingText] string status )
        {
            await ctx.TriggerTypingAsync();

            Logger.LogWarning( "A new activity status has been set for this bot instance." );

            DiscordActivity activity = new DiscordActivity();
            DiscordClient discord = ctx.Client;
            activity.Name = status;
            activity.ActivityType = ActivityType.Watching;

            await ctx.RespondAsync( $"New activity status set to: \"Watching {status}\"." );

            // Offline = 0,
            // Online = 1,
            // Idle = 2,
            // DoNotDisturb = 4,
            // Invisible = 5
            await discord.UpdateStatusAsync( activity, (UserStatus)type, DateTimeOffset.UtcNow );
        }

        [Command( "UpTime" )]
        [Description( "Displays the bot's current operational uptime." )]
        public async Task UpTime( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            var uptime = DateTime.Now - _botState.StartUpTime;
            await ctx.RespondAsync( $"Uptime: {Math.Abs( uptime.Days )} Day(s), {Math.Abs( uptime.Hours )} hour(s), {Math.Abs( uptime.Minutes )} minute(s)." );
        }

        [Command( "Erase" )]
        [Description( "Deletes set amount of messages if possible." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Erase( CommandContext ctx, int count )
        {
            await ctx.TriggerTypingAsync();
            if ( count <= 0 )
            {
                await ctx.RespondAsync( "Invalid number of messages to delete, should be at least 1." );
                return;
            }

            try
            {
                // count amount of messages + 1 message, which is '.erase ...'
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync( count + 1 );
                await ctx.Channel.DeleteMessagesAsync( messages );
                var response = await ctx.RespondAsync( $"Erased: {count} messages, executed by {ctx.User.Mention}." );

                // 7 second delay so that the response can be seen for a short while.
                await Task.Delay( 7000 );
                await TryDeleteEraseResponseMessage( ctx, response, ctx.Guild.Id, "Erase" );
            }
            catch ( BadRequestException )
            {
                await ctx.RespondAsync( $"Could not bulk delete messages older than 14 days. To delete these messages use \"{_botConfig.Prefix}EraseAggressive\" command." );
            }
        }

        [Command( "EraseFromTo" )]
        [Description( "Deletes messages within a specified range, defined by a start and end message ID (both inclusive)." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseFromTo( CommandContext ctx, ulong fromId, ulong toId )
        {
            await ctx.TriggerTypingAsync();

            DiscordMessage fromMessage;
            DiscordMessage toMessage;

            if ( fromId == toId )
            {
                await ctx.RespondAsync( "Identical message IDs, aborting..." );
                return;
            }

            try
            {
                fromMessage = await ctx.Channel.GetMessageAsync( fromId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "`From` message not found, invalid ID." );
                return;
            }

            try
            {
                toMessage = await ctx.Channel.GetMessageAsync( toId );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "`To` message not found, invalid ID." );
                return;
            }

            if ( fromMessage.Timestamp > toMessage.Timestamp )
            {
                (fromMessage, toMessage) = (toMessage, fromMessage);
                await ctx.RespondAsync( "`From` message older than `To` message, wrong order, reordered." );
            }

            List<DiscordMessage> messagesToDelete = new List<DiscordMessage>() { fromMessage, toMessage };

            ulong currentPivotId = toMessage.Id;
            bool foundStartMessage = false;
            var twoWeeks = DateTimeOffset.UtcNow.AddDays(-14);

            while ( !foundStartMessage )
            {
                var fetchedBatch = await ctx.Channel.GetMessagesBeforeAsync(currentPivotId);
                if ( !fetchedBatch.Any() ) break;

                foreach ( var msg in fetchedBatch )
                {
                    if ( msg.Timestamp < fromMessage.Timestamp )
                    {
                        foundStartMessage = true;
                        break;
                    }

                    if ( msg.Timestamp >= fromMessage.Timestamp )
                    {
                        messagesToDelete.Add( msg );
                    }

                    if ( msg.Id == fromMessage.Id )
                    {
                        foundStartMessage = true;
                        break;
                    }
                }

                currentPivotId = fetchedBatch[fetchedBatch.Count - 1].Id;
            }

            var distinctMessages = messagesToDelete
            .DistinctBy(m => m.Id)
            .ToList();

            if ( distinctMessages.Count == 0 )
            {
                await ctx.RespondAsync( "No eligible messages found within the specified range and 14-day limit, aborting..." );
                return;
            }

            var count = distinctMessages.Count;

            foreach ( var msg in distinctMessages )
            {
                await ctx.Channel.DeleteMessageAsync( msg );
            }

            var response = await ctx.RespondAsync( $"Erased: {count} messages, executed by {ctx.User.Mention}." );

            // 7 second delay so that the response can be seen for a short while.
            await Task.Delay( 7000 );
            await TryDeleteEraseResponseMessage( ctx, response, ctx.Guild.Id, "EraseFromTo" );
        }

        [Command( "EraseAggressive" )]
        [Description( "Deletes set amount of messages if possible, can delete messages older than 2 weeeks." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseAggressive( CommandContext ctx, int count )
        {
            await ctx.TriggerTypingAsync();
            if ( count <= 0 )
            {
                await ctx.RespondAsync( "Invalid number of messages to delete, should be at least 1." );
                return;
            }

            try
            {
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync( count );
                foreach ( DiscordMessage item in messages )
                {
                    await ctx.Channel.DeleteMessageAsync( item );
                }
                var response = await ctx.RespondAsync( $"Erased: {count} messages, executed by {ctx.User.Mention}." );

                // 7 second delay so that the response can be seen for a short while.
                await Task.Delay( 7000 );
                await TryDeleteEraseResponseMessage( ctx, response, ctx.Guild.Id, "EraseAggressive" );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Failed to erase targeted messages, aborting..." );
            }
        }

        private async Task TryDeleteEraseResponseMessage( CommandContext ctx, DiscordMessage msg, ulong guildId, string command )
        {
            var guildConfig = await _guildConfigManager.GetGuildConfig(guildId);

            if ( guildConfig.DeleteBotResponseAfterEraseCommands )
            {
                var messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                        .Append( $"Auto-deleted '{command}' command response in: [" )
                        .AppendHighlight( $"{guildConfig.GuildName}", ConsoleColor.Cyan )
                        .Append( ",")
                        .AppendHighlight($"{guildId}", ConsoleColor.DarkGreen)
                        .Append("] per server configuration.");

                Logger.LogColorableBuilderMessage( messageBuilder );
                await ctx.Channel.DeleteMessageAsync( msg );
            }
        }
    }
}