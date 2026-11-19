using System;
using System.Diagnostics;
using System.IO;

namespace ScreenCraft
{
    internal static class StartupFeedback
    {
        internal static void Send(Action<string,string> systemNotice,string captureKey,bool alreadyRunning)
        {
            string message=string.IsNullOrEmpty(captureKey)?"左键托盘图标截图，右键打开设置。":"按 "+captureKey+" 截图；右键托盘图标打开设置。";
            systemNotice(alreadyRunning?"ScreenCraft 已在运行":"ScreenCraft 已启动",message);
        }
    }
    public static class StartupRepair
    {
        static Version VersionOf(string path)
        {
            if(!File.Exists(path))return new Version(0,0);
            var info=FileVersionInfo.GetVersionInfo(path);return new Version(Math.Max(0,info.FileMajorPart),Math.Max(0,info.FileMinorPart),Math.Max(0,info.FileBuildPart),Math.Max(0,info.FilePrivatePart));
        }
        public static bool RepairMovedEntry(IStartupEntry entry,string currentExe){return RepairMovedEntry(entry,currentExe,VersionOf);}
        internal static bool RepairMovedEntry(IStartupEntry entry,string currentExe,Func<string,Version> versionOf)
        {
            string command=entry.Read();if(string.IsNullOrWhiteSpace(command)||StartupCommand.Matches(command,currentExe))return false;
            string text=command.Trim();if(!text.StartsWith("\"",StringComparison.Ordinal))return false;
            int end=text.IndexOf('"',1);if(end<2||!string.Equals(text.Substring(end+1).Trim(),"--background",StringComparison.OrdinalIgnoreCase))return false;
            string previous=text.Substring(1,end-1);
            if(!Path.IsPathRooted(previous)||!string.Equals(Path.GetFileName(previous),"ScreenCraft.exe",StringComparison.OrdinalIgnoreCase)||!StartupCommand.Matches(text,previous))return false;
            // Preserve an existing enabled startup entry, but never enable a missing
            // entry or replace a newer program with an older build.
            if(versionOf(currentExe).CompareTo(versionOf(previous))<0)return false;
            entry.Write(StartupCommand.For(currentExe));return true;
        }
    }
}
