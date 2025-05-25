using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.General
{
    public class GeneralCommandsModule : IBotModule
    {
        public string NameOfModule => "General Commands";
        public string ModuleDescription => "Provides general utility commands.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( CommandsNextExtension commandsNext, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( commandsNext );
            commandsNext.RegisterCommands<GeneralCommands>();
            Logger.LogMessage( $"Registered GeneralCommands." );
        }
    }
}