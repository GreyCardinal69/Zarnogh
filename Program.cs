using Newtonsoft.Json;
using Zarnogh.Configuration;
using Zarnogh.Services;

namespace Zarnogh
{
    public class Program
    {
        private static readonly string _configPath = $"{Directory.GetCurrentDirectory()}\\Config.json";

        static async Task Main( string[] args )
        {
            Logger.LogMessage( "Starting up..." );

            BotConfig botConfig = LoadGlobalConfig();

            var bot = new BotCore();
            try
            {
                await bot.InitializeAsync( botConfig );
                Logger.LogMessage( "Bot initialized. Press Ctrl+C to shut down." );
                await Task.Delay( -1 );
            }
            catch ( Exception ex )
            {
                Logger.LogError( $"An unhandled exception occurred during bot startup or runtime: {ex}" );
            }
            finally
            {
                Logger.LogMessage( "Shutting down..." );
                await bot.ShutdownAsync();
                Logger.LogMessage( "Press any key to exit." );
                Console.ReadKey();
            }
        }

        private static BotConfig LoadGlobalConfig()
        {
            if ( !File.Exists( _configPath ) )
            {
                Logger.LogError( "Bot config file not found, aborting..." );
                throw new FileNotFoundException();
            }

            var json = File.ReadAllText( _configPath );
            return JsonConvert.DeserializeObject<BotConfig>( json );
        }
    }
}