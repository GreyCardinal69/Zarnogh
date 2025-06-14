using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Zarnogh.Configuration;
using Zarnogh.Modules;
using Zarnogh.Modules.Logging;
using Zarnogh.Services;

namespace Zarnogh
{
    public class BotCore
    {
        public DiscordClient Client { get; private set; }

        public CommandsNextExtension CommandsNext { get; private set; }
        private BotConfig _botConfig;
        private ServiceProvider _services;
        private GuildConfigManager _guildConfigManager;
        private ModuleManager _moduleManager;
        private ZarnoghState _botState;
        private Task _tickLoopTask;
        private CancellationTokenSource _tickLoopCts;
        private GuildEventLoggingService _guildEventLoggingService;

        public event Func<BotCore, DateTimeOffset, Task> TickAsync;

        public async Task InitializeAsync( BotConfig config )
        {
            _botConfig = config;

            _services = new ServiceProvider();
            _services.AddService( _botConfig );

            var discordConfig = new DiscordConfiguration
            {
                Token = _botConfig.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Critical,
                Intents = DiscordIntents.All,
            };

            Client = new DiscordClient( discordConfig );
            _services.AddService( Client );

            Client.UseInteractivity( new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes( 2 ),
            } );

            var commandsNextConfig = new CommandsNextConfiguration
            {
                Services = _services,
                StringPrefixes = [config.Prefix],
                EnableMentionPrefix = false,
                EnableDms = true,
                CaseSensitive = false
            };
            CommandsNext = Client.UseCommandsNext( commandsNextConfig );

            _botState = new ZarnoghState()
            {
                Client = Client,
                CommandsNext = CommandsNext,
                StartUpTime = DateTime.Now,
                BotCore = this
            };
            _services.AddService( _botState );

            _guildConfigManager = new GuildConfigManager( _botConfig, _botState, _services );
            _services.AddService( _guildConfigManager );

            _moduleManager = new ModuleManager( _services, _botConfig, _guildConfigManager, _botState );
            _services.AddService( _moduleManager );

            _guildEventLoggingService = new GuildEventLoggingService( _botConfig, _guildConfigManager, _moduleManager, _botState );
            _services.AddService( _guildEventLoggingService );

            await _moduleManager.DiscoverAndLoadModulesAsync();
            _moduleManager.RegisterModuleCommands( CommandsNext );

            Client.Ready += OnClientReady;
            Client.UnknownEvent += OnUnknownEvent;
            Client.GuildAvailable += OnGuildAvailable;

            CommandsNext.CommandExecuted += OnCommandExecuted;
            CommandsNext.CommandErrored += OnCommandErrored;

            Client.GuildMemberRemoved += _guildEventLoggingService.OnGuildMemberRemoved;
            Client.GuildMemberAdded += _guildEventLoggingService.OnGuildMemberAdded;
            Client.GuildBanRemoved += _guildEventLoggingService.OnGuildBanRemoved;
            Client.GuildBanAdded += _guildEventLoggingService.OnGuildBanAdded;
            Client.GuildRoleDeleted += _guildEventLoggingService.OnGuildRoleDeleted;
            Client.MessagesBulkDeleted += _guildEventLoggingService.OnMessagesBulkDeleted;
            Client.MessageDeleted += _guildEventLoggingService.OnMessageDeleted;
            Client.MessageUpdated += _guildEventLoggingService.OnMessageUpdated;
            Client.MessageCreated += _guildEventLoggingService.OnMessageCreated;
            Client.InviteDeleted += _guildEventLoggingService.OnInviteDeleted;
            Client.InviteCreated += _guildEventLoggingService.OnInviteCreated;
            Client.ChannelCreated += _guildEventLoggingService.OnChannelCreated;
            Client.ChannelDeleted += _guildEventLoggingService.OnChannelDeleted;

            await Client.ConnectAsync();
            StartTickLoop();
        }

        private async Task OnUnknownEvent( DiscordClient sender, UnknownEventArgs args )
        {
            // Unknown event logs seem useless, just ignore them.
            return;
        }

        private async Task HandleGuildIsolationEntriesAsync()
        {
            foreach ( var server in Client.Guilds )
            {
                var profile = await _guildConfigManager.GetOrCreateGuildConfig(server.Key);
                if ( profile.IsolationConfiguration.ActiveIsolationEntries.Count == 0 ) continue;
                var now = DateTime.UtcNow;

                CommandContext fakeContext = await _botState.CreateNewCommandContext( profile.GuildId, profile.BotNotificationsChannel );
                for ( int i = 0; i < profile.IsolationConfiguration.ActiveIsolationEntries.Count; i++ )
                {
                    var entry = profile.IsolationConfiguration.ActiveIsolationEntries[i];
                    if ( now > entry.IsolationReleaseDate )
                    {
                        await fakeContext.TriggerTypingAsync();

                        DiscordMember user  = await fakeContext.Guild.GetMemberAsync( entry.UserId );

                        await user.RevokeRoleAsync( fakeContext.Guild.GetRole( entry.IsolationRoleId ) );

                        if ( entry.ReturnRolesOnRelease )
                        {
                            foreach ( var role in entry.UserRolesUponIsolation )
                            {
                                await user.GrantRoleAsync( fakeContext.Guild.GetRole( role ) );
                            }
                        }

                        profile.IsolationConfiguration.ActiveIsolationEntries.Remove( entry );

                        var channel = fakeContext.Guild.GetChannel( entry.IsolationChannelId );

                        await fakeContext.Channel.SendMessageAsync( $"Released user: {user.Mention} from isolation at channel: {channel.Mention}. The user was isolated for: `{Convert.ToDouble( ( DateTime.UtcNow - entry.IsolationCreationDate ).TotalDays )}` days." );
                        await fakeContext.Channel.SendMessageAsync( $"Were the revoked roles returned? `{entry.ReturnRolesOnRelease}`. The user was isolated for: `{entry.Reason}`." );
                        await _guildConfigManager.SaveGuildConfigAsync( profile );
                    }
                }
            }
        }

        private void StartTickLoop()
        {
            ColorableMessageBuilder msg;

            if ( _botConfig.TickLoopIntervalMilliseconds <= 0 )
            {
                msg = new ColorableMessageBuilder( Console.ForegroundColor )
                    .Append( "[" )
                    .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                    .Append( "] Tick loop interval is zero or negative. Loop will not start." );
                Logger.LogColorableBuilderMessage( msg );
                return;
            }

            _tickLoopCts = new CancellationTokenSource();
            _tickLoopTask = Task.Run( async () =>
            {
                msg = new ColorableMessageBuilder( Console.ForegroundColor )
                    .Append( "[" )
                    .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                    .Append( $"] Started using Task.Run with interval {_botConfig.TickLoopIntervalMilliseconds}ms at {DateTimeOffset.UtcNow}." );
                Logger.LogColorableBuilderMessage( msg );

                try
                {
                    while ( !_tickLoopCts.Token.IsCancellationRequested )
                    {
                        await Task.Delay( _botConfig.TickLoopIntervalMilliseconds, _tickLoopCts.Token );
                        if ( !_tickLoopCts.Token.IsCancellationRequested )
                        {
                            await OnTickAsync( DateTimeOffset.UtcNow );
                        }
                    }
                }
                catch ( TaskCanceledException )
                {
                    msg = new ColorableMessageBuilder( Console.ForegroundColor )
                    .Append( "[" )
                    .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                    .Append( $"] Task.Run loop was canceled." );
                    Logger.LogColorableBuilderMessage( msg );
                }
                catch ( Exception ex )
                {
                    Logger.LogError( $"[TickLoop] CRITICAL ERROR in Task.Run loop: {ex}" );
                }

                msg = new ColorableMessageBuilder( Console.ForegroundColor )
                    .Append( "[" )
                    .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                    .Append( $"] Task.Run loop has stopped" );
                Logger.LogColorableBuilderMessage( msg );
            }, _tickLoopCts.Token );
        }

        private async Task OnTickAsync( DateTimeOffset tickTime )
        {
            ColorableMessageBuilder msg = 
            new ColorableMessageBuilder( Console.ForegroundColor )
                .Append( "[" )
                .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                .Append( $"] Tick at {tickTime}." );
            Logger.LogColorableBuilderMessage( msg );

            await HandleGuildIsolationEntriesAsync();

            if ( TickAsync != null )
            {
                try
                {
                    foreach ( Func<BotCore, DateTimeOffset, Task> handler in TickAsync.GetInvocationList() )
                    {
                        try
                        {
                            await handler( this, tickTime );
                        }
                        catch ( Exception ex )
                        {
                            Logger.LogError( $"[TickLoop] Error in a TickAsync subscriber: {ex}" );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    Logger.LogError( $"[TickLoop] Error invoking TickAsync event: {ex}" );
                }
            }
        }

        private Task OnClientReady( DiscordClient sender, ReadyEventArgs e )
        {
            Logger.LogMessage( $"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}. Operating on {sender.Guilds.Count} servers." );
            return Task.CompletedTask;
        }

        private async Task OnGuildAvailable( DiscordClient sender, GuildCreateEventArgs e )
        {
            await _guildConfigManager.GetOrCreateGuildConfig( e.Guild.Id );
        }

        private Task OnCommandExecuted( CommandsNextExtension sender, CommandExecutionEventArgs e )
        {
            var messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                .Append($"The user \"{e.Context.User.Username}\" successfully executed the command: [")
                .AppendHighlight( $"{e.Command.QualifiedName}", ConsoleColor.Cyan )
                .Append("] in ")
                .AppendHighlight($"#{e.Context.Channel.Name}", ConsoleColor.DarkGreen)
                .Append($" in (")
                .AppendHighlight($"{e.Context.Guild?.Name ?? "Direct Messages"}", ConsoleColor.Cyan)
                .Append(").");

            Logger.LogColorableBuilderMessage( messageBuilder );
            return Task.CompletedTask;
        }

        private async Task OnCommandErrored( CommandsNextExtension sender, CommandErrorEventArgs e )
        {
            if ( e.Exception is DSharpPlus.CommandsNext.Exceptions.ChecksFailedException cfe )
            {
                // this error occurs only when we use RequireModuleEnabled Attribute to check
                // whether a command module is enabled for a server, the check returns false if not
                // which DSharpPlus handles as an exception, in this context it should do nothing except the notify the command caller
                // so we just ignore this here.
                return;
            }

            if ( e.Exception is ArgumentException )
            {
                // User called a command, didn't pass some argument.
                await e.Context.RespondAsync( $"Invalid arguments for command: `{e.Command.QualifiedName}`." );
                return;
            }

            if ( e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException )
            {
                // Command doesnt exist, or a typo, ignore it.
                return;
            }

            Logger.LogError( $"{e.Context.User.Username} tried to execute '{e.Command?.QualifiedName ?? "unknown command"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message}." );
            await e.Context.RespondAsync( $"An error occurred while executing the command. The details have been logged.\n\"{e.Exception}\"." );
            Logger.LogError( $"\n{e.Exception.StackTrace}." );
        }

        public async Task ShutdownAsync()
        {
            if ( Client != null )
            {
                await Client.DisconnectAsync();
                Client.Dispose();
            }
            if ( _tickLoopCts != null )
            {
                await _tickLoopCts.CancelAsync();
                _tickLoopCts.Dispose();
            }
            Environment.Exit( 0 );
        }
    }
}