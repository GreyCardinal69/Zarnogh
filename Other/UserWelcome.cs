namespace Zarnogh.Other
{
    public readonly struct UserWelcome
    {
        public readonly string Content;
        public readonly ulong RoleId;
        public readonly ulong ChannelId;

        public UserWelcome( string content, ulong roleId, ulong channelId )
        {
            Content = content;
            RoleId = roleId;
            ChannelId = channelId;
        }
    }
}