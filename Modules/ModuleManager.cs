using System.Reflection;
using DSharpPlus.CommandsNext;
using Zarnogh.Configuration;
using Zarnogh.Services;

namespace Zarnogh.Modules
{
    public class ModuleManager
    {
        private readonly List<IBotModule> _loadedModules = new List<IBotModule>();
        private readonly ServiceProvider _services;
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;

        public IReadOnlyList<IBotModule> LoadedModules => _loadedModules.AsReadOnly();

        public ModuleManager( ServiceProvider services, BotConfig botConfig, GuildConfigManager guildConfigManager, ZarnoghState state )
        {
            _botState = state ?? throw new ArgumentNullException( $"Bot state ({nameof( _botState )}) is null in ModuleManager constructor" );
            _services = services ?? throw new ArgumentNullException( $"Dependancy injector services ({nameof( services )}) is null in ModuleManager constructor" );
            _botConfig = botConfig ?? throw new ArgumentNullException( $"Global bot config ({nameof( botConfig )}) is null in ModuleManager constructor" );
            _guildConfigManager = guildConfigManager ?? throw new ArgumentNullException( $"Guild Config Manager ({nameof( guildConfigManager )}) is null in ModuleManager constructor" );
        }

        public async Task DiscoverAndLoadModulesAsync( Assembly assemblyToScan = null )
        {
            assemblyToScan ??= Assembly.GetEntryAssembly();

            var moduleTypes = assemblyToScan.GetTypes()
            .Where(t => typeof(IBotModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach ( var type in moduleTypes )
            {
                Logger.LogMessage( "\nAttempting to load command modules..." );
                try
                {
                    if ( Activator.CreateInstance( type ) is IBotModule moduleInstance )
                    {
                        if ( moduleInstance.IsACoreModule || _botConfig.DefaultGlobalModules.Contains( moduleInstance.NameOfModule ) )
                        {
                            await moduleInstance.InitializeAsync( _services );
                            _loadedModules.Add( moduleInstance );
                            Logger.LogMessage( $"Loaded module: {moduleInstance.NameOfModule}." );
                        }
                        else
                        {
                            Logger.LogMessage( $"Skipped module (globally disabled): {moduleInstance.NameOfModule}" );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    Logger.LogError( $"Failed to load module {type.FullName}: {ex.Message}" );
                }
            }
            Logger.LogMessage( "Module loading complete." );
        }

        public void RegisterModuleCommands( CommandsNextExtension commandsNext )
        {
            Logger.LogMessage( "\nAttempting to register commands for loaded modules..." );
            foreach ( var module in _loadedModules )
            {
                module.RegisterCommands( commandsNext, _services );
                Logger.LogMessage( $"Registered commands for module: {module.NameOfModule}" );
            }
            Logger.LogMessage( "Finished registering commands for loaded modules.\n" );
        }

        public bool IsModuleEnabledForGuild( string moduleName, ulong guildId )
        {
            if ( _botConfig.DefaultGlobalModules.Contains( moduleName ) ) return true;
            var guildConfig = _guildConfigManager.GetGuildConfig(guildId).Result;
            return guildConfig.EnabledModules.Contains( moduleName, StringComparer.OrdinalIgnoreCase );
        }
    }
}