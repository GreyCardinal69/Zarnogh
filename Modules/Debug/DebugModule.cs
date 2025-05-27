using DSharpPlus.CommandsNext;
using Zarnogh.Modules.General;
using Zarnogh.Services;

namespace Zarnogh.Modules.Debug
{
    public class DebugModule : IBotModule
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
            commandsNext.RegisterCommands<GeneralCommands>();
            Logger.LogMessage( $"Registered DebugCommands Module." );
        }
    }
}