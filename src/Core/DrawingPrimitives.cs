using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ScreenCraft
{
    public static class Theme
    {
        public static void Init(){Fonts.Initialize();}
        public static GraphicsPath Round(RectangleF bounds,float radius)
        {
            var path=new GraphicsPath();float r=Math.Max(0,Math.Min(radius,Math.Min(bounds.Width,bounds.Height)*.5f));
            if(bounds.Width<=0||bounds.Height<=0)return path;if(r<.01){path.AddRectangle(bounds);return path;}
            const float handle=.55228475f;float d=r*handle,x=bounds.Left,y=bounds.Top,right=bounds.Right,bottom=bounds.Bottom;
            path.StartFigure();path.AddLine(x+r,y,right-r,y);path.AddBezier(right-r,y,right-r+d,y,right,y+r-d,right,y+r);
            path.AddLine(right,y+r,right,bottom-r);path.AddBezier(right,bottom-r,right,bottom-r+d,right-r+d,bottom,right-r,bottom);
            path.AddLine(right-r,bottom,x+r,bottom);path.AddBezier(x+r,bottom,x+r-d,bottom,x,bottom-r+d,x,bottom-r);
            path.AddLine(x,bottom-r,x,y+r);path.AddBezier(x,y+r,x,y+r-d,x+r-d,y,x+r,y);path.CloseFigure();return path;
        }
    }
    public static class Fonts
    {
        static readonly HashSet<string> installed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        [ThreadStatic] static Dictionary<string,Font> cache;
        static Fonts(){using(var fonts=new InstalledFontCollection())foreach(var family in fonts.Families){installed.Add(family.Name);family.Dispose();}}
        internal static void Initialize(){Has("Microsoft YaHei UI");}
        public static bool Has(string family){return family!=null&&installed.Contains(family);}
        public static Font Get(string family,float pixels)
        {
            if(float.IsNaN(pixels)||float.IsInfinity(pixels))throw new ArgumentOutOfRangeException("pixels");
            string selected=Has(family)?family:"Segoe UI";float size=Math.Max(.5f,Math.Min(1024,(float)Math.Round(pixels*4)/4));string key=selected+":"+size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if(cache==null)cache=new Dictionary<string,Font>();Font font;if(!cache.TryGetValue(key,out font)){font=new Font(selected,size,FontStyle.Regular,GraphicsUnit.Pixel);cache.Add(key,font);}return font;
        }
    }
}
