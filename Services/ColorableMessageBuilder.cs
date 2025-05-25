namespace Zarnogh.Services
{
    internal class ColorableMessageBuilder
    {
        private List<(string text, ConsoleColor color)> _segments = new List<(string text, ConsoleColor color)> ();
        private ConsoleColor _defaultColor;

        public IReadOnlyList<(string, ConsoleColor)> Segments => _segments;

        public ColorableMessageBuilder( ConsoleColor defaultColor )
        {
            _defaultColor = defaultColor;
        }

        public ColorableMessageBuilder Append( string text )
        {
            _segments.Add( (text, _defaultColor) );
            return this;
        }

        public ColorableMessageBuilder AppendHighlight( string text, ConsoleColor color )
        {
            _segments.Add( (text, color) );
            return this;
        }
    }
}