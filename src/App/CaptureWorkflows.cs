using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    internal static class CaptureWorkflows
    {
        internal static Rectangle Resolve(string action,Rectangle desktop,Rectangle monitor,Rectangle foreground,int[] previous)
        {
            Rectangle requested=action=="fullscreen"?monitor:action=="window"?foreground:Rectangle.Empty;
            if(action=="repeat"&&previous!=null&&previous.Length==4&&previous[2]>0&&previous[3]>0)requested=new Rectangle(previous[0],previous[1],previous[2],previous[3]);
            if(requested.Width<1||requested.Height<1||requested.Width>desktop.Width||requested.Height>desktop.Height||requested.X<desktop.Left||requested.Y<desktop.Top||(long)requested.X+requested.Width>desktop.Right||(long)requested.Y+requested.Height>desktop.Bottom)return Rectangle.Empty;return requested;
        }
    }
    public sealed partial class Preferences
    {
        public int[] LastCapture;
        public bool IncludeCursor;
    }
    public sealed partial class CaptureOverlay
    {
        Bitmap pendingBoard;
        internal Bitmap TakeBoardImage(){var image=pendingBoard;pendingBoard=null;return image;}
        internal bool Completed{get;private set;}
        internal Rectangle ScreenSelection{get{var result=Rectangle.Round(canvas.Crop);result.Offset(desktop.Location);return result;}}
    }
}
