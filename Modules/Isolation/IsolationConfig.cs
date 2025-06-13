namespace Zarnogh.Modules.Isolation
{
    public class IsolationConfig
    {
        public Dictionary<ulong, ulong> IsolationChannelRolePairs { get; set; } // channel - role
        public List<IsolationEntry> ActiveIsolationEntries { get; set; }

        public (ulong, ulong) GetFreeOrFirstIsolationPair()
        {
            // no active entries, all isolation channels are free.
            if ( ActiveIsolationEntries.Count == 0 )
            {
                var pair = IsolationChannelRolePairs.First();
                return (pair.Key, pair.Value);
            }

            // find free isolation channel
            foreach ( var pair in IsolationChannelRolePairs )
            {
                foreach ( var entry in ActiveIsolationEntries )
                {
                    if ( entry.IsolationChannelId != pair.Key ) return (pair.Key, pair.Value);
                }
            }

            // all isolation channels are busy, we'll use the first one
            var first = IsolationChannelRolePairs.First();
            return (first.Key, first.Value);
        }
    }
}