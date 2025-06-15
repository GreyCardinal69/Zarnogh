using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.Debug
{
    public class DebugCommandsModule : IBotModule
    {
        public string NameOfModule => "Debug Commands";
        public string ModuleDescription => "Provides utility commands for the remote control and debugging of the bot.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.CommandsNext.RegisterCommands<DebugCommands>();
            Logger.LogMessage( $"Registered DebugCommands Module." );
        }
    }
}