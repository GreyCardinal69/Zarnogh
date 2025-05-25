using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
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

        [Command( "ping" )]
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
        [Description( "Responds with the total up time of the current bot instance in days, hours and minutes." )]
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

            try
            {
                // count amount of messages + 1 message, which is '.erase ...'
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync( count + 1 );
                await ctx.Channel.DeleteMessagesAsync( messages );
                var response = await ctx.RespondAsync( $"Erased: {count} messages, executed by {ctx.User.Mention}." );

                // 7 second delay so that the response can be seen for a short while.
                await Task.Delay( 7000 );

                var guildConfig = await _guildConfigManager.GetGuildConfig(ctx.Guild.Id);

                if ( guildConfig.DeleteBotResponseAfterEraseCommands )
                {
                    var messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                        .Append( "Auto-deleted 'erase' command response in: [" )
                        .AppendHighlight( $"{ctx.Guild.Name}", ConsoleColor.Cyan )
                        .Append( ",")
                        .AppendHighlight($"{ctx.Guild.Id}", ConsoleColor.DarkGreen)
                        .Append("] per server configuration.");

                    Logger.LogColorableBuilderMessage( messageBuilder );
                    await ctx.Channel.DeleteMessageAsync( response );
                }
            }
            catch ( BadRequestException )
            {
                await ctx.RespondAsync( $"Failed to erase messages. The messages are older than 14 days, use \"{_botConfig.Prefix}EraseAggressive\" command instead." );
            }
        }
    }
}