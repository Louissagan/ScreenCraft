using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    public static partial class CaptureKeyboard
    {
        public static Rectangle AdjustRange(Rectangle range,Keys keys,Rectangle bounds){Keys key=keys&Keys.KeyCode;int step=(keys&Keys.Shift)!=0?10:1;int dx=key==Keys.Left?-step:key==Keys.Right?step:0,dy=key==Keys.Up?-step:key==Keys.Down?step:0;int handle=(keys&Keys.Control)!=0?(dx!=0?3:5):(keys&Keys.Alt)!=0?(dx!=0?7:1):8;return CaptureOverlayGeometry.DragSelection(range,handle,new Point(dx,dy),bounds);}
    }
    public sealed partial class CaptureOverlay
    {
        Point guideAt;bool guideVisible;readonly bool guidesEnabled;
        internal void SetGuide(Point at,bool visible){visible=visible&&guidesEnabled;if(guideVisible==visible&&(!visible||at==guideAt))return;foreach(var r in GuideBands(guideAt,guideVisible))canvas.Invalidate(r);guideAt=at;guideVisible=visible;foreach(var r in GuideBands(guideAt,guideVisible))canvas.Invalidate(r);}
        Rectangle[] GuideBands(Point at,bool visible){return visible?new[]{new Rectangle(0,at.Y-2,Width,5),new Rectangle(at.X-2,0,5,Height)}:new Rectangle[0];}
    }
    public partial class RegionPicker
    {
        bool guideSeen;
        bool GuidesVisible{get{return guideSeen&&(mode==Mode.Image||mode==Mode.Text||mode==Mode.Scroll)&&!(adjusting&&barRect.Contains(cur))&&(ShotStack.Running==null||ShotStack.Running.ColorPreferences==null||ShotStack.Running.ColorPreferences.ShowCaptureGuides);}}
    }
}
