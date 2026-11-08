using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Stackshot
{
    public static class StartupPolicy
    {
        public static bool OpensSettings(string[] arguments){return arguments!=null&&Array.Exists(arguments,delegate(string arg){return string.Equals(arg,"--settings",StringComparison.OrdinalIgnoreCase);});}
        public static bool ShowsNotice(string[] arguments,Preferences preferences){return !OpensSettings(arguments)&&preferences.ShowStartupNotice;}
        public static string WakeEvent(string sid,bool settings){return "Local\\ScreenCraft-"+(settings?"Settings-":"Open-")+sid;}
        public static void Dispatch(bool settings,Action openSettings,Action notifyRunning){if(settings)openSettings();else notifyRunning();}
    }
    // The application lifetime has no MainForm. Application.Run therefore never shows a launcher.
    internal sealed class TrayApplicationContext:ApplicationContext
    {
        readonly Form controller;
        internal TrayApplicationContext(Form owner){controller=owner;controller.FormClosed+=ControllerClosed;}
        void ControllerClosed(object sender,FormClosedEventArgs e){ExitThread();}
        protected override void Dispose(bool disposing){if(disposing){controller.FormClosed-=ControllerClosed;controller.Dispose();}base.Dispose(disposing);}
    }
    internal static class TrayBrand
    {
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);
        public static Icon Create()
        {
            using(var stream=typeof(TrayBrand).Assembly.GetManifestResourceStream("ScreenCraft.AppIcon.ico"))using(var icon=new Icon(stream,new Size(32,32)))return (Icon)icon.Clone();
        }
    }
}
