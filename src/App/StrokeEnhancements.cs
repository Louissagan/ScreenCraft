using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    public partial class Shape{public bool SmoothPen,FreehandArrow,ArrowWhiteOutline,ClosedPen;}
    public static partial class Painter
    {
        internal static PointF[] SmoothPoints(PointF[] points)
        {
            if(points.Length<3)return points;var filtered=new List<PointF>{points[0]};for(int i=1;i<points.Length-1;i++){var before=points[i-1];var at=points[i];var after=points[i+1];filtered.Add(new PointF((before.X+2*at.X+after.X)/4,(before.Y+2*at.Y+after.Y)/4));}filtered.Add(points[points.Length-1]);return filtered.ToArray();
        }
        internal static GraphicsPath FreehandPath(Shape s)
        {
            var raw=s.Points==null?new PointF[0]:s.Points.ToArray();var points=s.SmoothPen&&!s.Polygon?SmoothPoints(raw):raw;var path=new GraphicsPath();
            if(s.ClosedPen&&points.Length>2){if(s.SmoothPen&&!s.Polygon)path.AddClosedCurve(points,.45f);else path.AddPolygon(points);}else if(points.Length>2&&s.SmoothPen&&!s.Polygon)path.AddCurve(points,.45f);else if(points.Length>1)path.AddLines(points);return path;
        }
        internal static void DrawFreehand(Graphics g,Shape s,PointF[] raw,Pen pen)
        {
            var points=s.SmoothPen&&!s.Polygon?SmoothPoints(raw):raw;using(var path=FreehandPath(s)){if(s.ClosedPen&&s.FillEnabled&&points.Length>2)using(var fill=new SolidBrush(Color.FromArgb(s.FillColor.A,s.Color)))g.FillPath(fill,path);if(path.PointCount>0)g.DrawPath(pen,path);}if(points.Length==1)using(var ink=new SolidBrush(s.Color))g.FillEllipse(ink,points[0].X-s.Width/2,points[0].Y-s.Width/2,s.Width,s.Width);
            if(s.FreehandArrow&&!s.ClosedPen&&points.Length>1){var end=points[points.Length-1];int index=points.Length-2;while(index>0&&Distance(points[index],end)<Math.Max(8,s.Width*3))index--;var at=points[index];float dx=end.X-at.X,dy=end.Y-at.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);if(length>.1){float size=Math.Max(10,s.Width*3.2f),ux=dx/length,uy=dy/length;g.DrawLines(pen,new[]{new PointF(end.X-ux*size-uy*size*.55f,end.Y-uy*size+ux*size*.55f),end,new PointF(end.X-ux*size+uy*size*.55f,end.Y-uy*size-ux*size*.55f)});}}
        }
        static float Distance(PointF a,PointF b){float dx=a.X-b.X,dy=a.Y-b.Y;return (float)Math.Sqrt(dx*dx+dy*dy);}
    }
}
