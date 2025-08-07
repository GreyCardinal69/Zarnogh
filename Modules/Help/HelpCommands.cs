using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Zarnogh.Configuration;
using Zarnogh.Other;

namespace Zarnogh.Modules.Help
{
    public class HelpCommands : BaseCommandModule
    {
        private readonly BotConfig _botConfig;
        private readonly ZarnoghState _botState;
        private readonly GuildConfigManager _guildConfigManager;
        private readonly ModuleManager _moduleManager;

        public HelpCommands( BotConfig botConfig, GuildConfigManager guildConfigManager, ModuleManager moduleManager, ZarnoghState botState )
        {
            _botState = botState;
            _botConfig = botConfig;
            _guildConfigManager = guildConfigManager;
            _moduleManager = moduleManager;
        }

        [Command( "Help" )]
        [Description( "Responds with information on available command modules or module commands." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task HelpCommand( CommandContext ctx, [RemainingText] string category = null )
        {
            await ctx.TriggerTypingAsync();
            DiscordEmbedBuilder embed;

            StringBuilder categories =  new StringBuilder();

            for ( int i = 0; i < _moduleManager.LoadedModules.Count; i++ )
            {
                if ( string.Equals( _moduleManager.LoadedModules[i].NameOfModule, "Help Commands", StringComparison.Ordinal ) ) continue;
                categories.Append( $"`{_moduleManager.LoadedModules[i].NameOfModule}" );
                if ( _moduleManager.LoadedModules[i].IsACoreModule ) categories.Append( " (Global)`" );
                else categories.Append( '`' );
                if ( i < _moduleManager.LoadedModules.Count - 1 ) categories.Append( '\n' );
            }
            categories.Append( '.' );

            if ( string.IsNullOrEmpty( category ) )
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Modules:",
                    Color = Constants.ZarnoghPink,
                    Description =
                    $"Listing command modules. \n Type `{_botConfig.Prefix}help <module>` to get more info on the specified module. \n\n **Modules**\n{categories.ToString()}\n\nDon't include `(Global)` in your help command.",
                    Timestamp = DateTime.UtcNow,
                };
                await ctx.RespondAsync( embed );
                return;
            }

            switch ( category.ToLowerInvariant() )
            {
                case "isolation commands":
                    StringBuilder isoCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}AddIsolationPair <ChannelID> <RoleID>`: ")
                        .Append($"Adds a channel and an appropriate role for isolation of users in that channel, with that role.\n\n")
                        .Append($"`{_botConfig.Prefix}Isolate <UserID> <Time> <ReturnRolesOnRelease> <Reason>`: ")
                        .Append($"Isolates the user at the first free Channel-Role isolation pair, if all are busy isolates at the first pair. ")
                        .Append($"`<Time>` is given as: `time_d`, where time can be both an integer and a double, f.e `0.5` for half a day. ")
                        .Append($"`<ReturnRolesOnRelease>` is a boolean, if set to true the bot will return the user's roles before the user was isolated.\n\n")
                        .Append($"`{_botConfig.Prefix}ReleaseUser <UserID>`: Releases the given user from isolation.");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Isolation Commands",
                        Color = Constants.ZarnoghPink,
                        Description = isoCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "debug commands":
                    StringBuilder debugCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}UpTime`: Responds with the bot's total uptime.\n\n")
                        .Append($"`{_botConfig.Prefix}ClearConsoleCache`: Clears the bot's console cache.\n\n")
                        .Append($"`{_botConfig.Prefix}DumpConsole <N>`: Responds with the last `N` lines of the console logs, upper limit of 1000.\n\n")
                        .Append($"`{_botConfig.Prefix}DumpConsole`: Responds with the last 20 lines of the console logs.\n\n")
                        .Append($"`{_botConfig.Prefix}Terminate`: Shuts down the bot.\n\n")
                        .Append($"`{_botConfig.Prefix}DumpInternalConsoleCache`: Dumps the bot's internal _cachedLines list of the console logs.\n\n")
                        .Append($"`{_botConfig.Prefix}SetInternalTickLoopInterval`: Changes the bot's TickLoopIntervalMilliseconds config property.");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Debug Commands",
                        Color = Constants.ZarnoghPink,
                        Description = debugCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                        Footer = new DiscordEmbedBuilder.EmbedFooter()
                        {
                            Text = "Debug Commands require owner permissions to execute, except for: `UpTime`."
                        }
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "timed commands":
                    StringBuilder timedCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}AddTimedReminder <Name> <Content> <Repeat> <DateType> <Date>`: Adds a timed reminder which goes off at a certain date with an option to repeat. ")
                        .Append($"You can use `_` for spaces in: `<Name>` and `<Content>`, or do \"...\" instead. There are 3 options for `<Date>`: `-r`, `-t` and `-e`. ")
                        .Append($"`-r` Adds day-hour-minute amount of time to the current date, in that order and format with numbers. `-t` Works with specific day-hour system, ")
                        .Append($"hour is 0-23 and for the day insert the first two letters of the day. `-e` Sets a timer for a very specific date in month-day-hour format.")
                        .Append($"  This type of reminder does not repeat even if told to.\n\n")
                        .Append($"`{_botConfig.Prefix}ListTimedReminders`: Lists all current registered timed reminders for the server, and their contents.\n\n")
                        .Append($"`{_botConfig.Prefix}PurgeTimedReminders`: Removes all registered timed reminders for the server.\n\n")
                        .Append($"`{_botConfig.Prefix}RemoveTimedReminder <Name>`: Removes a registered timed reminder with the given name.\n\n");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Timed Commands",
                        Color = Constants.ZarnoghPink,
                        Description = timedCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "server management":
                    StringBuilder serverCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}ToggleEraseAutoDelete <Bool>`: After the usage of any of the erase commands the bot responds with a message, ")
                        .Append($"you can instruct the bot to delete that message too after a short while.\n\n")
                        .Append($"`{_botConfig.Prefix}SetNotificationsChannel <ChannelID>`: Set's the channel for the bot's primary notifications.\n\n")
                        .Append($"`{_botConfig.Prefix}ToggleCommandModule <ModuleName>`: Toggles the given command module for the server.\n\n")
                        .Append($"`{_botConfig.Prefix}ListCommandModules`: Responds with the names of all command modules.\n\n")
                        .Append($"`{_botConfig.Prefix}DisableCustomWelcome`: If enabled, disables the custom welcome message for the server.\n\n")
                        .Append($"`{_botConfig.Prefix}SetCustomWelcome <Message> <RoleID> <ChannelID>`: Sets a custom welcome message that the bot will execute for new users. ")
                        .Append($"`<Message>` is what the bot will say, you can add `MENTION`, in this case the bot will also mention the new user in its message. ")
                        .Append($"`<ChannelID>` - Where the bot will send the message. `<RoleID>` - Whether the bot will give the user some role. leave as `0` if not required.\n\n")
                        .Append($"`{_botConfig.Prefix}CreateUserProfiles`: Creates user profiles for all the members in the server, the process takes a while.\n\n")
                        .Append($"`{_botConfig.Prefix}UserProfile <UserID>`: Responds with the given user's profile.\n\n")
                        .Append($"`{_botConfig.Prefix}ResetCustomWelcome`: Resets the custom welcome message for the server, disabling it.\n\n")
                        .Append($"`{_botConfig.Prefix}AddUserNote <UserId> <NoteIndex> <Note>`: Adds a note to the user's profile. `<Index>` is an integer.\n\n")
                        .Append($"`{_botConfig.Prefix}RemoveUserNote <UserId> <NoteIndex>`: Removes a note from the user's profile at the given index.\n\n")
                        .Append($"`{_botConfig.Prefix}ServerProfile`: Responds with the server's profile.\n\n");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Server Management",
                        Color = Constants.ZarnoghPink,
                        Description = serverCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "logging":
                    StringBuilder loggingCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}AddLogExclusion <ChannelID>`: Instructs the bot to exclude the channel from being logged.\n\n")
                        .Append($"`{_botConfig.Prefix}SetLogChannel <ChannelID>`: Sets the bot's log events' reports channel.\n\n")
                        .Append($"`{_botConfig.Prefix}RemoveLogExclusion <ChannelID>`: Instructs the bot to resume the logging of the excluded channel.\n\n")
                        .Append($"`{_botConfig.Prefix}ListLogExclusions`: Lists all channels excluded from logging.\n\n")
                        .Append($"`{_botConfig.Prefix}ToggleLogEvents`: Creates a menu allowing for the toggling of events the bot will log for the server.\n\n");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Logging",
                        Color = Constants.ZarnoghPink,
                        Description = loggingCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "general commands":
                    StringBuilder generalCommands = new StringBuilder()
                        .Append($"`{_botConfig.Prefix}Ping`: Gets the WS latency for the current bot client.\n\n")
                        .Append($"`{_botConfig.Prefix}BotTalk <GuildID> <ChannelID> <ThreadID> <UseThread> <Message>`: Allows to talk as the bot, in the given ")
                        .Append($"channel or thread of a channel.\n\n")
                        .Append($"`{_botConfig.Prefix}BotTalkReply <GuildID> <ChannelID> <ThreadID> <UseThread> <MessageID> <Message>`: Allows to talk as the bot, ")
                        .Append($"in the given channel or thread of a channel, instructs the bot to reply to the given message.\n\n")
                        .Append($"`{_botConfig.Prefix}Kick <UserId> <Reason>`: Kicks the given user with the given reason, requires confirmation first.\n\n")
                        .Append($"`{_botConfig.Prefix}Ban <UserId> <CountOfMessagesToDelete> <Reason>`: Bans the given user with the given reason,")
                        .Append($" with the given amount of messages to delete, requires confirmation first.\n\n")
                        .Append($"`{_botConfig.Prefix}Unban <UserID>`: Unbans the given user.\n\n")
                        .Append($"`{_botConfig.Prefix}SetStatus <Type> <StatusMessage>`: Sets the bots status, requires owner permissions.\n\n")
                        .Append($"`{_botConfig.Prefix}Erase <Amount>`: Deletes `<Amount>` of messages in the channel.\n\n")
                        .Append($"`{_botConfig.Prefix}EraseFromTo <FromID> <ToID>`: Deletes messages within a specified range, defined by a start and end message IDs (both inclusive).\n\n")
                        .Append($"`{_botConfig.Prefix}EraseAggressive <Amount>`: Deletes `<Amount>` of messages in the channel, used for the deletion of messages ")
                        .Append($"older than 2 weeks.\n\n");

                    embed = new DiscordEmbedBuilder
                    {
                        Title = "General Commands",
                        Color = Constants.ZarnoghPink,
                        Description = generalCommands.ToString(),
                        Timestamp = DateTime.UtcNow,
                    };
                    await ctx.RespondAsync( embed );
                    break;
            }
        }
    }
}