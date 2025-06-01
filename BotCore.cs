using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Zarnogh.Configuration;
using Zarnogh.Modules;
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
                MinimumLogLevel = LogLevel.Information,
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

            await _moduleManager.DiscoverAndLoadModulesAsync();
            _moduleManager.RegisterModuleCommands( CommandsNext );

            Client.Ready += OnClientReady;
            Client.GuildAvailable += OnGuildAvailable;

            CommandsNext.CommandExecuted += OnCommandExecuted;
            CommandsNext.CommandErrored += OnCommandErrored;

            await Client.ConnectAsync();
            StartTickLoop();
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
            ColorableMessageBuilder msg;

            msg = new ColorableMessageBuilder( Console.ForegroundColor )
                .Append( "[" )
                .AppendHighlight( "TickLoop", ConsoleColor.DarkMagenta )
                .Append( $"] Tick at {tickTime}." );
            Logger.LogColorableBuilderMessage( msg );

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
                _tickLoopCts.Cancel();
                _tickLoopCts.Dispose();
            }
            Environment.Exit( 0 );
        }
    }
}