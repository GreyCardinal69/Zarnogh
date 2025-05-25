namespace Zarnogh.Configuration
{
    public class BotConfig
    {
        public string Token { get; init; }
        public string Prefix { get; init; }
        public IReadOnlyList<string> DefaultGlobalModules { get; set; }
        public ulong OwnerId { get; init; }
    }
}
