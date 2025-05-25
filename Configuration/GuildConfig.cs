namespace Zarnogh.Configuration
{
    public class GuildConfig
    {
        public string GuildName { get; set; }
        public ulong GuildId { get; set; }
        public List<string> EnabledModules { get; set; }
    }
}