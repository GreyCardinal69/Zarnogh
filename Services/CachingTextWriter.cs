using System.Text;

namespace Zarnogh.Services
{
    public class CachingTextWriter : TextWriter
    {
        private readonly TextWriter _originalOutput;
        private readonly List<string> _cachedLines = new List<string>();
        private readonly object _cacheLock = new object();
        private readonly Func<int> _getMaxLines;

        public CachingTextWriter( TextWriter originalOutput, Func<int> getMaxLinesFunc )
        {
            _originalOutput = originalOutput ?? throw new ArgumentNullException( nameof( originalOutput ) );
            _getMaxLines = getMaxLinesFunc ?? throw new ArgumentNullException( nameof( getMaxLinesFunc ) );
        }

        public override Encoding Encoding => _originalOutput.Encoding;

        public override void WriteLine( string value )
        {
            lock ( _cacheLock )
            {
                _originalOutput.WriteLine( value );
                Cache( $"{value}{Environment.NewLine}" );
            }
        }

        public override void Write( char value )
        {
            lock ( _cacheLock )
            {
                _originalOutput.Write( value );
                Cache( value.ToString() );
            }
        }

        public override void Write( string value )
        {
            if ( value == null ) return;
            lock ( _cacheLock )
            {
                _originalOutput.Write( value );
                Cache( value.ToString() );
            }
        }

        public override void WriteLine()
        {
            lock ( _cacheLock )
            {
                _originalOutput.WriteLine();
                Cache( Environment.NewLine );
            }
        }

        private void Cache( string line )
        {
            _cachedLines.Add( line );
            TrimCache();
        }

        private void TrimCache()
        {
            int maxLines = _getMaxLines();
            while ( _cachedLines.Count > maxLines && maxLines > 0 )
            {
                _cachedLines.RemoveAt( 0 );
            }
        }

        public IReadOnlyList<string> GetLastNLines( int n )
        {
            lock ( _cacheLock )
            {
                if ( n <= 0 ) return Array.Empty<string>();
                int count = Math.Min(n, _cachedLines.Count);
                return _cachedLines.Skip( _cachedLines.Count - count ).ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<string> GetFirstNLines( int n )
        {
            lock ( _cacheLock )
            {
                if ( n <= 0 ) return Array.Empty<string>();
                int count = Math.Min(n, _cachedLines.Count);
                return _cachedLines.Take( count ).ToList().AsReadOnly();
            }
        }

        public void ClearConsoleCache() => _cachedLines.Clear();

        public IReadOnlyList<string> GetAllLines()
        {
            lock ( _cacheLock )
            {
                return _cachedLines.ToList().AsReadOnly();
            }
        }

        public override void Flush()
        {
            _originalOutput.Flush();
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                Flush();
            }
            base.Dispose( disposing );
        }
    }
}