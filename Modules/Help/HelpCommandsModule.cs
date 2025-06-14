using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.Help
{
    public class HelpCommandsModule : IBotModule
    {
        public string NameOfModule => "Help Commands";
        public string ModuleDescription => "Provides commands for information about other bot commands.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( CommandsNextExtension commandsNext, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( commandsNext );
            commandsNext.RegisterCommands<HelpCommands>();
            Logger.LogMessage( $"Registered Help Commands Module." );
        }
    }
}