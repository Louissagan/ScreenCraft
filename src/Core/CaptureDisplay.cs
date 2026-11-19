using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenCraft
{
    // ShowDialog exposes an HWND before VisibleChanged/Shown paint it. Hold the
    // compositor surface until the complete first frame has reached GDI and DWM.
    public abstract class CaptureSurfaceForm : Form
    {
        bool cloaked;

        protected override void OnHandleCreated(EventArgs e)
        {
            cloaked = false;
            CaptureDisplay.Prepare(Handle);
            CaptureDisplay.ExcludeFromCapture(Handle);
            base.OnHandleCreated(e);
        }

        protected abstract void PaintOpeningFrame();

        protected override void SetVisibleCore(bool value)
        {
            bool opening = value && !Visible;
            if (opening) cloaked = CaptureDisplay.SetCloak(Handle, true) >= 0;
            base.SetVisibleCore(value);
            if (!opening || !Visible || !IsHandleCreated) return;

            PaintOpeningFrame();
            Native.GdiFlush();
            if (cloaked)
            {
                CaptureInterop.DwmFlush();
                Marshal.ThrowExceptionForHR(CaptureDisplay.SetCloak(Handle, false));
                cloaked = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) CloakForExit();
        }

        internal void CloakForExit()
        {
            if (!cloaked && IsHandleCreated)
            {
                // Keep the last valid frame through cancelled/failed commands.
                // Once close is accepted, remove the entire tree from composition
                // before WinForms tears down its children or reactivates its owner.
                cloaked = CaptureDisplay.SetCloak(Handle, true) >= 0;
                if (cloaked) CaptureInterop.DwmFlush();
            }
        }
    }

    // Only capture surfaces opt out of DWM transitions; system animation settings
    // and ordinary settings/editor windows are left alone.
    internal static class CaptureDisplay
    {
        // Native probes replace this boundary before creating their synthetic HWNDs.
        // Toggling display affinity after showing a window contaminates screen samples.
        internal static Action<IntPtr> ExcludeFromCapture=delegate(IntPtr window){Native.SetWindowDisplayAffinity(window,0x11);};
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        const int DwmwaCloak = 13;
        internal static int SetCloak(IntPtr window, bool hidden)
        {
            int value = hidden ? 1 : 0;
            return DwmSetWindowAttribute(window, DwmwaCloak, ref value, sizeof(int));
        }
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
