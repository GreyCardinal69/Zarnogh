namespace Zarnogh.Other
{
    public static class Utilities
    {
        public static List<string> GetAllFilesFromFolder( string root, bool searchSubfolders )
        {
            Queue<string> folders = new Queue<string>();
            List<string> files = new List<string>();
            folders.Enqueue( root );
            while ( folders.Count != 0 )
            {
                string currentFolder = folders.Dequeue();
                try
                {
                    string[] filesInCurrent = System.IO.Directory.GetFiles( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                    files.AddRange( filesInCurrent );
                }
                catch { }
                try
                {
                    if ( searchSubfolders )
                    {
                        string[] foldersInCurrent = System.IO.Directory.GetDirectories( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                        foreach ( string _current in foldersInCurrent )
                        {
                            folders.Enqueue( _current );
                        }
                    }
                }
                catch { }
            }
            return files;
        }
    }
}