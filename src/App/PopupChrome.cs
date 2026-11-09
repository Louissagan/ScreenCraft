using System;
using System.Runtime.InteropServices;
namespace ScreenCraft
{
    internal static class PopupChrome
    {
        // https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int bytes);
        internal static bool Apply(IntPtr window){try{int smallCorners=3,noBorder=unchecked((int)0xFFFFFFFE);int result=DwmSetWindowAttribute(window,33,ref smallCorners,4);DwmSetWindowAttribute(window,34,ref noBorder,4);return result==0;}catch(EntryPointNotFoundException){return false;}catch(DllNotFoundException){return false;}}
    }
}
