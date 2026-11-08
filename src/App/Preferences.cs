using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Win32;
namespace Stackshot
{
    public sealed partial class Preferences
    {
        public int Version=1;
        public string Capture="Ctrl+Alt+A",Ocr="Ctrl+Shift+O",Paste="Ctrl+Alt+V",Color="Ctrl+Shift+C",Scroll="Ctrl+Alt+L";
        public string OcrLanguage="auto",ColorFormat="RGB";
        public bool ColorUpper=true;
        public int SelectionDimPercent=SelectionAppearance.DefaultDimPercent;
        public int SelectionBorderWidth=SelectionAppearance.DefaultBorderWidth;
        public string SelectionBorderColor="blue",ToolbarPlacement="bottom";
        public bool ShowStartupNotice=true;
        public bool ShowCaptureGuides=true;
        public bool RunOnStartup;
        public ToolbarSettings Toolbar=new ToolbarSettings();
        public Dictionary<string,string> Hotkeys(){return new Dictionary<string,string>{{"capture",Capture??""},{"ocr",Ocr??""},{"paste",Paste??""},{"color",Color??""},{"scroll",Scroll??""},{"fullscreen",FullScreen??""},{"window",ActiveWindow??""},{"repeat",RepeatRegion??""},{"togglepins",TogglePins??""},{"board",Board??""}};}
    }
    public sealed class PreferenceStore
    {
        public readonly string DirectoryPath;
        public string Warning;
        public string FilePath{get{return Path.Combine(DirectoryPath,"settings.json");}}
        public PreferenceStore(string root){DirectoryPath=root;}
        public static PreferenceStore Select(string requested,string fallback){if(Writable(requested))return new PreferenceStore(requested);if(Writable(fallback))return new PreferenceStore(fallback){Warning="便携目录不可写，已使用当前用户数据目录；本次不是纯便携模式。"};throw new IOException("配置目录不可写，请将程序移到可写目录。");}
        static bool Writable(string path){try{FilePaths.EnsureDirectory(path);string probe=Path.Combine(path,".write-"+Guid.NewGuid().ToString("N"));using(var file=new FileStream(FilePaths.Native(probe),FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.DeleteOnClose)){}return true;}catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}}
        public Preferences Load()
        {
            if(!FilePaths.Exists(FilePath))return new Preferences();
            try{var info=new FileInfo(FilePaths.Native(FilePath));if(info.Length>65536)throw new InvalidDataException();var p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(FilePaths.Native(FilePath)));if(p==null||p.Version!=1||p.ValidateDaily()!=null)throw new InvalidDataException();return p;}
            catch(Exception ex){if(!(ex is IOException)&&!(ex is InvalidDataException)&&!(ex is ArgumentException)&&!(ex is InvalidOperationException))throw;Warning="设置文件损坏，已加载默认值。原文件已保留。";return new Preferences();}
        }
        public void Save(Preferences preferences)
        {
            FilePaths.EnsureDirectory(DirectoryPath);string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(FilePaths.Native(temp),new JavaScriptSerializer().Serialize(preferences));FilePaths.Commit(temp,FilePath,true);}finally{if(FilePaths.Exists(temp))FilePaths.Delete(temp);}
        }
    }
    public interface IStartupEntry {string Read();void Write(string command);void Remove();}
    public sealed class UserStartupEntry:IStartupEntry
    {
        const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
        public string Read(){using(var k=Registry.CurrentUser.OpenSubKey(Key))return k==null?null:k.GetValue("ScreenCraft") as string;}
        public void Write(string command){using(var k=Registry.CurrentUser.CreateSubKey(Key))k.SetValue("ScreenCraft",command,RegistryValueKind.String);}
        public void Remove(){using(var k=Registry.CurrentUser.OpenSubKey(Key,true))if(k!=null)k.DeleteValue("ScreenCraft",false);}
    }
    public static class StartupCommand
    {
        public static bool Matches(string command,string exe){return string.Equals(command,For(exe),StringComparison.OrdinalIgnoreCase);}
        public static string For(string exe){return "\""+Path.GetFullPath(exe)+"\" --background";}
        public static void Apply(IStartupEntry adapter,bool enabled,string exe){if(enabled)adapter.Write(For(exe));else adapter.Remove();}
    }
}
