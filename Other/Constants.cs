using DSharpPlus.Entities;

namespace Zarnogh.Other
{
    public class Constants
    {
        public static readonly string ChannelExportFirstHalf = File.ReadAllText( $"{AppDomain.CurrentDomain.BaseDirectory}Content\\ExportChannelFirstHalf.txt" );
        public static readonly DiscordColor ZarnoghPink = new DiscordColor(16711789);
    }
}