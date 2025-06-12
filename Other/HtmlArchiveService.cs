using System.IO.Compression;
using System.Net;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Zarnogh.Services;

namespace Zarnogh.Other
{
    public static class HtmlArchiveService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ArchiveInputAsync( CommandContext ctx, IReadOnlyList<DiscordMessage> messages, DiscordChannel channel )
        {
            string baseAppDir = AppDomain.CurrentDomain.BaseDirectory;
            string globalExportsOutputFolder = Path.Combine(baseAppDir, "Exports");

            if ( Directory.Exists( globalExportsOutputFolder ) )
            {
                var filesToDelete = Directory.GetFiles(globalExportsOutputFolder);
                foreach ( var filePath in filesToDelete )
                {
                    try
                    {
                        File.Delete( filePath );
                    }
                    catch ( Exception ex ) { Logger.LogError( $"Error deleting old file {filePath}: {ex.Message}" ); }
                }
            }
            Directory.CreateDirectory( globalExportsOutputFolder );

            string exportSessionGuid = Guid.NewGuid().ToString();
            string tempSessionWorkingDir = Path.Combine(Path.GetTempPath(), $"DiscordExport_{exportSessionGuid}");
            Directory.CreateDirectory( tempSessionWorkingDir );

            string exportId = $"{ctx.Guild.Name.Replace(" ", "")}{new Random().Next(1, int.MaxValue)}";

            string htmlFileName = $"{exportId}.html";
            string imagesFolderName = $"{exportId}_Images";

            string absoluteHtmlFilePath = Path.Combine(tempSessionWorkingDir, htmlFileName);
            string absoluteImagesFolderPath = Path.Combine(tempSessionWorkingDir, imagesFolderName);
            Directory.CreateDirectory( absoluteImagesFolderPath );

            StringBuilder sb = new StringBuilder();
            sb.Append( @$"<!DOCTYPE html><html lang=""en""><head><title>{ctx.Guild.Name} - #{channel.Name}</title>" );
            sb.Append( Constants.ChannelExportFirstHalf );

            string guildIconFileName = SanitizeFileName($"{ctx.Guild.Name}.png");
            string guildIconLocalPath = Path.Combine(absoluteImagesFolderPath, guildIconFileName);

            sb.Append( @"<body><div class=""preamble""><div class=""preamble__guild-icon-container""><img class=""preamble__guild-icon"" src=""" );
            sb.Append( WebUtility.HtmlEncode( Path.Combine( imagesFolderName, guildIconFileName ).Replace( '\\', '/' ) ) );
            sb.Append( @""" alt=""Guild icon"" loading=""lazy""></div><div class=""preamble__entries-container""><div class=""preamble__entry"">" );
            sb.Append( WebUtility.HtmlEncode( ctx.Guild.Name ) );
            sb.Append( @"</div><div class=""preamble__entry"">" );
            sb.Append( WebUtility.HtmlEncode( $"{( channel.Parent?.Name ?? "Unknown Category" ).ToLowerInvariant()} / #{channel.Name}" ) );
            sb.Append( @"</div><div class=""preamble__entry preamble__entry--small"">" );
            sb.Append( WebUtility.HtmlEncode( channel.Topic ) );
            sb.Append( @"</div></div></div><div class=""chatlog"">" );

            ulong oldId = 0;
            bool open = false;

            foreach ( DiscordMessage item in messages.Reverse() )
            {
                if ( item.Author == null ) continue;

                string authorAvatarFileName = SanitizeFileName($"{item.Author.Username}_{item.Author.Id}.png");
                string htmlAuthorAvatarPath = WebUtility.HtmlEncode(Path.Combine(imagesFolderName, authorAvatarFileName).Replace('\\', '/'));
                string localAuthorAvatarPath = Path.Combine(absoluteImagesFolderPath, authorAvatarFileName);

                if ( !File.Exists( localAuthorAvatarPath ) && !string.IsNullOrEmpty( item.Author.GetAvatarUrl( ImageFormat.Png ) ) )
                {
                    await DownloadFileAsync( item.Author.GetAvatarUrl( ImageFormat.Png ), localAuthorAvatarPath );
                }

                if ( oldId != item.Author.Id && !open )
                {
                    oldId = item.Author.Id;
                    open = true;
                    sb.Append( $@"<div class=""chatlog__message-group""><div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container""" );
                    sb.Append( $@"data-message-id=""{item.Id}""><div class=""chatlog__message""><div class=""chatlog__message-aside"">" );
                    sb.Append( $@"<img class=""chatlog__avatar"" src=""{htmlAuthorAvatarPath}"" alt=""Avatar"" loading=""lazy""></div><div class=""chatlog__message-primary"">" );
                    sb.Append( $@"<div class=""chatlog__header""><span class=""chatlog__author"" title=""{WebUtility.HtmlEncode( item.Author.Username )}""" );
                    sb.Append( $@"data-user-id=""{item.Author.Id}"">{WebUtility.HtmlEncode( item.Author.Username )}</span><span class=""chatlog__timestamp"">{item.Timestamp}</span></div>" );
                }
                else if ( oldId != item.Author.Id && open )
                {
                    oldId = item.Author.Id;
                    sb.Append( "</div>" );
                    sb.Append( "</div>" );
                    sb.Append( $@"<div class=""chatlog__message-group""><div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container"" data-message-id=""{item.Id}""><div class=""chatlog__message""><div class=""chatlog__message-aside"">" );
                    if ( item.ReferencedMessage != null ) sb.Append( @"<div class=""chatlog__reference-symbol""></div>" );
                    sb.Append( $@"<img class=""chatlog__avatar"" src=""{htmlAuthorAvatarPath}"" alt=""Avatar"" loading=""lazy""></div><div class=""chatlog__message-primary"">" );
                    sb.Append( $@"<div class=""chatlog__header""><span class=""chatlog__author"" title=""{WebUtility.HtmlEncode( item.Author.Username )}"" data-user-id=""{item.Author.Id}"">{WebUtility.HtmlEncode( item.Author.Username )}</span><span class=""chatlog__timestamp"">{item.Timestamp}</span></div>" );
                }
                else if ( oldId == item.Author.Id && open )
                {
                    sb.Append( $@"<div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container"" data-message-id=""{item.Id}""><div class=""chatlog__message"">" ); // No new group or primary avatar display
                    sb.Append( $@"<div class=""chatlog__message-aside""><div class=""chatlog__short-timestamp"" title=""{item.Timestamp}"">{item.Timestamp.ToString( "HH:mm" )}</div></div><div class=""chatlog__message-primary"">" ); // No header, just content
                }

                if ( item.ReferencedMessage != null && item.ReferencedMessage.Author != null )
                {
                    string refAuthorAvatarFileName = SanitizeFileName($"{item.ReferencedMessage.Author.Username}_{item.ReferencedMessage.Author.Id}.png");
                    string htmlRefAuthorAvatarPath = WebUtility.HtmlEncode(Path.Combine(imagesFolderName, refAuthorAvatarFileName).Replace('\\', '/'));
                    string localRefAuthorAvatarPath = Path.Combine(absoluteImagesFolderPath, refAuthorAvatarFileName);
                    if ( !File.Exists( localRefAuthorAvatarPath ) && !string.IsNullOrEmpty( item.ReferencedMessage.Author.GetAvatarUrl( ImageFormat.Png ) ) )
                    {
                        await DownloadFileAsync( item.ReferencedMessage.Author.GetAvatarUrl( ImageFormat.Png ), localRefAuthorAvatarPath );
                    }

                    sb.Append( $@"<div class=""chatlog__reference""><img class=""chatlog__reference-avatar"" src=""{htmlRefAuthorAvatarPath}"" alt=""Ref Avatar"" loading=""lazy""><div class=""chatlog__reference-author"" title=""{WebUtility.HtmlEncode( item.ReferencedMessage.Author.Username )}"">{WebUtility.HtmlEncode( item.ReferencedMessage.Author.Username )}</div><div class=""chatlog__reference-content""><span class=""chatlog__reference-link"" onclick=""scrollToMessage(event, '{item.ReferencedMessage.Id}')"">{WebUtility.HtmlEncode( item.ReferencedMessage.Content )}</span></div></div>" );
                }

                if ( item.MentionedUsers.Any() )
                {
                    string contentWithMentions = WebUtility.HtmlEncode(item.Content);
                    foreach ( var mentionedUser in item.MentionedUsers )
                    {
                        contentWithMentions = contentWithMentions.Replace( WebUtility.HtmlEncode( mentionedUser.Mention ),
                            $@"<span class=""chatlog__markdown-mention"" title=""{WebUtility.HtmlEncode( mentionedUser.Username )}"">@{WebUtility.HtmlEncode( mentionedUser.Username )}</span>" );
                    }
                    sb.Append( $@"<div class=""chatlog__content chatlog__markdown""><span class=""chatlog__markdown-preserve"">{contentWithMentions}</span></div>" );
                }
                else
                {
                    sb.Append( $@"<div class=""chatlog__content chatlog__markdown""><span class=""chatlog__markdown-preserve"">{WebUtility.HtmlEncode( item.Content )}</span></div>" );
                }

                if ( item.Attachments.Any() )
                {
                    foreach ( var att in item.Attachments )
                    {
                        string sanitizedAttFileName = SanitizeFileName(att.FileName);
                        string htmlAttachmentPath = WebUtility.HtmlEncode(Path.Combine(imagesFolderName, sanitizedAttFileName).Replace('\\', '/'));
                        string localAttachmentPath = Path.Combine(absoluteImagesFolderPath, sanitizedAttFileName);
                        await DownloadFileAsync( att.Url, localAttachmentPath );
                        sb.Append( $@"<div class=""chatlog__attachment""><img class=""chatlog__attachment-media"" src=""{htmlAttachmentPath}"" alt=""{WebUtility.HtmlEncode( att.FileName )}"" title=""{WebUtility.HtmlEncode( att.FileName )} ({att.FileSize} bytes)"" loading=""lazy""></div>" );
                    }
                }
                if ( oldId == item.Author.Id && open )
                {
                    sb.Append( @"</div></div>" );
                }
                else
                {
                    sb.Append( @"</div></div></div>" );
                }
            }

            if ( open )
            {
                sb.Append( "</div>" );
            }

            sb.Append( $@"</div><div class=""postamble""><div class=""postamble__entry"">Exported {messages.Count} message(s).</div></div></body></html>" );

            await File.WriteAllTextAsync( absoluteHtmlFilePath, sb.ToString() );

            if ( !string.IsNullOrEmpty( ctx.Guild.IconUrl ) )
            {
                await DownloadFileAsync( ctx.Guild.GetIconUrl( ImageFormat.Png ), guildIconLocalPath );
            }

            string safeGuildName = SanitizeFileName(ctx.Guild.Name);
            string safeChannelName = SanitizeFileName(channel.Name);
            string finalZipFileName = $"Export_{safeGuildName}_{safeChannelName}_{DateTime.Now:yyyyMMddHHmmss}_{exportSessionGuid.Substring(0, 8)}.zip";
            string finalZipFilePath = Path.Combine(globalExportsOutputFolder, finalZipFileName);

            if ( File.Exists( finalZipFilePath ) )
            {
                File.Delete( finalZipFilePath );
            }

            await Task.Run( () => ZipFile.CreateFromDirectory( tempSessionWorkingDir, finalZipFilePath, CompressionLevel.Optimal, false ) );

            try
            {
                Directory.Delete( tempSessionWorkingDir, true );
            }
            catch ( Exception ex )
            {
                Logger.LogError( $"Error deleting temporary session directory {tempSessionWorkingDir}: {ex.Message}" );
            }

            return finalZipFilePath;
        }

        private static async Task DownloadFileAsync( string url, string outputPath )
        {
            if ( string.IsNullOrWhiteSpace( url ) ) return;
            try
            {
                byte[] fileBytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync( outputPath, fileBytes );
            }
            catch ( HttpRequestException ex )
            {
                Logger.LogError( $"HTTP Error downloading {url} to {outputPath}: {ex.Message}" );
            }
            catch ( Exception ex )
            {
                Logger.LogError( $"Error downloading {url} to {outputPath}: {ex.Message}" );
            }
        }

        private static string SanitizeFileName( string fileName )
        {
            if ( string.IsNullOrWhiteSpace( fileName ) ) return "unknown_file";
            foreach ( char c in Path.GetInvalidFileNameChars() )
            {
                fileName = fileName.Replace( c, '_' );
            }
            return fileName.Length > 100 ? fileName.Substring( 0, 100 ) : fileName;
        }
    }

}