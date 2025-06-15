using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules.Isolation
{
    public class IsolationCommandsModule : IBotModule
    {
        public string NameOfModule => "Isolation Commands";
        public string ModuleDescription => "Provides commands for the isolation of troublesome users.";
        public bool IsACoreModule => true;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.CommandsNext.RegisterCommands<IsolationCommands>();
            Logger.LogMessage( $"Registered Isolation Commands Module." );
        }
    }
}