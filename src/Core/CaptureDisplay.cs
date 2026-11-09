using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenCraft
{
    // Only capture surfaces opt out of DWM transitions; system animation settings
    // and ordinary settings/editor windows are left alone.
    internal static class CaptureDisplay
    {
        // Native probes replace this boundary before creating their synthetic HWNDs.
        // Toggling display affinity after showing a window contaminates screen samples.
        internal static Action<IntPtr> ExcludeFromCapture=delegate(IntPtr window){Native.SetWindowDisplayAffinity(window,0x11);};
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        internal static int Prepare(IntPtr window)
        {
            int disabled=1;
            return DwmSetWindowAttribute(window,3,ref disabled,sizeof(int));
        }
        internal static void PrepareHandles(Control root)
        {
            var handle=root.Handle;
            foreach(Control child in root.Controls)PrepareHandles(child);
        }
        internal static void PresentThenRetire(Action present,Action synchronize,Action retire)
        {
            present();synchronize();retire();
        }
    }
}
