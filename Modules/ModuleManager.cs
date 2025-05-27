using System.Reflection;
using DSharpPlus.CommandsNext;
using Zarnogh.Configuration;
using Zarnogh.Services;

namespace Zarnogh.Modules
{
    public class ModuleManager
    {
        private readonly List<IBotModule> _loadedModules = new List<IBotModule>();
        private readonly List<IBotModule> _loadedGlobalModules = new List<IBotModule>();
        private readonly ServiceProvider _services;
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;

        public IReadOnlyList<IBotModule> LoadedModules => _loadedModules.AsReadOnly();
        public IReadOnlyList<IBotModule> LoadedGlobalModules => _loadedGlobalModules.AsReadOnly();

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

            Logger.LogMessage( "\nAttempting to load command modules..." );
            foreach ( var type in moduleTypes )
            {
                try
                {
                    if ( Activator.CreateInstance( type ) is IBotModule moduleInstance )
                    {
                        _loadedModules.Add( moduleInstance );
                        if ( moduleInstance.IsACoreModule || _botConfig.DefaultGlobalModules.Contains( moduleInstance.NameOfModule ) )
                        {
                            await moduleInstance.InitializeAsync( _services );
                            _loadedGlobalModules.Add( moduleInstance );
                            Logger.LogMessage( $"Loaded module: {moduleInstance.NameOfModule}." );
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

        public bool IsCommandModuleGlobal( string name )
        {
            for ( int i = 0; i < _loadedModules.Count; i++ ) if ( string.Equals( name, _loadedModules[i] ) && _loadedModules[i].IsACoreModule ) return true;
            return false;
        }

        public bool IsModuleEnabledForGuild( string moduleName, ulong guildId )
        {
            if ( _botConfig.DefaultGlobalModules.Contains( moduleName ) ) return true;
            var guildConfig = _guildConfigManager.GetGuildConfig(guildId).Result;
            return guildConfig.EnabledModules.Contains( moduleName, StringComparer.OrdinalIgnoreCase );
        }
    }
}