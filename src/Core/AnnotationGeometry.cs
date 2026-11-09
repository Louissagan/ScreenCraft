using System;
using System.Drawing;

// Image-coordinate geometry shared by canvas, documents and renderer.
namespace ScreenCraft
{
    public enum Tool { Arrow, Rect, Ellipse, Text, Counter, Highlight, Pixelate, Crop, Redact, Line, Spotlight, Cloud, Pen, Callout, Watermark }

    public partial class Shape
    {
        public Tool Kind;
        public Color Color;
        public int Weight=1, Number, MosaicBlock, CacheBlock;
        public float Width, FontPx, MeasuredPx;
        public PointF A, B, C;
        public bool Curved;
        public string Text, MeasuredText;
        public SizeF MeasuredSize;
        public Bitmap Cache;
        public RectangleF CacheBox;

        public static bool IsLinear(Tool tool) { return tool==Tool.Line || tool==Tool.Arrow; }
        public RectangleF Box
        {
            get
            {
                if(Kind==Tool.Pen && Points!=null && Points.Count!=0) return PointBounds(Points);
                float left=Math.Min(A.X,B.X),top=Math.Min(A.Y,B.Y),right=Math.Max(A.X,B.X),bottom=Math.Max(A.Y,B.Y);
                if(IsLinear(Kind) && Curved) {
                    IncludeExtremum(A.X,C.X,B.X,ref left,ref right);
                    IncludeExtremum(A.Y,C.Y,B.Y,ref top,ref bottom);
                }
                return RectangleF.FromLTRB(left,top,right,bottom);
            }
        }
        static void IncludeExtremum(float start,float control,float end,ref float low,ref float high)
        {
            double divisor=start-2.0*control+end;
            if(Math.Abs(divisor)<1e-12)return;
            double t=(start-control)/divisor;
            if(t<=0 || t>=1)return;
            float value=(float)((1-t)*(1-t)*start+2*t*(1-t)*control+t*t*end);
            low=Math.Min(low,value);high=Math.Max(high,value);
        }
        PointF At(double t)
        {
            double u=1-t;
            return Curved ? new PointF((float)(u*u*A.X+2*u*t*C.X+t*t*B.X),(float)(u*u*A.Y+2*u*t*C.Y+t*t*B.Y))
                : new PointF((float)(A.X+(B.X-A.X)*t),(float)(A.Y+(B.Y-A.Y)*t));
        }
        public PointF Mid { get { return At(.5); } }
        public void SetMid(PointF position,float snap)
        {
            var center=new PointF((A.X+B.X)*.5f,(A.Y+B.Y)*.5f);
            double dx=position.X-center.X,dy=position.Y-center.Y;
            Curved=dx*dx+dy*dy>=snap*snap;
            C=new PointF(position.X*2-center.X,position.Y*2-center.Y);
        }
        public PointF[] Curve(int segments)
        {
            if(segments<1 || segments>65536)throw new ArgumentOutOfRangeException("segments");
            var result=new PointF[segments+1];
            for(int i=0;i<=segments;i++)result[i]=At((double)i/segments);
            return result;
        }
        public Shape Clone()
        {
            var result=(Shape)MemberwiseClone();
            result.Cache=null;result.CacheBox=RectangleF.Empty;
            if(Points!=null)result.Points=new System.Collections.Generic.List<PointF>(Points);
            return result;
        }
        public void Offset(Shape source,float dx,float dy)
        {
            // Read all coordinates before writing, including when source == this.
            var a=source.A;var b=source.B;var c=source.C;var points=source.Points;
            A=new PointF(a.X+dx,a.Y+dy);B=new PointF(b.X+dx,b.Y+dy);C=new PointF(c.X+dx,c.Y+dy);
            if(points!=null){var moved=new System.Collections.Generic.List<PointF>(points.Count);foreach(var p in points)moved.Add(new PointF(p.X+dx,p.Y+dy));Points=moved;}
            DropCache();
        }
        public void DropCache() { var previous=Cache;Cache=null;CacheBox=RectangleF.Empty;if(previous!=null)previous.Dispose(); }
    }
}
