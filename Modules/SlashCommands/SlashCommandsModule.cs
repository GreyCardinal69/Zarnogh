using Zarnogh.Services;

namespace Zarnogh.Modules.SlashCommands
{
    internal class SlashCommandsModule : IBotModule
    {
        public string NameOfModule => "Slash Commands";
        public string ModuleDescription => "Provides essential slash commands for the server.";
        public bool IsACoreModule => true;
        public bool ServerSpecificModule => false;

        public Task InitializeAsync( ServiceProvider services )
        {
            return Task.CompletedTask;
        }

        public void RegisterCommands( ZarnoghState state, ServiceProvider services )
        {
            ArgumentNullException.ThrowIfNull( state );
            state.SlashNext.RegisterCommands<SlashCommands>();
            Logger.LogMessage( $"Registered Slash Commands Module." );
        }
    }
}