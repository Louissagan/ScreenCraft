using System;
using System.ComponentModel;
using System.Drawing;

namespace ScreenCraft
{
    // Compose before a capture HWND is shown. WM_PAINT only transfers a complete
    // opaque frame; allocation, scene composition and text layout stay outside it.
    internal sealed class PreparedCaptureFrame:IDisposable
    {
        readonly Dib pixels;
        internal int Revision{get;private set;}
        internal PreparedCaptureFrame(Size size){pixels=new Dib(size.Width,size.Height);}
        internal void Compose(Action<Graphics> paint){using(var graphics=pixels.Graphics())paint(graphics);Revision++;}
        internal void Paint(Graphics graphics,Rectangle requested)
        {
            var clip=Rectangle.Intersect(requested,new Rectangle(0,0,pixels.Width,pixels.Height));if(clip.Width<1||clip.Height<1)return;
            IntPtr target=graphics.GetHdc();try{Copy(target,clip);}finally{graphics.ReleaseHdc(target);}
        }
        internal void Present(IntPtr window)
        {
            IntPtr target=Native.GetDC(window);if(target==IntPtr.Zero)throw new Win32Exception();try{Copy(target,new Rectangle(0,0,pixels.Width,pixels.Height));}finally{Native.ReleaseDC(window,target);}
        }
        void Copy(IntPtr target,Rectangle clip){Native.GdiFlush();if(!Native.BitBlt(target,clip.X,clip.Y,clip.Width,clip.Height,pixels.Dc,clip.X,clip.Y,Native.SRCCOPY))throw new Win32Exception();}
        public void Dispose(){pixels.Dispose();}
    }
}
