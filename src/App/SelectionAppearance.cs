using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace Stackshot
{
    public static class SelectionAppearance
    {
        public const int DefaultDimPercent=70;
        public const int DefaultBorderWidth=4;
        public static int BorderWidth(int width){return Math.Max(2,Math.Min(6,width));}
        internal static int FrameOutset(int width){return BorderWidth(width)/2+4;}
        public static Color BorderColor(string name){return name=="green"?Color.FromArgb(0,196,143):name=="orange"?Color.FromArgb(255,142,37):CaptureLook.Blue;}
        internal static void PaintCrosshair(Graphics g,Point at,Rectangle bounds,Color color){var state=g.Save();g.SmoothingMode=SmoothingMode.None;using(var halo=new Pen(Color.FromArgb(90,255,255,255),3))using(var line=new Pen(Color.FromArgb(190,color),1)){g.DrawLine(halo,bounds.Left,at.Y,bounds.Right,at.Y);g.DrawLine(halo,at.X,bounds.Top,at.X,bounds.Bottom);g.DrawLine(line,bounds.Left,at.Y,bounds.Right,at.Y);g.DrawLine(line,at.X,bounds.Top,at.X,bounds.Bottom);}g.Restore(state);}
        public static int Percent(int value){return Math.Max(45,Math.Min(85,value));}
        public static byte DimAlpha(int percent){return (byte)Math.Round(Percent(percent)*255.0/100);}
        public static byte SourceAlpha(int percent){return (byte)(255-DimAlpha(percent));}
        public static void PaintFrame(Graphics graphics,Rectangle selection,int width=DefaultBorderWidth,Color? color=null)
        {
            if(selection.Width<1||selection.Height<1)return;var state=graphics.Save();graphics.SmoothingMode=SmoothingMode.None;
            width=BorderWidth(width);using(var halo=new Pen(Color.FromArgb(104,15,23,35),width+2))using(var accent=new Pen(color??CaptureLook.Blue,width))using(var white=new Pen(Color.White,1)){
                graphics.DrawRectangle(halo,Rectangle.Inflate(selection,1,1));graphics.DrawRectangle(accent,Rectangle.Inflate(selection,1,1));graphics.DrawRectangle(white,selection);
                // The outer stroke can be clipped at a monitor edge. Retain one accent pixel inside every side.
                if(selection.Width>2&&selection.Height>2)using(var inner=new Pen(color??CaptureLook.Blue,1))graphics.DrawRectangle(inner,Rectangle.Inflate(selection,-1,-1));
            }graphics.Restore(state);
        }
        public static void PaintHandle(Graphics graphics,Rectangle bounds,Color? color=null){using(var fill=new SolidBrush(Color.White))using(var edge=new Pen(color??CaptureLook.Blue,2)){graphics.FillRectangle(fill,bounds);var r=Rectangle.Inflate(bounds,-1,-1);graphics.DrawRectangle(edge,r);}}
    }
}
