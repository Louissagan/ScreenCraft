using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScreenCraft
{
    // Owns one top-down DIB, its selected memory DC, and a borrowed GDI+ view.
    // Destruction follows the inverse order of acquisition, including failed construction.
    public sealed class Dib : IDisposable
    {
        public readonly int Width,Height;
        public readonly IntPtr Dc,Bits;
        public readonly Bitmap Bitmap;
        IntPtr allocation,previous;
        bool disposed;
        public Dib(int width,int height):this(width,height,false) { }
        public Dib(int width,int height,bool premultiplied)
        {
            Width=Math.Max(1,width);Height=Math.Max(1,height);
            if((long)Width*Height>64000000)throw new ArgumentOutOfRangeException("width","图像超过 6400 万像素。");
            try{
                Dc=Native.CreateCompatibleDC(IntPtr.Zero);if(Dc==IntPtr.Zero)throw new Win32Exception();
                var header=new Native.BITMAPINFOHEADER();header.biSize=Marshal.SizeOf(header);header.biWidth=Width;header.biHeight=-Height;header.biPlanes=1;header.biBitCount=32;
                IntPtr pixels;allocation=Native.CreateDIBSection(Dc,ref header,0,out pixels,IntPtr.Zero,0);Bits=pixels;
                if(allocation==IntPtr.Zero||Bits==IntPtr.Zero)throw new Win32Exception();
                previous=Native.SelectObject(Dc,allocation);if(previous==IntPtr.Zero||previous==new IntPtr(-1))throw new Win32Exception();
                Bitmap=new Bitmap(Width,Height,checked(Width*4),premultiplied?PixelFormat.Format32bppPArgb:PixelFormat.Format32bppRgb,Bits);
            }catch{Dispose();throw;}
        }
        void CheckAlive(){if(disposed)throw new ObjectDisposedException("Dib");}
        public Graphics Graphics(){CheckAlive();Native.GdiFlush();return System.Drawing.Graphics.FromImage(Bitmap);}
        public Graphics DcGraphics(){CheckAlive();Native.GdiFlush();return System.Drawing.Graphics.FromHdc(Dc);}
        public static Dib FromScreen(Rectangle area)
        {
            if(area.Width<1||area.Height<1)throw new ArgumentException("截图区域不能为空。","area");
            var surface=new Dib(area.Width,area.Height);IntPtr desktop=IntPtr.Zero;
            try{desktop=Native.GetDC(IntPtr.Zero);if(desktop==IntPtr.Zero||!Native.BitBlt(surface.Dc,0,0,area.Width,area.Height,desktop,area.X,area.Y,Native.SRCCOPY))throw new Win32Exception();Native.GdiFlush();return surface;}
            catch{surface.Dispose();throw;}finally{if(desktop!=IntPtr.Zero)Native.ReleaseDC(IntPtr.Zero,desktop);}
        }
        public Bitmap ToBitmap()
        {
            CheckAlive();Native.GdiFlush();
            var result=new Bitmap(Width,Height,PixelFormat.Format32bppRgb);
            try{using(var g=System.Drawing.Graphics.FromImage(result)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(Bitmap,0,0);}return result;}catch{result.Dispose();throw;}
        }
        public void Darken(byte opacity)
        {
            using(var g=Graphics())using(var brush=new SolidBrush(Color.FromArgb(opacity,Color.Black)))g.FillRectangle(brush,0,0,Width,Height);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(Bitmap!=null)Bitmap.Dispose();
            if(Dc!=IntPtr.Zero){if(previous!=IntPtr.Zero&&previous!=new IntPtr(-1))Native.SelectObject(Dc,previous);Native.DeleteDC(Dc);}
            if(allocation!=IntPtr.Zero){Native.DeleteObject(allocation);allocation=IntPtr.Zero;}
        }
    }

    public static class Grabber
    {
        public sealed class Win { public IntPtr Handle;public Rectangle Bounds;public string Name; }
        public static Bitmap Grab(Rectangle area) { using(var surface=Dib.FromScreen(area))return surface.ToBitmap(); }
        public static Rectangle Bounds(IntPtr window)
        {
            Native.RECT bounds;
            if(Native.DwmGetRect(window,9,out bounds,Marshal.SizeOf(typeof(Native.RECT)))!=0 && !Native.GetWindowRect(window,out bounds))return Rectangle.Empty;
            return Rectangle.FromLTRB(bounds.Left,bounds.Top,bounds.Right,bounds.Bottom);
        }
        public static string ProcessName(IntPtr window)
        {
            try{uint pid;Native.GetWindowPid(window,out pid);using(var process=Process.GetProcessById((int)pid))return process.ProcessName;}catch(ArgumentException){return null;}catch(InvalidOperationException){return null;}catch(Win32Exception){return null;}
        }
        public static List<Win> Windows()
        {
            var windows=new List<Win>();uint own;using(var process=Process.GetCurrentProcess())own=(uint)process.Id;
            Native.EnumWindows(delegate(IntPtr handle,IntPtr unused){
                if(!Native.IsWindowVisible(handle)||Native.IsIconic(handle))return true;
                int cloaked;if(Native.DwmGetInt(handle,14,out cloaked,4)==0&&cloaked!=0)return true;
                int style=Native.GetWindowLong(handle,-20);if((style&0x20)!=0||((style&0x80)!=0&&(style&0x40000)==0))return true;
                uint pid,affinity;Native.GetWindowPid(handle,out pid);if(pid==own&&Native.GetWindowDisplayAffinity(handle,out affinity)&&affinity!=0)return true;
                var className=new StringBuilder(256);Native.GetClassName(handle,className,className.Capacity);if(className.ToString()=="Progman"||className.ToString()=="WorkerW")return true;
                var area=Bounds(handle);if(area.Width>=40&&area.Height>=30)windows.Add(new Win{Handle=handle,Bounds=area});return true;
            },IntPtr.Zero);return windows;
        }
        public static Screen CurrentScreen(){return Screen.FromPoint(Control.MousePosition);}
        public static Bitmap RoundCorners(Bitmap source,float radius)
        {
            if(radius<1)return source;var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
            try{using(var g=System.Drawing.Graphics.FromImage(result))using(var path=Theme.Round(new RectangleF(PointF.Empty,source.Size),radius))using(var texture=new TextureBrush(source,WrapMode.Clamp)){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.FillPath(texture,path);}source.Dispose();return result;}
            catch{result.Dispose();throw;}
        }
        public static Native.CURSORINFO CursorNow()
        {
            var info=new Native.CURSORINFO();info.cbSize=Marshal.SizeOf(info);if(!Native.GetCursorInfo(ref info))info.flags=0;return info;
        }
        public static void DrawCursor(IntPtr dc,int originX,int originY){DrawCursor(dc,originX,originY,CursorNow());}
        public static void DrawCursor(IntPtr dc,int originX,int originY,Native.CURSORINFO cursor)
        {
            if((cursor.flags&1)==0||cursor.hCursor==IntPtr.Zero)return;int hotX=0,hotY=0;Native.ICONINFO icon;
            if(Native.GetIconInfo(cursor.hCursor,out icon)){
                hotX=icon.xHotspot;hotY=icon.yHotspot;
                if(icon.hbmColor!=IntPtr.Zero)Native.DeleteObject(icon.hbmColor);if(icon.hbmMask!=IntPtr.Zero)Native.DeleteObject(icon.hbmMask);
            }
            Native.DrawIconEx(dc,cursor.ptScreenPos.X-originX-hotX,cursor.ptScreenPos.Y-originY-hotY,cursor.hCursor,0,0,0,IntPtr.Zero,3);
        }
    }
}
