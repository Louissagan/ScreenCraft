using System;
using System.IO;
namespace ScreenCraft
{
    // Managed file streams plus explicit extended paths avoid GDI+/Win32 MAX_PATH checks.
    // No machine-wide long-path policy or registry setting is changed.
    internal static class FilePaths
    {
        internal static string Native(string path){string full=Path.GetFullPath(path);if(full.StartsWith(@"\\?\",StringComparison.Ordinal)||full.Length<248)return full;return full.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+full.Substring(2):@"\\?\"+full;}
        internal static void EnsureDirectory(string path){Directory.CreateDirectory(Native(path));}
        internal static bool Exists(string path){return File.Exists(Native(path));}
        internal static bool DirectoryExists(string path){return Directory.Exists(Native(path));}
        internal static FileStream Read(string path){return new FileStream(Native(path),FileMode.Open,FileAccess.Read,FileShare.Read);}
        internal static FileStream Create(string path){return new FileStream(Native(path),FileMode.CreateNew,FileAccess.Write,FileShare.None);}
        internal static void Commit(string temporary,string destination,bool replace){if(replace&&Exists(destination))File.Replace(Native(temporary),Native(destination),null);else File.Move(Native(temporary),Native(destination));}
        internal static void Delete(string path){File.Delete(Native(path));}
    }
}
