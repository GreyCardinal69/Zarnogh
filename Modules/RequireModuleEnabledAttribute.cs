using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Zarnogh.Modules
{
    public class RequireModuleEnabledAttribute : CheckBaseAttribute
    {
        public string ModuleName { get; }

        public RequireModuleEnabledAttribute( string moduleName )
        {
            ModuleName = moduleName;
        }

        public override async Task<bool> ExecuteCheckAsync( CommandContext ctx, bool help )
        {

            if ( !await ( (ModuleManager)ctx.Services.GetService( typeof( ModuleManager ) ) ).IsModuleEnabledForGuild( ModuleName, ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( $"The module \"{ModuleName}\" is not enabled for this server, aborting..." );
                return false;
            }
            return true;
        }
    }
}