namespace Zarnogh.Configuration
{
    public class UserProfile
    {
        public string UserName { get; set; }
        public ulong ID { get; init; }
        public List<(DateTime, string)> IsolationEntries { get; set; }
        public List<(DateTime, string)> BanEntries { get; set; }
        public List<(DateTime, string)> KickEntries { get; set; }
        public Dictionary<int, string> Notes { get; set; }
        public DateTimeOffset CreationDate { get; init; }

        public UserProfile( ulong id, DateTimeOffset creationDate, string username )
        {
            IsolationEntries = new List<(DateTime, string)>();
            BanEntries = new List<(DateTime, string)>();
            KickEntries = new List<(DateTime, string)>();
            Notes = new Dictionary<int, string>();

            UserName = username;
            ID = id;
            CreationDate = creationDate;
        }
    }
}