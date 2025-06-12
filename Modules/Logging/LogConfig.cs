namespace Zarnogh.Modules.Logging
{
    public class LogConfig
    {
        public bool OnInviteDeleted { get; set; }
        public bool OnGuildRoleDeleted { get; set; }
        public bool OnMessageDeleted { get; set; }
        public bool OnMessageUpdated { get; set; }
        public bool OnChannelDeleted { get; set; }
        public bool OnChannelCreated { get; set; }
        public bool OnInviteCreated { get; set; }
        public bool OnMessageCreated { get; set; }
        public bool OnGuildBanAdded { get; set; }
        public bool OnGuildBanRemoved { get; set; }
        public bool OnGuildMemberAdded { get; set; }
        public bool OnGuildMemberRemoved { get; set; }
        public bool OnMessagesBulkDeleted { get; set; }
        public List<ulong> ChannelsExcludedFromLogging { get; set; }
    }
}