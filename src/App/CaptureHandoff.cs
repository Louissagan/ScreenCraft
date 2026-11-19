using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenCraft
{
    public sealed partial class CaptureOverlay
    {
        PreparedCaptureFrame firstFrame;
        internal bool FirstFramePrepared{get{return firstFrame!=null;}}
        internal bool InitialPixelsPresented{get;private set;}
        void PrepareFirstFrame()
        {
            if(firstFrame==null)firstFrame=new PreparedCaptureFrame(desktop.Size);
            firstFrame.Compose(delegate(Graphics graphics){canvas.PaintCaptureBackdrop(graphics);PaintRangeChrome(graphics);});
        }
        void DropFirstFrame(){if(firstFrame!=null){firstFrame.Dispose();firstFrame=null;}}
        protected override void PaintOpeningFrame()
        {
            if(firstFrame!=null){firstFrame.Present(Handle);firstFrame.Present(canvas.Handle);}
            Refresh();
            InitialPixelsPresented=true;
        }
        internal DialogResult ShowForCapture(IWin32Window previous)
        {
            // Child HWNDs must exist while the top-level surface is still hidden.
            // Creating the full-screen canvas after ShowWindow exposes its empty DC.
            CaptureDisplay.PrepareHandles(this);
            var picker=previous as RegionPicker;
            // ShowDialog can activate its previous owner again on return, even
            // after RetireForAnnotation hid that HWND and released its backing.
            FormClosedEventHandler closing=delegate{if(picker!=null&&!picker.IsDisposed)picker.CloakForExit();};
            EventHandler ready=delegate{
                CaptureDisplay.PresentThenRetire(
                    delegate{Update();},
                    delegate{CaptureInterop.DwmFlush();},
                    delegate{if(picker!=null&&!picker.IsDisposed)picker.RetireForAnnotation();});
                DropFirstFrame();
            };
            Shown+=ready;
            FormClosed+=closing;
            try{return previous==null?ShowDialog():ShowDialog(previous);}
            finally{Shown-=ready;FormClosed-=closing;}
        }
    }
}
