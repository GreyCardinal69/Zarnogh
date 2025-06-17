using Zarnogh.Services;

namespace Zarnogh.Modules.Timing
{
    public class TimingModule : IBotModule
    {
        public string NameOfModule => "Timed Commands";
        public string ModuleDescription => "Provides commands that work with the bot's internal time tick system.";
        public bool IsACoreModule => false;
        public bool ServerSpecificModule => false;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.CommandsNext.RegisterCommands<TimingCommands>();
            Logger.LogMessage( $"Registered Timed Commands Module." );
        }
    }
}