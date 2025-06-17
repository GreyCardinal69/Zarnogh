using Zarnogh.Services;

namespace Zarnogh.Modules.ServerManagement
{
    public class ServerCommandsModule : IBotModule
    {
        public string NameOfModule => "Server Management";
        public string ModuleDescription => "Provides commands for managing the bot's server configuration.";
        public bool IsACoreModule => true;
        public bool ServerSpecificModule => false;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.CommandsNext.RegisterCommands<ServerCommands>();
            Logger.LogMessage( $"Registered Server Management Module." );
        }
    }
}