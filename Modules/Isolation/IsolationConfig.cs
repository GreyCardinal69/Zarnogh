namespace Zarnogh.Modules.Isolation
{
    public class IsolationConfig
    {
        public Dictionary<ulong, ulong> IsolationChannelRolePairs { get; set; } // channel - role
        public List<IsolationEntry> ActiveIsolationEntries { get; set; }
    }
}