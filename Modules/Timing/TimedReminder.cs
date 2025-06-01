using DSharpPlus.CommandsNext;
using Zarnogh.Configuration;

namespace Zarnogh.Modules.Timing
{
    public class TimedReminder : ITimedAction
    {
        public string Name { get; private set; }
        public bool Repeat { get; private set; }
        public long ExpDate { get; private set; }
        public DateTime StartDate { get; private set; }
        public string Content { get; private set; }
        public string Date { get; private set; }
        public string DateFormat { get; private set; }

        public ZarnoghState BotState { get; set; }
        public GuildConfigManager GuildConfigManager { get; set; }
        public ulong GuildId { get; set; }

        // Sometimes the bot can be started after a timed reminder has expired
        // Dont want a notification after 10 hours at most from expected reminder date
        public bool HasExpiredRecently( DateTimeOffset now )
        {
            var unix =  now.ToUnixTimeSeconds();
            return Math.Abs( unix - ExpDate ) < 36000 && unix >= ExpDate;
        }

        public void UpdateExpirationDate()
        {
            string[] times;
            DateTimeOffset temp;
            DateTimeOffset current = DateTimeOffset.UtcNow;

            switch ( DateFormat )
            {
                case "-r":
                    // 4d, 4d 2h etc
                    times = Date.Split( '-' );
                    ExpDate = DateTimeOffset.UtcNow.AddDays( Convert.ToDouble( times[0] ) ).AddHours( Convert.ToDouble( times[1] ) ).AddMinutes( Convert.ToDouble( times[2] ) ).ToUnixTimeSeconds();
                    break;
                case "-t":
                    // saturday x hour, so on
                    times = Date.Split( '-' );

                    temp = new DateTimeOffset( current.Year, current.Month, current.Day, Math.Max( 0, Convert.ToInt32( times[1] ) - 1 ), 0, 0, new TimeSpan() );
                    int num = 0;

                    switch ( times[0].ToLower() )
                    {
                        case "mo":
                            num = (int)DayOfWeek.Monday;
                            break;
                        case "tu":
                            num = (int)DayOfWeek.Tuesday;
                            break;
                        case "we":
                            num = (int)DayOfWeek.Wednesday;
                            break;
                        case "th":
                            num = (int)DayOfWeek.Thursday;
                            break;
                        case "fr":
                            num = (int)DayOfWeek.Friday;
                            break;
                        case "sa":
                            num = (int)DayOfWeek.Saturday;
                            break;
                        case "su":
                            num = (int)DayOfWeek.Sunday;
                            break;
                    }

                    temp = temp.AddDays( num - (int)temp.DayOfWeek );

                    if ( DateTimeOffset.UtcNow >= temp )
                    {
                        temp = temp.AddDays( 7 );
                    }

                    ExpDate = temp.ToUnixTimeSeconds();
                    break;
                case "-e":

                    // specific day of specific month
                    times = Date.Split( '-' );

                    int month = Convert.ToInt32( times[0] );
                    int day = Convert.ToInt32( times[1] );
                    int hour = Convert.ToInt32( times[2] );

                    temp = new DateTimeOffset( current.Year, month, day, Math.Max( 0, hour - 1 ), 0, 0, new TimeSpan() );
                    ExpDate = temp.ToUnixTimeSeconds();
                    break;
            }
        }

        public override string ToString()
        {
            return $"Timed Reminder: `{Name}` \nContent: {Content} \n`{DateTime.UtcNow}`";
        }

        public TimedReminder( string name, string content, bool repeat, string dateFormat, string date, long expdate = 0 )
        {
            Name = name;
            Content = content;
            Repeat = repeat;

            if ( dateFormat == "-e" )
            {
                Repeat = false;
            }

            Date = date;
            DateFormat = dateFormat;
            StartDate = DateTime.UtcNow;

            if ( expdate != 0 )
            {
                ExpDate = expdate;
                return;
            }

            UpdateExpirationDate();
        }

        public void Inject( GuildConfigManager mgr, ZarnoghState state, ulong guildId )
        {
            this.GuildConfigManager = mgr;
            this.BotState = state;
            this.GuildId = guildId;
        }

        public async Task BotCoreTickAsync( BotCore state, DateTimeOffset fireDate )
        {
            if ( HasExpiredRecently( DateTimeOffset.UtcNow ) )
            {
                var profile = await GuildConfigManager.GetOrCreateGuildConfig( GuildId );
                CommandContext tempContext = await BotState.CreateNewCommandContext( GuildId, profile.BotNotificationsChannel );

                await tempContext.RespondAsync( this.ToString() );
                if ( !Repeat )
                {
                    profile.TimedReminders.Remove( this );
                    state.TickAsync -= BotCoreTickAsync;
                    await tempContext.RespondAsync( $"Timed Reminder not set to repeat, removing it from server reminders list." );
                    await GuildConfigManager.SaveGuildConfigAsync( profile );
                }
                else
                {
                    UpdateExpirationDate();
                    await tempContext.RespondAsync( $"Timed Reminder set to repeat, repeating..\n Next time the reminder will go off at <t:{ExpDate}>." );
                    await GuildConfigManager.SaveGuildConfigAsync( profile );
                }
            }
        }
    }
}