using System;
using System.Drawing;
using System.Globalization;
namespace ScreenCraft
{
    public static class ColorValues
    {
        public static string Format(Color color,string format,bool upper)
        {
            switch((format??"RGB").ToUpperInvariant())
            {
                case "RGB":return string.Format(CultureInfo.InvariantCulture,"rgb({0}, {1}, {2})",color.R,color.G,color.B);
                case "HSL":
                    double r=color.R/255.0,g=color.G/255.0,b=color.B/255.0,max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),d=max-min,l=(max+min)/2,s=d==0?0:d/(1-Math.Abs(2*l-1)),h=0;
                    if(d>0){h=max==r?((g-b)/d)%6:max==g?(b-r)/d+2:(r-g)/d+4;h*=60;if(h<0)h+=360;}
                    return string.Format(CultureInfo.InvariantCulture,"hsl({0:0.#}, {1:0.#}%, {2:0.#}%)",h,s*100,l*100);
                case "HEX":string hex="#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");return upper?hex:hex.ToLowerInvariant();
                default:return string.Format(CultureInfo.InvariantCulture,"rgb({0}, {1}, {2})",color.R,color.G,color.B);
            }
        }
        public static Color ReadBgra(IntPtr pixels,int width,int height,Point point)
        {
            if(width<1||height<1||pixels==IntPtr.Zero)throw new ArgumentException("像素缓冲区无效。");
            int x=Math.Max(0,Math.Min(width-1,point.X)),y=Math.Max(0,Math.Min(height-1,point.Y));
            int pixel=System.Runtime.InteropServices.Marshal.ReadInt32(pixels,checked((y*width+x)*4));return Color.FromArgb(255,(pixel>>16)&255,(pixel>>8)&255,pixel&255);
        }
    }
    public sealed class FrozenColorMap:IDisposable
    {
        readonly Bitmap pixels;public readonly Rectangle ScreenBounds;
        public FrozenColorMap(Bitmap source,Rectangle physicalBounds){if(source==null||source.Size!=physicalBounds.Size||physicalBounds.Width<1||physicalBounds.Height<1)throw new ArgumentException("冻结图片必须与物理屏幕范围匹配。");pixels=new Bitmap(source);ScreenBounds=physicalBounds;}
        public Color Sample(Point physicalPoint){if(!ScreenBounds.Contains(physicalPoint))throw new ArgumentOutOfRangeException("physicalPoint","取色点不在冻结范围内。");return pixels.GetPixel(physicalPoint.X-ScreenBounds.Left,physicalPoint.Y-ScreenBounds.Top);}
        public static Point DipToPixel(PointF monitorLocalDip,Point physicalMonitorOrigin,float dpi)
        {
            if(float.IsNaN(dpi)||float.IsInfinity(dpi)||dpi<48||dpi>768||float.IsNaN(monitorLocalDip.X)||float.IsNaN(monitorLocalDip.Y)||float.IsInfinity(monitorLocalDip.X)||float.IsInfinity(monitorLocalDip.Y))throw new ArgumentException("DPI 或坐标无效。");
            return new Point(checked(physicalMonitorOrigin.X+(int)Math.Round(monitorLocalDip.X*dpi/96)),checked(physicalMonitorOrigin.Y+(int)Math.Round(monitorLocalDip.Y*dpi/96)));
        }
        public void Dispose(){pixels.Dispose();}
    }
    public partial class Canvas
    {
        Action<Color> sampledColor;
        public void CancelColorPick(){sampledColor=null;Cursor=System.Windows.Forms.Cursors.Cross;}
        public void BeginColorPick(Action<Color> selected){if(selected==null)throw new ArgumentNullException("selected");CommitText();sampledColor=selected;Cursor=System.Windows.Forms.Cursors.Cross;}
        bool TrySampleColor(Point position)
        {
            if(sampledColor==null)return false;
            if(!ScreenRect(Crop).Contains(position))return true;
            PointF p=ToImg(position,true);Color c=Img.GetPixel(Math.Max(0,Math.Min(Img.Width-1,(int)Math.Round(p.X))),Math.Max(0,Math.Min(Img.Height-1,(int)Math.Round(p.Y))));
            var action=sampledColor;CancelColorPick();action(c);return true;
        }
    }
    public partial class RegionPicker
    {
        string colorFormat="RGB";bool colorUpper=true,colorOnly;string pickedColor;
        internal Action<string> WriteColorToClipboard=delegate(string text){System.Windows.Forms.Clipboard.SetText(text);};
        public static string PickColor(string format,bool upper)
        {
            var bounds=System.Windows.Forms.SystemInformation.VirtualScreen;
            var grab=Dib.FromScreen(bounds);
            using(var picker=new RegionPicker(grab,bounds,Grabber.Windows(),Mode.Image))
            {
                picker.colorFormat=format;picker.colorUpper=upper;picker.colorOnly=true;picker.loupe=true;picker.WriteColorToClipboard=null;
                var mouse=System.Windows.Forms.Control.MousePosition;picker.cur=new Point(mouse.X-bounds.X,mouse.Y-bounds.Y);picker.TrackMonitor();picker.Present();picker.ShowDialog();return picker.pickedColor;
            }
        }
    }
}
