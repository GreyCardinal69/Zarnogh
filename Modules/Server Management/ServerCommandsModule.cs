using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.ServerManagement
{
    public class ServerCommandsModule : IBotModule
    {
        public string NameOfModule => "Server Management";
        public string ModuleDescription => "Provides commands for managing the bot's server configuration.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( CommandsNextExtension commandsNext, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( commandsNext );
            commandsNext.RegisterCommands<ServerCommands>();
            Logger.LogMessage( $"Registered Server Management Module." );
        }
    }
}