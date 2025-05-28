using DSharpPlus.CommandsNext;
using Zarnogh.Modules.General;
using Zarnogh.Services;

namespace Zarnogh.Modules.Debug
{
    public class DebugCommandsModule : IBotModule
    {
        public string NameOfModule => "Debug Commands";
        public string ModuleDescription => "Provides utility commands for debugging the bot.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( CommandsNextExtension commandsNext, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( commandsNext );
            commandsNext.RegisterCommands<DebugCommands>();
            Logger.LogMessage( $"Registered DebugCommands Module." );
        }
    }
}