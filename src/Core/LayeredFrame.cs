using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenCraft
{
    internal static class LayeredFrame
    {
        [StructLayout(LayoutKind.Sequential)] struct Blend {internal byte Operation,Flags,Alpha,Format;}
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr window,IntPtr desktop,ref Native.POINT position,ref Native.SIZE size,IntPtr pixels,ref Native.POINT origin,uint key,ref Blend blend,uint flags);
        internal static void Present(IntPtr window,Dib image,Point location,byte opacity)
        {
            var position=new Native.POINT{X=location.X,Y=location.Y};var origin=new Native.POINT();var size=new Native.SIZE{cx=image.Width,cy=image.Height};var blend=new Blend{Alpha=opacity,Format=1};
            Native.GdiFlush();if(!UpdateLayeredWindow(window,IntPtr.Zero,ref position,ref size,image.Dc,ref origin,0,ref blend,2))throw new Win32Exception();
        }
    }
}
