using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Stackshot
{
    // The capture is already frozen: blur its real pixels once per toolbar position, never read the live desktop.
    internal static class FrozenGlass
    {
        internal static Bitmap Create(Canvas canvas,Rectangle bounds)
        {
            using(var source=new Bitmap(bounds.Width,bounds.Height)){using(var g=Graphics.FromImage(source)){g.TranslateTransform(-bounds.Left,-bounds.Top);canvas.PaintCaptureBackdrop(g);}return Blur(source);}
        }
        internal static Bitmap Blur(Bitmap source,bool tint=true,int downsample=6)
        {
            int w=Math.Max(1,source.Width/downsample),h=Math.Max(1,source.Height/downsample);using(var small=new Bitmap(w,h,PixelFormat.Format32bppArgb)){
                using(var g=Graphics.FromImage(small)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(source,new Rectangle(0,0,w,h));}
                var bits=small.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);var pixels=new int[w*h];Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);
                for(int pass=0;pass<2;pass++){var next=new int[pixels.Length];for(int y=0;y<h;y++)for(int x=0;x<w;x++){int r=0,g=0,b=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int p=pixels[Math.Max(0,Math.Min(h-1,y+dy))*w+Math.Max(0,Math.Min(w-1,x+dx))];r+=(p>>16)&255;g+=(p>>8)&255;b+=p&255;}next[y*w+x]=unchecked((int)0xff000000)|(r/9<<16)|(g/9<<8)|b/9;}pixels=next;}
                Marshal.Copy(pixels,0,bits.Scan0,pixels.Length);small.UnlockBits(bits);var result=new Bitmap(source.Width,source.Height);using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(small,new Rectangle(0,0,result.Width,result.Height),0,0,small.Width,small.Height,GraphicsUnit.Pixel,attributes);if(tint)using(var wash=new SolidBrush(Color.FromArgb(211,251,252,254)))g.FillRectangle(wash,0,0,result.Width,result.Height);}return result;
            }
        }
        internal static bool PaintChild(Control child,Graphics graphics,Rectangle destination)
        {
            Point at=Point.Empty;Control node=child;while(node.Parent!=null){at.Offset(node.Location);node=node.Parent;var bar=node as CaptureBar;if(bar!=null){if(bar.Material==null)return false;graphics.DrawImage(bar.Material,destination,new Rectangle(at,child.Size),GraphicsUnit.Pixel);return true;}}return false;
        }
    }
}
