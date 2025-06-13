namespace Zarnogh.Configuration
{
    public class UserProfile
    {
        public ulong ID { get; init; }
        public List<(DateTime, string)> IsolationEntries { get; set; }
        public List<(DateTime, string)> BanEntries { get; set; }
        public List<(DateTime, string)> KickEntries { get; set; }
        public Dictionary<int, string> Notes { get; set; }
        public DateTimeOffset CreationDate { get; init; }

        public UserProfile( ulong id, DateTimeOffset creationDate )
        {
            IsolationEntries = new List<(DateTime, string)>();
            BanEntries = new List<(DateTime, string)>();
            KickEntries = new List<(DateTime, string)>();
            Notes = new Dictionary<int, string>();

            ID = id;
            CreationDate = creationDate;
        }
    }
}