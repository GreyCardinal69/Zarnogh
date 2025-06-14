using DSharpPlus.CommandsNext;

namespace Zarnogh.Services
{
    internal static class Logger
    {
        private static CachingTextWriter _cachingConsoleWriter;
        private static TextWriter _originalConsoleOut;
        private static readonly object _logLock = new object();

        public static int MaxCachedLines { get; set; } = 1000;

        static Logger()
        {
            _originalConsoleOut = Console.Out;
            _cachingConsoleWriter = new CachingTextWriter( _originalConsoleOut, () => MaxCachedLines );
            Console.SetOut( _cachingConsoleWriter );
        }

        private static void LogMessage( string message, ConsoleColor logColor )
        {
            lock ( _logLock )
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = logColor;
                Console.WriteLine( message );
                Console.ForegroundColor = previousColor;
            }
        }

        public static void ClearConsoleCache() => _cachingConsoleWriter.ClearConsoleCache();

        public static string GetConsoleLines( CommandContext ctx, int count )
        {
            return $"```\n{string.Join( "", _cachingConsoleWriter.GetLastNLines( count ) )}\n```";
        }

        private static void LogMessage( ColorableMessageBuilder messageBuilder )
        {
            lock ( _logLock )
            {
                var previousColor = Console.ForegroundColor;
                foreach ( var segment in messageBuilder.Segments )
                {
                    Console.ForegroundColor = segment.Item2;
                    Console.Write( segment.Item1 );
                }
                Console.WriteLine();
                Console.ForegroundColor = previousColor;
            }
        }

        public static void LogError( string message ) => LogMessage( message, ConsoleColor.Red );
        public static void LogWarning( string message ) => LogMessage( message, ConsoleColor.Yellow );
        public static void LogMessage( string message ) => LogMessage( message, Console.ForegroundColor );
        public static void LogColorableBuilderMessage( ColorableMessageBuilder messageBuilder ) => LogMessage( messageBuilder );
    }
}