namespace Zarnogh.Modules.Isolation
{
    public class IsolationEntry
    {
        public string Reason { get; set; }
        public ulong UserId { get; init; }
        public ulong IsolationChannelId { get; init; }
        public ulong IsolationRoleId { get; init; }
        public DateTime IsolationCreationDate { get; init; }
        public DateTime IsolationReleaseDate { get; init; }
        public ulong[] UserRolesUponIsolation { get; init; }
        public bool ReturnRolesOnRelease { get; init; }
    }
}