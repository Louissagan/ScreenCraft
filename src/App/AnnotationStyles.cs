// ScreenCraft extensions to the MIT Stackshot Shape/Painter model.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace Stackshot
{
    public partial class Shape
    {
        public string Id = Guid.NewGuid().ToString("N");
        public float CornerRadius;
        public bool FillEnabled;
        public Color FillColor = Color.FromArgb(60, 255, 210, 0);
        public DashStyle Dash = DashStyle.Solid;
        public bool SimpleArrow;
        public List<PointF> Points;
        public float NoteWidth = 220;
        public float NoteHeight,MarkerSize,EffectOpacity;
        public PointF NoteOffset = new PointF(42, -18);
        public bool HasNoteTextColor;public Color NoteTextColor;public float NoteFontPx;

        internal static RectangleF PointBounds(IList<PointF> points)
        {
            float x0=points[0].X, y0=points[0].Y, x1=x0, y1=y0;
            foreach (var p in points) { x0=Math.Min(x0,p.X); y0=Math.Min(y0,p.Y); x1=Math.Max(x1,p.X); y1=Math.Max(y1,p.Y); }
            return RectangleF.FromLTRB(x0,y0,x1,y1);
        }
    }

    public static partial class Painter
    {
        internal static float CloudAmplitude(Shape s) { return Math.Min(9, Math.Max(2, Math.Min(s.Box.Width,s.Box.Height)/8)); }

        public static GraphicsPath CloudPath(RectangleF r)
        {
            var path=new GraphicsPath();
            float amp=Math.Min(9,Math.Max(2,Math.Min(r.Width,r.Height)/8));
            PointF[] corners={new PointF(r.Left,r.Top),new PointF(r.Right,r.Top),new PointF(r.Right,r.Bottom),new PointF(r.Left,r.Bottom),new PointF(r.Left,r.Top)};
            for(int edge=0;edge<4;edge++)
            {
                var a=corners[edge]; var b=corners[edge+1]; float dx=b.X-a.X,dy=b.Y-a.Y;
                float length=(float)Math.Sqrt(dx*dx+dy*dy);
                int count=Math.Max(1,Math.Min(512,(int)Math.Ceiling(length/18)));
                float nx=length>0?dy/length:0, ny=length>0?-dx/length:0;
                for(int i=0;i<count;i++)
                {
                    float t0=i/(float)count,t1=(i+1)/(float)count;
                    var p0=new PointF(a.X+dx*t0,a.Y+dy*t0); var p1=new PointF(a.X+dx*t1,a.Y+dy*t1);
                    path.AddBezier(p0,new PointF(p0.X+(p1.X-p0.X)/3+nx*amp,p0.Y+(p1.Y-p0.Y)/3+ny*amp),
                        new PointF(p0.X+2*(p1.X-p0.X)/3+nx*amp,p0.Y+2*(p1.Y-p0.Y)/3+ny*amp),p1);
                }
            }
            path.CloseFigure(); return path;
        }

        public static RectangleF NoteBounds(Shape s)
        {
            float font=NoteFont(s), width=WrappedWidth(s.Text,NoteFont(s),s.NoteWidth,s.NoteAutoWidth,8);
            var text=s.Text??"";
            SizeF size=measure.MeasureString(text,TextFont(font),new SizeF(width-16,10000));
            float x=s.Kind==Tool.Cloud?s.Box.Right:s.Kind==Tool.Counter&&s.LeaderCounter?s.B.X:s.A.X;
            float y=s.Kind==Tool.Cloud?s.Box.Top:s.Kind==Tool.Counter&&s.LeaderCounter?s.B.Y:s.A.Y;
            return new RectangleF(x+s.NoteOffset.X,y+s.NoteOffset.Y,width,Math.Max(s.NoteHeight,Math.Max(font+16,size.Height+16)));
        }

        static Pen StyledPen(Shape s)
        {
            var pen=new Pen(s.Color,Math.Max(0.25f,s.Width));
            pen.DashStyle=s.Dash; pen.LineJoin=LineJoin.Round;
            pen.StartCap=pen.EndCap=LineCap.Round;
            return pen;
        }

        static bool DrawExtended(Graphics g,Shape s,Bitmap source,RectangleF area)
        {
            if(s.Kind==Tool.Pixelate&&s.RedactionMode>0){DrawSoftRedaction(g,s,source,area);return true;}
            if(s.Kind==Tool.Watermark){DrawWatermark(g,s);return true;}
            if(s.Kind!=Tool.Rect && s.Kind!=Tool.Ellipse && s.Kind!=Tool.Cloud && s.Kind!=Tool.Pen &&
               s.Kind!=Tool.Callout && s.Kind!=Tool.Text && s.Kind!=Tool.Counter && s.Kind!=Tool.Line && !(s.Kind==Tool.Arrow && s.SimpleArrow)) return false;
            var state=g.Save();
            try
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                if(s.Kind==Tool.Rect || s.Kind==Tool.Ellipse || s.Kind==Tool.Cloud)
                {
                    using(var path=s.Kind==Tool.Cloud?CloudPath(s.Box):s.Kind==Tool.Rect?Theme.Round(s.Box,s.CornerRadius):new GraphicsPath())
                    using(var pen=StyledPen(s))
                    {
                        if(s.Kind==Tool.Ellipse) path.AddEllipse(s.Box);
                        if(s.FillEnabled) using(var fill=new SolidBrush(s.FillColor))g.FillPath(fill,path);
                        g.DrawPath(pen,path);
                    }
                }
                else if(s.Kind==Tool.Pen || s.Kind==Tool.Line || s.Kind==Tool.Arrow)
                {
                    PointF[] points=s.Kind==Tool.Pen?(s.Points??new List<PointF>()).ToArray():s.Curve(s.Curved?48:1);
                    using(var pen=StyledPen(s))
                    {
                        if(s.Kind==Tool.Pen)DrawFreehand(g,s,points,pen);
                        else if(s.Kind==Tool.Arrow) using(var cap=new AdjustableArrowCap(3,4,true)){pen.CustomEndCap=cap;if(points.Length>1)g.DrawLines(pen,points);}
                        else if(points.Length>1)g.DrawLines(pen,points);
                        else if(points.Length==1)using(var b=new SolidBrush(s.Color))g.FillEllipse(b,points[0].X-s.Width/2,points[0].Y-s.Width/2,s.Width,s.Width);
                    }
                }
                else if(s.Kind==Tool.Text)
                {
                    var box=TextBounds(s); float pad=8;
                    using(var path=Theme.Round(box,6))using(var fill=new SolidBrush(LabelColor(s)))g.FillPath(fill,path);
                    g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                    using(var ink=new SolidBrush(Color.White)){if(s.TextWrap)g.DrawString(s.Text??"",TextFont(s.FontPx),ink,RectangleF.Inflate(box,-pad,-pad));else g.DrawString(s.Text??"",TextFont(s.FontPx),ink,s.A.X+pad,s.A.Y+pad,TextFmt);}
                }
                else if(s.Kind==Tool.Callout){if(!string.IsNullOrEmpty(s.Text))DrawPointLeader(g,s,NoteBounds(s));else using(var ink=new SolidBrush(s.Color)){float d=MarkerDiameter(s);g.FillEllipse(ink,s.A.X-d/2,s.A.Y-d/2,d,d);}}
                else if(s.Kind==Tool.Counter&&s.LeaderCounter){DrawCounterLeader(g,s);}
                else if(s.Kind==Tool.Counter)
                {
                    float radius=CounterRadius(s);
                    var circle=new RectangleF(s.A.X-radius,s.A.Y-radius,2*radius,2*radius);
                    using(var fill=new SolidBrush(s.Color))g.FillEllipse(fill,circle);
                    string digits=s.Number.ToString();float letterSize=radius*1.05f;var measured=g.MeasureString(digits,CounterFont(letterSize));if(measured.Width>circle.Width*.8f)letterSize*=circle.Width*.8f/measured.Width;
                    using(var ink=new SolidBrush(Color.White))
                    using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                        g.DrawString(digits,CounterFont(letterSize),ink,circle,format);
                }
                if(Painter.HasNote(s.Kind) && !string.IsNullOrEmpty(s.Text)) DrawNote(g,s);
                return true;
            }
            finally {g.Restore(state);}
        }

        static void DrawNote(Graphics g,Shape s)
        {
            var box=NoteBounds(s); var origin=s.Kind==Tool.Cloud?new PointF(s.Box.Right,s.Box.Top+s.Box.Height/2):s.Kind==Tool.Counter&&s.LeaderCounter?s.B:s.A;
            var target=new PointF(Math.Max(box.Left,Math.Min(box.Right,origin.X)),Math.Max(box.Top,Math.Min(box.Bottom,origin.Y)));
            if(s.Kind!=Tool.Cloud){float dx=target.X-origin.X,dy=target.Y-origin.Y,d=(float)Math.Sqrt(dx*dx+dy*dy);if(d>0){float r=Math.Min(MarkerDiameter(s)/2,d);origin=new PointF(origin.X+dx*r/d,origin.Y+dy*r/d);}}
            using(var pen=new Pen(s.Color,LeaderWidth(s)))g.DrawLine(pen,origin,target);
            using(var path=Theme.Round(box,6))using(var fill=new SolidBrush(Color.FromArgb(255,s.Color)))g.FillPath(fill,path);
            var textRect=RectangleF.Inflate(box,-8,-8);
            g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            using(var ink=new SolidBrush(NoteInk(s)))g.DrawString(s.Text,TextFont(NoteFont(s)),ink,textRect);
        }
        public static float NoteFont(Shape s){return Math.Max(8,s.NoteFontPx>0?s.NoteFontPx:s.FontPx);}
        public static Color NoteInk(Shape s){return Color.White;}
        public static Color LabelColor(Shape s){return s.Kind==Tool.Text&&s.FillEnabled?Color.FromArgb(255,s.FillColor):Color.FromArgb(255,s.Color);}
        public static float MarkerDiameter(Shape s){return s.MarkerSize>0?s.MarkerSize:s.Kind==Tool.Counter?s.FontPx*1.6f:12;}
        public static float LeaderWidth(Shape s){return s.Kind==Tool.Cloud?Math.Max(1,s.Width/2):Math.Max(1,MarkerDiameter(s)/12);}
    }
}
