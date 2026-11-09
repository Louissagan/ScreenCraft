using System;
using System.Drawing;
using System.Drawing.Imaging;
namespace ScreenCraft
{
    public partial class Shape{public int RedactionMode;internal int CachedRedactionMode;public float WatermarkAngle=-25,WatermarkSpacing=100;}
    public static partial class Painter
    {
        internal static void DrawSoftRedaction(Graphics g,Shape s,Bitmap source,RectangleF captureArea)
        {
            var area=Rectangle.Intersect(Rectangle.Round(s.Box),Rectangle.Intersect(new Rectangle(Point.Empty,source.Size),Rectangle.Round(captureArea)));if(area.Width<1||area.Height<1)return;int block=s.MosaicBlock>0?s.MosaicBlock:16;
            if(s.Cache==null||s.CacheBox!=area||s.CacheBlock!=block||s.CachedRedactionMode!=s.RedactionMode){s.DropCache();if(s.RedactionMode==1)using(var cropped=source.Clone(area,PixelFormat.Format32bppArgb))s.Cache=FrozenGlass.Blur(cropped,false,Math.Max(3,block/3));else{s.Cache=new Bitmap(area.Width,area.Height);using(var paint=Graphics.FromImage(s.Cache)){paint.Clear(Color.White);paint.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;int step=Math.Max(6,block/2);Color[] colors={Color.FromArgb(0,105,239),Color.FromArgb(90,165,255),Color.FromArgb(255,150,40),Color.FromArgb(170,205,250)};for(int y=step/2;y<area.Height;y+=step)for(int x=step/2;x<area.Width;x+=step)using(var brush=new SolidBrush(colors[((x/step)*7+(y/step)*3)%colors.Length]))paint.FillEllipse(brush,x-step/6f,y-step/6f,step/3f,step/3f);}}s.CacheBox=area;s.CacheBlock=block;s.CachedRedactionMode=s.RedactionMode;}
            g.DrawImage(s.Cache,area);
        }
        internal static void DrawWatermark(Graphics g,Shape s)
        {
            if(string.IsNullOrEmpty(s.Text))return;var state=g.Save();try{g.SetClip(s.Box,System.Drawing.Drawing2D.CombineMode.Intersect);g.TranslateTransform(s.Box.Left+s.Box.Width/2,s.Box.Top+s.Box.Height/2);g.RotateTransform(s.WatermarkAngle);float span=(float)Math.Sqrt(s.Box.Width*s.Box.Width+s.Box.Height*s.Box.Height);float gap=Math.Max(s.WatermarkSpacing,span/45);var font=TextFont(s.FontPx);var textSize=g.MeasureString(s.Text,font);float stepX=Math.Max(40,textSize.Width+gap),stepY=Math.Max(30,textSize.Height+gap*.5f);using(var ink=new SolidBrush(Color.FromArgb(s.FillColor.A,s.Color)))for(float y=-span;y<=span;y+=stepY)for(float x=-span;x<=span;x+=stepX){var rectangle=new RectangleF(x,y,textSize.Width,textSize.Height);if(g.IsVisible(rectangle))g.DrawString(s.Text,font,ink,rectangle);}}finally{g.Restore(state);}
        }
    }
}
