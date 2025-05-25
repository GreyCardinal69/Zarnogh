using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Zarnogh
{
    public class ZarnoghState
    {
        public DiscordClient Client { get; init; }
        public DateTime StartUpTime { get; init; }
        public CommandsNextExtension CommandsNext { get; init; }

        public CommandContext CreateNewCommandContext( ulong guildId, ulong channelId = 0 )
        {
            CommandsNextExtension cmds = Client.GetCommandsNext();
            Command cmd = cmds.FindCommand( "fake", out string? customArgs );
            customArgs = "fake";
            DiscordGuild guild = Client.GetGuildAsync( guildId ).Result;

            DiscordChannel channel = channelId == 0 ? guild.Channels.Values.First() : guild.GetChannel(channelId);
            CommandContext context = cmds.CreateFakeContext( Client.CurrentUser, channel, "fake", "fake", cmd, customArgs );
            return context;
        }
    }
}