using Zarnogh.Services;

namespace Zarnogh.Modules.General
{
    public class GeneralCommandsModule : IBotModule
    {
        public string NameOfModule => "General Commands";
        public string ModuleDescription => "Provides general utility commands.";
        public bool IsACoreModule => true;
        public bool ServerSpecificModule => false;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.CommandsNext.RegisterCommands<GeneralCommands>();
            Logger.LogMessage( $"Registered GeneralCommands Module." );
        }
    }
}