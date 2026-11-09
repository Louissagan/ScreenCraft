using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ScreenCraft
{
    public static partial class Painter
    {
        [ThreadStatic] static Graphics measuring;
        [ThreadStatic] static Bitmap measuringImage;
        [ThreadStatic] static StringFormat format;
        [ThreadStatic] static Dictionary<int,Font> badgeFonts;
        static Graphics measure { get { if(measuring==null){measuringImage=new Bitmap(1,1);measuring=Graphics.FromImage(measuringImage);measuring.PageUnit=GraphicsUnit.Pixel;}return measuring; } }
        static StringFormat TextFmt { get { if(format==null){format=(StringFormat)StringFormat.GenericTypographic.Clone();format.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;}return format; } }
        static Font TextFont(float size) { return Fonts.Get("Microsoft YaHei UI",Math.Max(1,size)); }
        static Font CounterFont(float size)
        {
            int key=Math.Max(100,(int)Math.Round(size*100));
            if(badgeFonts==null)badgeFonts=new Dictionary<int,Font>();
            Font font;if(badgeFonts.TryGetValue(key,out font))return font;
            if(badgeFonts.Count>=64){foreach(var item in badgeFonts.Values)item.Dispose();badgeFonts.Clear();}
            font=new Font("Segoe UI",key/100f,FontStyle.Bold,GraphicsUnit.Pixel);badgeFonts.Add(key,font);return font;
        }
        public static Color Contrast(Color color) { return color.R*.2126+color.G*.7152+color.B*.0722>=160?Color.Black:Color.White; }
        public static RectangleF TextBounds(Shape shape)
        {
            if(shape.TextWrap)return WrappedTextBounds(shape);
            if(shape.MeasuredText!=(shape.Text??"") || shape.MeasuredPx!=shape.FontPx){shape.MeasuredSize=measure.MeasureString(shape.Text??"",TextFont(shape.FontPx),PointF.Empty,TextFmt);shape.MeasuredText=shape.Text??"";shape.MeasuredPx=shape.FontPx;}
            return new RectangleF(shape.A,new SizeF(shape.MeasuredSize.Width+16,Math.Max(shape.NoteHeight,shape.MeasuredSize.Height+16)));
        }
        public static float CounterRadius(Shape shape) { return MarkerDiameter(shape)*.5f; }
        static RectangleF CircleBounds(PointF center,float diameter) { return new RectangleF(center.X-diameter*.5f,center.Y-diameter*.5f,diameter,diameter); }
        public static RectangleF Bounds(Shape shape)
        {
            RectangleF box;
            switch(shape.Kind){
                case Tool.Text:return TextBounds(shape);
                case Tool.Callout:box=CircleBounds(shape.A,MarkerDiameter(shape));break;
                case Tool.Counter:box=CircleBounds(shape.A,MarkerDiameter(shape));if(shape.LeaderCounter)box=RectangleF.Union(box,CounterBadge(shape));break;
                default:box=shape.Box;float padding=Shape.IsLinear(shape.Kind)?Math.Max(1,shape.Width*3):shape.Kind==Tool.Cloud?CloudAmplitude(shape)+shape.Width:shape.Width*.5f;box.Inflate(padding,padding);break;
            }
            return HasNote(shape.Kind)&&!string.IsNullOrEmpty(shape.Text)?RectangleF.Union(box,NoteBounds(shape)):box;
        }
        public static PointF Similar(PointF point,PointF oldStart,PointF oldEnd,PointF newStart,PointF newEnd)
        {
            double x=oldEnd.X-oldStart.X,y=oldEnd.Y-oldStart.Y,den=x*x+y*y;
            if(den<1e-12)return new PointF(point.X+newStart.X-oldStart.X,point.Y+newStart.Y-oldStart.Y);
            double px=point.X-oldStart.X,py=point.Y-oldStart.Y;
            double along=(px*x+py*y)/den,across=(py*x-px*y)/den;
            double nx=newEnd.X-newStart.X,ny=newEnd.Y-newStart.Y;
            return new PointF((float)(newStart.X+along*nx-across*ny),(float)(newStart.Y+along*ny+across*nx));
        }
        public static GraphicsPath ArrowPath(PointF start,PointF end,float width) { return ArrowPath(new Shape{A=start,B=end,Width=width}); }
        public static GraphicsPath ArrowPath(Shape shape)
        {
            var path=new GraphicsPath();float width=Math.Max(.25f,shape.Width);
            var points=shape.Curve(shape.Curved?64:1);double length=0;var distances=new double[points.Length];
            for(int i=1;i<points.Length;i++){double dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;length+=Math.Sqrt(dx*dx+dy*dy);distances[i]=length;}
            if(length<.01){path.AddEllipse(shape.A.X-width/2,shape.A.Y-width/2,width,width);return path;}
            double head=Math.Min(length*.45,Math.Max(8,width*4)),neck=length-head;
            var left=new List<PointF>();var right=new List<PointF>();PointF basePoint=shape.A,tangent=new PointF(1,0);
            for(int i=1;i<points.Length;i++){
                double segment=distances[i]-distances[i-1];if(segment<1e-8)continue;
                float ux=(float)((points[i].X-points[i-1].X)/segment),uy=(float)((points[i].Y-points[i-1].Y)/segment);
                if(left.Count==0){left.Add(new PointF(points[0].X-uy*width*.2f,points[0].Y+ux*width*.2f));right.Add(new PointF(points[0].X+uy*width*.2f,points[0].Y-ux*width*.2f));}
                double d=Math.Min(neck,distances[i]),t=(d-distances[i-1])/segment;
                basePoint=new PointF((float)(points[i-1].X+t*(points[i].X-points[i-1].X)),(float)(points[i-1].Y+t*(points[i].Y-points[i-1].Y)));tangent=new PointF(ux,uy);
                float half=width*(float)(.2+.4*d/Math.Max(1,neck));left.Add(new PointF(basePoint.X-uy*half,basePoint.Y+ux*half));right.Add(new PointF(basePoint.X+uy*half,basePoint.Y-ux*half));
                if(distances[i]>=neck)break;
            }
            float wing=(float)Math.Min(head*.5,width*2.5);left.Add(new PointF(basePoint.X-tangent.Y*wing,basePoint.Y+tangent.X*wing));left.Add(shape.B);left.Add(new PointF(basePoint.X+tangent.Y*wing,basePoint.Y-tangent.X*wing));right.Reverse();left.AddRange(right);path.AddPolygon(left.ToArray());return path;
        }
        public static void Draw(Graphics graphics,Shape shape,Bitmap source) { Draw(graphics,shape,source,new RectangleF(PointF.Empty,source.Size)); }
        static void Draw(Graphics graphics,Shape shape,Bitmap source,RectangleF area)
        {
            var state=graphics.Save();try{
                graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                if(DrawExtended(graphics,shape,source,area))return;
                switch(shape.Kind){
                    case Tool.Arrow:using(var path=ArrowPath(shape))using(var brush=new SolidBrush(shape.Color)){
                        if(shape.ArrowWhiteOutline)using(var border=new Pen(Color.White,Math.Max(2,shape.Width*.6f))){border.LineJoin=LineJoin.Round;graphics.DrawPath(border,path);}graphics.FillPath(brush,path);
                    }break;
                    case Tool.Highlight:using(var brush=new SolidBrush(Color.FromArgb(95,shape.Color)))graphics.FillRectangle(brush,shape.Box);break;
                    case Tool.Redact:graphics.SmoothingMode=SmoothingMode.None;using(var brush=new SolidBrush(Color.FromArgb(22,22,26)))graphics.FillRectangle(brush,Rectangle.Round(shape.Box));break;
                    case Tool.Pixelate:
                        var region=Rectangle.Intersect(Rectangle.Round(shape.Box),Rectangle.Intersect(Rectangle.Round(area),new Rectangle(Point.Empty,source.Size)));
                        if(region.Width<1||region.Height<1)break;
                        int block=shape.MosaicBlock>0?shape.MosaicBlock:Math.Max(12,Math.Min(source.Width,source.Height)/45);
                        if(shape.Cache==null||shape.CacheBox!=region||shape.CacheBlock!=block||shape.CachedRedactionMode!=0){shape.DropCache();shape.Cache=Pixelate(source,region,block);shape.CacheBox=region;shape.CacheBlock=block;shape.CachedRedactionMode=0;}
                        graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(shape.Cache,region,0,0,region.Width,region.Height,GraphicsUnit.Pixel);break;
                }
            }finally{graphics.Restore(state);}
        }
        public static void DrawMarks(Graphics graphics,IList<Shape> shapes,Shape extra,Shape skip,Bitmap source,RectangleF area)
        {
            DrawSpotlights(graphics,shapes,extra,area);
            for(int index=0;index<=shapes.Count;index++){
                Shape shape=index==shapes.Count?extra:shapes[index];if(shape==null||shape==skip)continue;
                Draw(graphics,shape,source,area);
                if(shape.Kind==Tool.Pixelate){var state=graphics.Save();try{graphics.SetClip(shape.Box,CombineMode.Intersect);DrawSpotlights(graphics,shapes,extra,area);}finally{graphics.Restore(state);}}
            }
        }
        public static void DrawSpotlights(Graphics graphics,IList<Shape> shapes,Shape extra,RectangleF area)
        {
            var holes=new List<Shape>();float opacity=0;
            foreach(var shape in shapes)if(shape!=null&&shape.Kind==Tool.Spotlight)holes.Add(shape);
            if(extra!=null&&extra.Kind==Tool.Spotlight)holes.Add(extra);if(holes.Count==0)return;
            var visible=Rectangle.Intersect(Rectangle.Ceiling(area),Rectangle.Ceiling(graphics.VisibleClipBounds));if(visible.Width<1||visible.Height<1)return;
            foreach(var shape in holes)opacity=Math.Max(opacity,shape.EffectOpacity>0?shape.EffectOpacity:150f/255*100);
            using(var mask=new Bitmap(visible.Width,visible.Height,PixelFormat.Format32bppArgb)){
                int alpha=(int)Math.Round(Math.Min(100,opacity)*2.55);var pixels=new int[checked(visible.Width*visible.Height)];for(int i=0;i<pixels.Length;i++)pixels[i]=unchecked(alpha<<24);
                // Analytic pixel coverage of the union of rounded apertures. Taking
                // minimum coverage avoids the dark seam of overlapping cutout paths.
                foreach(var shape in holes){var box=shape.Box;if(box.Width<1||box.Height<1)continue;float radius=Math.Min(6+shape.Width*2,Math.Min(box.Width,box.Height)/2),cx=box.Left+box.Width/2,cy=box.Top+box.Height/2;
                    var affected=Rectangle.Intersect(visible,Rectangle.Ceiling(RectangleF.Inflate(box,1,1)));
                    for(int y=affected.Top;y<affected.Bottom;y++)for(int x=affected.Left;x<affected.Right;x++){
                        double qx=Math.Abs(x+.5-cx)-(box.Width/2-radius),qy=Math.Abs(y+.5-cy)-(box.Height/2-radius),ex=Math.Max(0,qx),ey=Math.Max(0,qy);
                        double distance=Math.Sqrt(ex*ex+ey*ey)+Math.Min(Math.Max(qx,qy),0)-radius;
                        int coverage=(int)Math.Round(alpha*Math.Max(0,Math.Min(1,distance+.5)));int index=(y-visible.Top)*visible.Width+x-visible.Left;
                        if(coverage<(int)((uint)pixels[index]>>24))pixels[index]=unchecked(coverage<<24);
                    }
                }
                var data=mask.LockBits(new Rectangle(Point.Empty,mask.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<mask.Height;y++)Marshal.Copy(pixels,y*mask.Width,IntPtr.Add(data.Scan0,y*data.Stride),mask.Width);}finally{mask.UnlockBits(data);}
                var state=graphics.Save();try{graphics.InterpolationMode=InterpolationMode.HighQualityBilinear;graphics.DrawImage(mask,visible,0,0,visible.Width,visible.Height,GraphicsUnit.Pixel);}finally{graphics.Restore(state);}
            }
        }
        public static Bitmap Pixelate(Bitmap source,Rectangle region) { return Pixelate(source,region,16); }
        public static Bitmap Pixelate(Bitmap source,Rectangle region,int blockSize)
        {
            region=Rectangle.Intersect(region,new Rectangle(Point.Empty,source.Size));if(region.Width<1||region.Height<1)return null;
            int block=Math.Max(2,Math.Min(128,blockSize));var output=new Bitmap(region.Width,region.Height,PixelFormat.Format32bppArgb);
            try{using(var cropped=source.Clone(region,PixelFormat.Format32bppArgb)){
                var rect=new Rectangle(Point.Empty,output.Size);var data=cropped.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                int[] pixels=new int[checked(output.Width*output.Height)];try{for(int y=0;y<output.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*output.Width,output.Width);}finally{cropped.UnlockBits(data);}
                for(int y0=0;y0<output.Height;y0+=block)for(int x0=0;x0<output.Width;x0+=block){int x1=Math.Min(output.Width,x0+block),y1=Math.Min(output.Height,y0+block);long red=0,green=0,blue=0,alpha=0;int count=(x1-x0)*(y1-y0);
                    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){uint pixel=unchecked((uint)pixels[y*output.Width+x]);alpha+=pixel>>24;red+=(pixel>>16)&255;green+=(pixel>>8)&255;blue+=pixel&255;}
                    int average=unchecked((int)((alpha/count)<<24 | (red/count)<<16 | (green/count)<<8 | blue/count));
                    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)pixels[y*output.Width+x]=average;
                }
                var target=output.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<output.Height;y++)Marshal.Copy(pixels,y*output.Width,IntPtr.Add(target.Scan0,y*target.Stride),output.Width);}finally{output.UnlockBits(target);}
            }return output;}catch{output.Dispose();throw;}
        }
    }
}
