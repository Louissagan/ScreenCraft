using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    public partial class RegionPicker
    {
        bool annotationHandoff;Bitmap annotationDesktop;
        Action<Bitmap,Rectangle,Rectangle,IWin32Window> annotationEditor;Exception annotationError;
        public static void PickAndAnnotate(Action<Bitmap,Rectangle,Rectangle,IWin32Window> editor)
        {
            var bounds=SystemInformation.VirtualScreen;var grab=Dib.FromScreen(bounds);if(CaptureHost.Running!=null&&CaptureHost.Running.ColorPreferences!=null&&CaptureHost.Running.ColorPreferences.IncludeCursor)Grabber.DrawCursor(grab.Dc,bounds.X,bounds.Y);Exception failure;
            using(var picker=new RegionPicker(grab,bounds,Grabber.Windows(),Mode.Image)){picker.annotationHandoff=true;picker.annotationEditor=editor;picker.ShowDialog();failure=picker.annotationError;}
            if(failure!=null)throw new InvalidOperationException("截图编辑无法打开："+failure.Message,failure);
        }
        public static Rectangle PickForAnnotation(out Bitmap frozenDesktop,out Rectangle desktopBounds)
        {
            frozenDesktop=null;desktopBounds=SystemInformation.VirtualScreen;Dib grab=Dib.FromScreen(desktopBounds);
            using(var picker=new RegionPicker(grab,desktopBounds,Grabber.Windows(),Mode.Image)){picker.annotationHandoff=true;picker.ShowDialog();frozenDesktop=picker.annotationDesktop;picker.annotationDesktop=null;return picker.result;}
        }
    }
}
