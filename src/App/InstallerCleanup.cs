using System;
using System.IO;
namespace Stackshot
{
    internal static class InstallerCleanup
    {
        internal static bool IsCommand(string[] args){return Array.Exists(args,delegate(string arg){return arg=="--installer-cleanup"||arg=="--installer-clear-cache"||arg=="--installer-request-exit";});}
        internal static string ExitEvent(string sid,string executable,bool acknowledge=false){using(var sha=System.Security.Cryptography.SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant()))).Replace("-","").Substring(0,24);return "Local\\ScreenCraft-"+(acknowledge?"ExitDone-":"Exit-")+sid+"-"+hash;}}
        internal static void RequestInstalledExit()
        {
            string sid=System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,exe=System.Windows.Forms.Application.ExecutablePath;
            try{using(var done=System.Threading.EventWaitHandle.OpenExisting(ExitEvent(sid,exe,true)))using(var request=System.Threading.EventWaitHandle.OpenExisting(ExitEvent(sid,exe))){request.Set();if(!done.WaitOne(15000))throw new IOException("请关闭 ScreenCraft 的未保存窗口后重试卸载。");}}
            catch(System.Threading.WaitHandleCannotBeOpenedException){/* This installed path is not running; portable copies are untouched. */}
        }
        internal static void RemoveOwnedStartup(IStartupEntry entry,string installedExe)
        {
            string value=entry.Read();if(StartupCommand.Matches(value,installedExe))entry.Remove();
        }
        internal static void ClearOwnedCache(string dataRoot)
        {
            // Deliberately only generated history PNGs/index. Never recursively delete user data.
            var history=Path.Combine(Path.GetFullPath(dataRoot),"History");if(!FilePaths.DirectoryExists(history))return;
            foreach(var path in Directory.GetFiles(FilePaths.Native(history),"*",SearchOption.TopDirectoryOnly)){
                string file=Path.GetFileName(path);Guid id;
                if((file.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&Guid.TryParseExact(Path.GetFileNameWithoutExtension(file),"N",out id))||file=="index.json")File.Delete(path);
            }
        }
        internal static bool Handle(string[] args)
        {
            if(Array.IndexOf(args,"--installer-request-exit")>=0){RequestInstalledExit();return true;}
            if(Array.IndexOf(args,"--installer-cleanup")>=0){RemoveOwnedStartup(new UserStartupEntry(),System.Windows.Forms.Application.ExecutablePath);return true;}
            if(Array.IndexOf(args,"--installer-clear-cache")>=0){ClearOwnedCache(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenCraft"));return true;}
            return false;
        }
    }
}
