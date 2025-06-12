using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.Logging
{
    public class LoggingCommandsModule : IBotModule
    {
        public string NameOfModule => "Logging";
        public string ModuleDescription => "Provides commands for managing internal and server logging.";
        public bool IsACoreModule => false;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( CommandsNextExtension commandsNext, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( commandsNext );
            commandsNext.RegisterCommands<LoggingCommands>();
            Logger.LogMessage( $"Registered Logging Module." );
        }
    }
}