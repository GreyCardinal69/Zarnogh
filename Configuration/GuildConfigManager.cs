using System.Collections.Concurrent;
using Newtonsoft.Json;
using Zarnogh.Modules;
using Zarnogh.Services;

namespace Zarnogh.Configuration
{
    public class GuildConfigManager
    {
        private readonly ConcurrentDictionary<ulong, GuildConfig> _guildConfigurations = new();
        private const string _configDirectory = "GuildConfigs";
        private readonly BotConfig _globalConfig;
        private readonly ZarnoghState _globalState;
        private readonly ServiceProvider _services;

        public GuildConfigManager( BotConfig globalConfig, ZarnoghState globalState, ServiceProvider services )
        {
            _services = services;
            _globalConfig = globalConfig ?? throw new ArgumentNullException( $"Global Config ({nameof( globalConfig )}) is null in GuildConfigManager constructor" );
            _globalState = globalState ?? throw new ArgumentNullException( $"Global State ({nameof( globalState )}) is null in GuildConfigManager constructor" );
            Directory.CreateDirectory( _configDirectory );
        }

        public async Task<GuildConfig> GetOrCreateGuildConfig( ulong guildId )
        {
            if ( _guildConfigurations.TryGetValue( guildId, out var config ) )
            {
                return config;
            }

            ColorableMessageBuilder messageBuilder;

            var filePath = Path.Combine(_configDirectory, $"{guildId}.json");
            if ( File.Exists( filePath ) )
            {
                try
                {
                    var str = await File.ReadAllTextAsync( filePath );
                    GuildConfig guildConfig = JsonConvert.DeserializeObject<GuildConfig>( str );
                    _guildConfigurations.TryAdd( guildId, guildConfig );

                    messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                        .Append( "Loaded guild config file for guild: [" )
                        .AppendHighlight( $"{guildConfig.GuildName}", ConsoleColor.Cyan )
                        .Append( "," )
                        .AppendHighlight( $"{guildId}", ConsoleColor.DarkGreen )
                        .Append( "]." );

                    Logger.LogColorableBuilderMessage( messageBuilder );

                    ModuleManager moduleMgr = _services.GetService<ModuleManager>();

                    // Server might have timed reminders registered, but the module disabled, should not run them in that case.
                    if ( await moduleMgr.IsModuleEnabledForGuild( "Timed Commands", guildId ) )
                    {
                        if ( guildConfig.TimedReminders.Count > 0 )
                        {
                            foreach ( var item in guildConfig.TimedReminders )
                            {
                                item.Inject( this, _globalState, guildId );
                                _globalState.BotCore.TickAsync += item.BotCoreTickAsync;
                            }
                        }
                    }

                    return config;
                }
                catch ( Exception ex )
                {
                    Logger.LogError( $"Error loading config for guild with ID: {guildId}, exception: {ex.Message}." );
                }
            }

            // config doesn't exist, create manually from DHsarp context.
            var ctx = await _globalState.CreateNewCommandContext( guildId );
            GuildConfig newConfig = new GuildConfig()
            {
                EnabledModules = new List<string>(),
                GuildId = guildId,
                GuildName = ctx.Guild.Name,
                ProfileCreationDate = DateTime.UtcNow,
                DeleteBotResponseAfterEraseCommands = false,
                BotNotificationsChannel = 0,
                TimedReminders = new()
            };

            _guildConfigurations.TryAdd( guildId, newConfig );

            messageBuilder = new ColorableMessageBuilder( Console.ForegroundColor )
                        .Append( "Created a new guild config file for guild: [" )
                        .AppendHighlight( $"{newConfig.GuildName}", ConsoleColor.Cyan )
                        .Append( "," )
                        .AppendHighlight( $"{guildId}", ConsoleColor.DarkGreen )
                        .Append( "]." );

            Logger.LogColorableBuilderMessage( messageBuilder );

            await SaveGuildConfigAsync( newConfig );
            return newConfig;
        }

        public async Task SaveGuildConfigAsync( GuildConfig config )
        {
            if ( config == null )
            {
                Logger.LogError( "Encountered an error while trying to save the guild config. The config is null." );
                return;
            }
            _guildConfigurations[config.GuildId] = config;
            var filePath = Path.Combine(_configDirectory, $"{config.GuildId}.json");
            await File.WriteAllTextAsync( filePath, JsonConvert.SerializeObject( config, Formatting.Indented ) );
        }
    }
}
