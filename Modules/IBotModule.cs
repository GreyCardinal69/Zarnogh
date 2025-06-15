using DSharpPlus.CommandsNext;
using Zarnogh.Services;

namespace Zarnogh.Modules
{
    public interface IBotModule
    {
        string NameOfModule { get; }
        string ModuleDescription { get; }
        bool IsACoreModule { get; }
        Task InitializeAsync( ServiceProvider services );
        void RegisterCommands( ZarnoghState state, ServiceProvider services );
    }
}