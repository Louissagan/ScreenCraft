using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    public partial class PinWindow
    {
        bool resizing;Point resizeAt;float resizeZoom;Size resizeBody;
        float beforeActual=1;
        internal Size DisplayedImageSize{get{return body;}}
        internal Rectangle ResizeGrip{get{return new Rectangle(Pad+Math.Max(0,body.Width-22),Pad+Math.Max(0,body.Height-22),Math.Min(22,body.Width),Math.Min(22,body.Height));}}
        internal void BeginProportionalResize(Point point){resizeAt=point;resizeZoom=zoom;resizeBody=body;resizing=true;}
        internal void UpdateProportionalResize(Point point){if(!resizing)return;float factor=PinGeometry.ResizeFactor(resizeBody,new Point(point.X-resizeAt.X,point.Y-resizeAt.Y));SetZoom(resizeZoom*factor,Point.Empty);}
        internal void EndProportionalResize(){resizing=false;}
        internal void CancelProportionalResize(){if(!resizing)return;resizing=false;SetZoom(resizeZoom,Point.Empty);}
        internal void ApplyPinWheel(int delta,Point anchor,Keys modifiers){if(delta==0)return;delta=pinPreferences.PinReverseWheel?-delta:delta;if(((modifiers&Keys.Control)!=0)^pinPreferences.PinWheelOpacity)SetOpacityPercent(PinGeometry.StepOpacity(State.OpacityPercent,delta));else SetZoom(zoom*(float)Math.Pow(1.1,delta/120.0),anchor);}
        internal bool ApplyPinKey(Keys keys)
        {
            Keys arrow=keys&Keys.KeyCode;if((keys&Keys.Modifiers)==Keys.None||(keys&Keys.Modifiers)==Keys.Shift){int step=(keys&Keys.Shift)!=0?10:1;if(arrow==Keys.Left||arrow==Keys.Right||arrow==Keys.Up||arrow==Keys.Down){Location=new Point(Left+(arrow==Keys.Left?-step:arrow==Keys.Right?step:0),Top+(arrow==Keys.Up?-step:arrow==Keys.Down?step:0));return true;}}
            if(keys==(Keys.Control|Keys.R)){ChangeView(1,false,false);return true;}if(keys==(Keys.Control|Keys.H)){ChangeView(0,true,false);return true;}if(keys==(Keys.Control|Keys.J)){ChangeView(0,false,true);return true;}
            var center=new Point(body.Width/2,body.Height/2);
            if(keys==Keys.Escape){if(resizing){CancelProportionalResize();Capture=false;}else Close();return true;}
            if(keys==(Keys.Control|Keys.C)){Copy();return true;}
            if(keys==Keys.S){ToggleThumbnail();return true;}
            if(keys==Keys.Z){if(Math.Abs(zoom-1)<.001f)SetZoom(beforeActual,center);else{beforeActual=zoom;SetZoom(1,center);}return true;}
            if(keys==(Keys.Control|Keys.D0)||keys==(Keys.Control|Keys.NumPad0)){SetZoom(1,center);return true;}
            if(keys==(Keys.Control|Keys.Oemplus)||keys==(Keys.Control|Keys.Shift|Keys.Oemplus)||keys==(Keys.Control|Keys.Add)){SetZoom(zoom*1.25f,center);return true;}
            if(keys==(Keys.Control|Keys.OemMinus)||keys==(Keys.Control|Keys.Subtract)){SetZoom(zoom/1.25f,center);return true;}
            return false;
        }
    }
}
