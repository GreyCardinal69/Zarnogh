namespace Zarnogh.Services
{
    internal static class Logger
    {
        private static void LogMessage( string message, ConsoleColor logColor )
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = logColor;
            Console.WriteLine( message );
            Console.ForegroundColor = previousColor;
        }

        private static void LogMessage( ColorableMessageBuilder messageBuilder )
        {
            foreach ( var segment in messageBuilder.Segments )
            {
                // ( string, color )
                Console.ForegroundColor = segment.Item2;
                Console.Write( segment.Item1 );
            }
            Console.WriteLine();
        }

        public static void LogError( string message ) => LogMessage( message, ConsoleColor.Red );
        public static void LogWarning( string message ) => LogMessage( message, ConsoleColor.Yellow );
        public static void LogMessage( string message ) => Console.WriteLine( message );
        public static void LogColorableBuilderMessage( ColorableMessageBuilder messageBuilder ) => LogMessage( messageBuilder );
    }
}