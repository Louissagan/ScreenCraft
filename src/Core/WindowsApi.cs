using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenCraft
{
    // Windows SDK ABI declarations. Application behavior lives outside this boundary.
    public static class Native
    {
        public const int SRCCOPY=0x00CC0020;
        [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X,Y;}
        [StructLayout(LayoutKind.Sequential)] public struct SIZE {public int cx,cy;}
        [StructLayout(LayoutKind.Sequential)] public struct RECT {public int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)] public struct CURSORINFO {public int cbSize,flags;public IntPtr hCursor;public POINT ptScreenPos;}
        [StructLayout(LayoutKind.Sequential)] public struct ICONINFO {[MarshalAs(UnmanagedType.Bool)]public bool fIcon;public int xHotspot,yHotspot;public IntPtr hbmMask,hbmColor;}
        [StructLayout(LayoutKind.Sequential)] public struct BITMAPINFOHEADER {public int biSize,biWidth,biHeight;public short biPlanes,biBitCount;public int biCompression,biSizeImage,biXPelsPerMeter,biYPelsPerMeter,biClrUsed,biClrImportant;}
        [return:MarshalAs(UnmanagedType.Bool)] public delegate bool EnumWindowsProc(IntPtr window,IntPtr context);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool EnumWindows(EnumWindowsProc callback,IntPtr context);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool GetWindowRect(IntPtr window,out RECT bounds);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool GetClientRect(IntPtr window,out RECT bounds);
        [DllImport("user32.dll",EntryPoint="GetWindowLongW",SetLastError=true)] public static extern int GetWindowLong(IntPtr window,int index);
        [DllImport("user32.dll",EntryPoint="SetWindowLongW",SetLastError=true)] public static extern int SetWindowLong(IntPtr window,int index,int value);
        [DllImport("user32.dll",EntryPoint="GetClassNameW",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window,StringBuilder buffer,int capacity);
        [DllImport("user32.dll",EntryPoint="GetWindowThreadProcessId")] public static extern uint GetWindowPid(IntPtr window,out uint processId);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool GetWindowDisplayAffinity(IntPtr window,out uint affinity);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT point,uint flags);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr monitor,int kind,out uint x,out uint y);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool SetProcessDpiAwarenessContext(IntPtr awareness);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool UnregisterHotKey(IntPtr window,int id);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll",EntryPoint="SendMessageW")] public static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam);
        [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] public static extern int DwmGetRect(IntPtr window,int attribute,out RECT value,int size);
        [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] public static extern int DwmGetInt(IntPtr window,int attribute,out int value,int size);
        [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr window,IntPtr dc);
        [DllImport("gdi32.dll",SetLastError=true)] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll",SetLastError=true)] public static extern IntPtr CreateDIBSection(IntPtr dc,ref BITMAPINFOHEADER header,uint usage,out IntPtr pixels,IntPtr section,uint offset);
        [DllImport("gdi32.dll",SetLastError=true)] public static extern IntPtr SelectObject(IntPtr dc,IntPtr resource);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr resource);
        [DllImport("gdi32.dll",SetLastError=true)] public static extern bool BitBlt(IntPtr target,int x,int y,int width,int height,IntPtr source,int sourceX,int sourceY,int operation);
        [DllImport("gdi32.dll")] public static extern bool GdiFlush();
        [DllImport("user32.dll")] public static extern bool GetCursorInfo(ref CURSORINFO info);
        [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr icon,out ICONINFO info);
        [DllImport("user32.dll")] public static extern bool DrawIconEx(IntPtr dc,int x,int y,IntPtr icon,int width,int height,uint step,IntPtr brush,uint flags);
    }
}
