using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
                StartUpTime = DateTime.UtcNow,
            };
            _services.AddService( _botState );

            _guildConfigManager = new GuildConfigManager( _botConfig, _botState );
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
        }

        private Task OnClientReady( DiscordClient sender, ReadyEventArgs e )
        {
            Logger.LogMessage( $"Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}. Operating on {sender.Guilds.Count} servers." );
            return Task.CompletedTask;
        }

        private async Task OnGuildAvailable( DiscordClient sender, GuildCreateEventArgs e )
        {
            await _guildConfigManager.GetGuildConfig( e.Guild.Id );
        }

        private Task OnCommandExecuted( CommandsNextExtension sender, CommandExecutionEventArgs e )
        {
            var messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                .Append($"The user \"{e.Context.User.Username}\" successfully executed command: [")
                .AppendHighlight( $"{e.Command.QualifiedName}", ConsoleColor.Cyan )
                .Append("] in ")
                .AppendHighlight($"#{e.Context.Channel.Name}", ConsoleColor.DarkGreen)
                .Append($" in ({e.Context.Guild?.Name ?? "Direct Messages"}).");

            Logger.LogColorableBuilderMessage( messageBuilder );
            return Task.CompletedTask;
        }

        private async Task OnCommandErrored( CommandsNextExtension sender, CommandErrorEventArgs e )
        {
            Logger.LogError( $"{e.Context.User.Username} tried to execute '{e.Command?.QualifiedName ?? "unknown command"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message}." );

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Command Error")
                .WithDescription($"An error occurred while executing the command. Details have been logged.\n`{e.Exception.Message}`")
                .WithColor(DiscordColor.DarkRed);
            await e.Context.RespondAsync( embed: embed );
        }

        public async Task ShutdownAsync()
        {
            if ( Client != null )
            {
                await Client.DisconnectAsync();
                Client.Dispose();
            }
        }
    }
}
