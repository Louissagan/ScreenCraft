using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ScreenCraft
{
    public partial class Shape { public float Rotation; }

    internal static class ArrangeKeys
    {
        internal static bool Read(Keys keys,out int direction,out bool edge)
        {
            direction=0;edge=(keys&Keys.Shift)!=0;
            if((keys&Keys.Modifiers)!=(edge?Keys.Control|Keys.Shift:Keys.Control))return false;
            if((keys&Keys.KeyCode)==Keys.OemCloseBrackets)direction=1;
            if((keys&Keys.KeyCode)==Keys.OemOpenBrackets)direction=-1;
            return direction!=0;
        }
    }

    public static partial class Painter
    {
        public static float NormalizeRotation(float degrees)
        {
            if(float.IsNaN(degrees)||float.IsInfinity(degrees))throw new ArgumentOutOfRangeException("degrees");
            degrees%=360;return degrees<0?degrees+360:degrees;
        }
        internal static PointF Center(RectangleF bounds){return new PointF(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2);}
        public static PointF RotationCenter(Shape shape){return Center(LocalBounds(shape));}
        public static PointF RotatePoint(PointF point,PointF center,float degrees)
        {
            if(degrees==0)return point;double radians=degrees*Math.PI/180,c=Math.Cos(radians),s=Math.Sin(radians),x=point.X-center.X,y=point.Y-center.Y;
            return new PointF(center.X+(float)(x*c-y*s),center.Y+(float)(x*s+y*c));
        }
        public static PointF WorldPoint(Shape shape,PointF point){return shape.Rotation==0?point:RotatePoint(point,RotationCenter(shape),shape.Rotation);}
        public static PointF LocalPoint(Shape shape,PointF point){return shape.Rotation==0?point:RotatePoint(point,RotationCenter(shape),-shape.Rotation);}
        internal static PointF[] WorldCorners(Shape shape,RectangleF bounds){var center=RotationCenter(shape);return Canvas.BoxHandles(bounds).Take(4).Select(p=>RotatePoint(p,center,shape.Rotation)).ToArray();}
        public static RectangleF Bounds(Shape shape)
        {
            var local=LocalBounds(shape);return shape.Rotation==0?local:Shape.PointBounds(WorldCorners(shape,local));
        }
        internal static RectangleF EffectBounds(Shape shape){return shape.Rotation==0?shape.Box:Shape.PointBounds(WorldCorners(shape,shape.Box));}
        internal static GraphicsPath EffectPath(Shape shape){var path=new GraphicsPath();path.AddPolygon(WorldCorners(shape,shape.Box));return path;}
        internal static void ApplyRotation(Graphics graphics,Shape shape)
        {
            if(shape.Rotation==0)return;var center=RotationCenter(shape);graphics.TranslateTransform(center.X,center.Y);graphics.RotateTransform(shape.Rotation);graphics.TranslateTransform(-center.X,-center.Y);
        }
    }

    public partial class Canvas
    {
        PointF rotationPivot;
        float rotationStart;
        static float PointerAngle(PointF point,PointF center){return (float)(Math.Atan2(point.Y-center.Y,point.X-center.X)*180/Math.PI);}
        public bool ApplyArrangeKey(Keys keys)
        {
            int direction;bool edge;if(Typing||!ArrangeKeys.Read(keys,out direction,out edge))return false;
            if(!Busy)ReorderSelected(direction,edge);return true;
        }
        public void RotateSelected(float degrees)
        {
            if(!HasSelection||Busy||Typing)return;degrees=Painter.NormalizeRotation(degrees);if(Math.Abs(degrees)<.001)return;
            Commit();var pivot=Painter.Center(GroupBounds());foreach(var shape in SelectedShapes)RotateShape(shape,shape.Clone(),pivot,degrees);NotifyEdit();
        }
        public void SetSelectedRotation(float degrees)
        {
            degrees=Painter.NormalizeRotation(degrees);if(!HasSelection||Busy||Typing)return;
            if(SelectionCount>1){RotateSelected(degrees-selected.Rotation);return;}
            if(Math.Abs(selected.Rotation-degrees)<.001)return;EditSelected(s=>s.Rotation=degrees);
        }
        static void RotateShape(Shape target,Shape source,PointF pivot,float degrees)
        {
            var before=Painter.RotationCenter(source);var after=Painter.RotatePoint(before,pivot,degrees);
            target.Offset(source,after.X-before.X,after.Y-before.Y);target.Rotation=Painter.NormalizeRotation(source.Rotation+degrees);
        }
        void RotateGesture(PointF pointer,bool snap)
        {
            float degrees=PointerAngle(pointer,rotationPivot)-rotationStart;
            if(snap){float initial=SelectionCount==1?orig.Rotation:0;degrees=(float)Math.Round((initial+degrees)/15)*15-initial;}
            foreach(var pair in gestureShapes)RotateShape(pair.Key,pair.Value,rotationPivot,degrees);
        }
        // Keep the old coordinate frame while the local bounding box changes.
        static void PreserveRotationFrame(Shape target,Shape source)
        {
            if(source.Rotation==0)return;var before=Painter.RotationCenter(source);var after=Painter.RotationCenter(target);
            var mapped=Painter.RotatePoint(after,before,source.Rotation);target.Offset(target,mapped.X-after.X,mapped.Y-after.Y);
        }
        PointF[] AdornmentCorners(Shape shape)
        {
            var bounds=HandleBounds(shape);return SelectionCount>1?BoxHandles(bounds).Take(4).ToArray():Painter.WorldCorners(shape,bounds);
        }
        Point RotationStem(Shape shape)
        {
            var corners=AdornmentCorners(shape);return ToScreen(new PointF((corners[0].X+corners[1].X)/2,(corners[0].Y+corners[1].Y)/2));
        }
        internal Point SelectionRotationHandle()
        {
            Fit();if(selected==null)return Point.Empty;var stem=RotationStem(selected);float degrees=SelectionCount>1?0:selected.Rotation;
            double r=degrees*Math.PI/180;return new Point(stem.X+(int)Math.Round(Math.Sin(r)*Pu(27)),stem.Y-(int)Math.Round(Math.Cos(r)*Pu(27)));
        }
        internal bool SelectionControlAt(Point point){return HasSelection&&!Typing&&(RotationAt(point)||HandleAt(point)>=0);}
        bool RotationAt(Point point){if(selected==null||Typing)return false;var at=SelectionRotationHandle();return Math.Abs(at.X-point.X)<=Pu(9)&&Math.Abs(at.Y-point.Y)<=Pu(9);}
        void PaintRotationControl(Graphics graphics,Shape shape)
        {
            var at=SelectionRotationHandle();using(var pen=new Pen(CaptureLook.Blue,1))graphics.DrawLine(pen,RotationStem(shape),at);
            var state=graphics.Save();try{graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(System.Drawing.Color.White))graphics.FillEllipse(fill,at.X-Pu(6),at.Y-Pu(6),Pu(12),Pu(12));using(var pen=new Pen(CaptureLook.Blue,1.5f))graphics.DrawArc(pen,at.X-Pu(6),at.Y-Pu(6),Pu(12),Pu(12),0,360);}finally{graphics.Restore(state);}
        }
        public void SetClosedPen(bool value)
        {
            if(selected==null)ChangeDefaultStyle(d=>{d.ClosedPen=value;if(value)d.FreehandArrow=false;});
            else EditSelected(s=>{if(s.Kind==Tool.Pen){s.ClosedPen=value;if(value)s.FreehandArrow=false;}});
        }
    }
}
