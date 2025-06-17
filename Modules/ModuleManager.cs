using System.Reflection;
using Zarnogh.Configuration;
using Zarnogh.Services;

namespace Zarnogh.Modules
{
    public class ModuleManager
    {
        private readonly List<IBotModule> _loadedModules = new List<IBotModule>();
        private readonly List<IBotModule> _loadedGlobalModules = new List<IBotModule>();
        private readonly List<IBotModule> _loadedServerSpecificModules = new List<IBotModule>();
        private readonly ServiceProvider _services;
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;

        public IReadOnlyList<IBotModule> LoadedModules => _loadedModules.AsReadOnly();
        public IReadOnlyList<IBotModule> LoadedGlobalModules => _loadedGlobalModules.AsReadOnly();
        public IReadOnlyList<IBotModule> LoadedServerSpecificModules => _loadedServerSpecificModules.AsReadOnly();

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
                        if ( moduleInstance.ServerSpecificModule )
                        {
                            _loadedServerSpecificModules.Add( moduleInstance );
                            await moduleInstance.InitializeAsync( _services );
                            Logger.LogMessage( $"Loaded module: {moduleInstance.NameOfModule} (Server Specific)." );
                            continue;
                        }
                        _loadedModules.Add( moduleInstance );
                        if ( moduleInstance.IsACoreModule || _botConfig.DefaultGlobalModules.Contains( moduleInstance.NameOfModule ) )
                        {
                            await moduleInstance.InitializeAsync( _services );
                            _loadedGlobalModules.Add( moduleInstance );
                            Logger.LogMessage( $"Loaded module: {moduleInstance.NameOfModule} (Global)." );
                        }
                        else
                        {
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

        public void RegisterModuleCommands( ZarnoghState state )
        {
            Logger.LogMessage( "\nAttempting to register commands for loaded modules..." );
            foreach ( var module in _loadedModules )
            {
                module.RegisterCommands( state, _services );
                Logger.LogMessage( $"Registered commands for module: {module.NameOfModule}" );
            }
            foreach ( var module in _loadedServerSpecificModules )
            {
                module.RegisterCommands( state, _services );
                Logger.LogMessage( $"Registered commands for module: {module.NameOfModule} (Server Specific)" );
            }
            Logger.LogMessage( "Finished registering commands for loaded modules.\n" );
        }

        public bool IsCommandModuleGlobal( string name )
        {
            for ( int i = 0; i < _loadedModules.Count; i++ ) if ( string.Equals( name, _loadedModules[i].NameOfModule, StringComparison.Ordinal ) && _loadedModules[i].IsACoreModule ) return true;
            return false;
        }

        public bool CommandModuleExists( string name )
        {
            for ( int i = 0; i < _loadedModules.Count; i++ ) if ( string.Equals( name, _loadedModules[i].NameOfModule, StringComparison.Ordinal ) ) return true;
            return false;
        }

        public async Task<bool> IsModuleEnabledForGuild( string moduleName, ulong guildId )
        {
            if ( _botConfig.DefaultGlobalModules.Contains( moduleName ) ) return true;
            var guildConfig = await _guildConfigManager.GetOrCreateGuildConfig(guildId);
            return guildConfig.EnabledModules.Contains( moduleName, StringComparer.OrdinalIgnoreCase );
        }
    }
}