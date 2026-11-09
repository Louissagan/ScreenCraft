using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    // One artwork for the executable resource, tray and every application window.
    public static class BrandArtwork
    {
        public static readonly Color Blue=Color.FromArgb(0,105,239);
        public static Bitmap Render(int size)
        {
            var image=new Bitmap(size,size,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image)){
                g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/32f,size/32f);
                using(var path=new GraphicsPath()){path.AddArc(1,1,16,16,180,90);path.AddArc(15,1,16,16,270,90);path.AddArc(15,15,16,16,0,90);path.AddArc(1,15,16,16,90,90);path.CloseFigure();using(var b=new SolidBrush(Blue))g.FillPath(b,path);}
                using(var pen=new Pen(Color.White,2.5f){StartCap=LineCap.Round,EndCap=LineCap.Round}){
                    g.DrawLines(pen,new[]{new PointF(8,13),new PointF(8,8),new PointF(13,8)});g.DrawLines(pen,new[]{new PointF(19,8),new PointF(24,8),new PointF(24,13)});
                    g.DrawLines(pen,new[]{new PointF(8,19),new PointF(8,24),new PointF(13,24)});g.DrawLines(pen,new[]{new PointF(19,24),new PointF(24,24),new PointF(24,19)});
                }
            }return image;
        }
    }
}
