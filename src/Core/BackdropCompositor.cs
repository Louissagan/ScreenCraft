using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenCraft
{
    public static class Backdrop
    {
        static readonly int[,] gradients={{0x5678ED,0x9671D8},{0xEF786C,0xF5C178},{0x3BB49F,0xA3D4B4},{0x327696,0x7BCAD2},{0xF5C4A6,0xECA2A0},{0xB5A5D0,0xE0C9E8},{0xD8759D,0xEDABA1},{0x65B9B4,0x8C96D6},{0x243F53,0x526877},{0x4B5260,0x292F3B},{0xF5F6F8,0xDFE3EA},{0x343844,0x1B1F28}};
        public static int Count{get{return gradients.GetLength(0);}}
        static Color Rgb(int value){return Color.FromArgb(255,(value>>16)&255,(value>>8)&255,value&255);}
        static double Unit(Size size){return Math.Sqrt(Math.Max(1,(double)size.Width*size.Height));}
        static int Percent(int value,double scale){return (int)Math.Round(Math.Max(0,Math.Min(100,value))*scale/100);}
        public static int PaddingFor(Size size,Settings settings){return Percent(settings.BgPadding,Math.Max(80,Unit(size)*.2));}
        public static int RadiusFor(Size size,Settings settings){return Math.Min(Math.Min(size.Width,size.Height)/2,Percent(settings.BgRadius,Math.Max(28,Unit(size)*.05)));}
        public static int InsetFor(Size size,Settings settings){return Percent(settings.BgInset,Math.Max(32,Unit(size)*.08));}
        public sealed class ShotInfo:IDisposable
        {
            public Padding Inset;public Color Edge=Color.White;public Bitmap Blur;public string Key;
            internal Bitmap Source;internal Rectangle Area;
            public void Dispose(){if(Blur!=null){Blur.Dispose();Blur=null;}}
        }
        public static ShotInfo Study(Bitmap source,Rectangle area,Settings settings,ShotInfo previous)
        {
            var info=previous!=null&&previous.Source==source&&previous.Area==area?previous:new ShotInfo{Source=source,Area=area};
            int margin=InsetFor(area.Size,settings);info.Edge=source.GetPixel(Math.Max(0,area.Left),Math.Max(0,area.Top));info.Inset=new Padding(margin);
            if(settings.BgBalance){int[] existing=new int[4];for(int side=0;side<4;side++){
                int maximum=(side%2==0?area.Width:area.Height)/4;
                for(int offset=0;offset<maximum;offset++){bool uniform=true;for(int sample=1;sample<16;sample++){
                    int x=side==0?area.Left+offset:side==2?area.Right-1-offset:area.Left+sample*area.Width/16;
                    int y=side==1?area.Top+offset:side==3?area.Bottom-1-offset:area.Top+sample*area.Height/16;
                    Color pixel=source.GetPixel(x,y);if(Math.Abs(pixel.R-info.Edge.R)+Math.Abs(pixel.G-info.Edge.G)+Math.Abs(pixel.B-info.Edge.B)>12){uniform=false;break;}
                }if(!uniform)break;existing[side]++;}
            }int widest=Math.Max(Math.Max(existing[0],existing[1]),Math.Max(existing[2],existing[3]));info.Inset=new Padding(margin+widest-existing[0],margin+widest-existing[1],margin+widest-existing[2],margin+widest-existing[3]);}
            info.Key=area+"/"+info.Inset+"/"+info.Edge.ToArgb();return info;
        }
        public static Bitmap BlurOf(ShotInfo info,Bitmap source,Rectangle area)
        {
            if(info.Blur==null)using(var crop=source.Clone(area,PixelFormat.Format32bppArgb))info.Blur=FrozenGlass.Blur(crop,false,12);return info.Blur;
        }
        public static void Measure(Size content,Settings settings,bool even,out Size frame,out Rectangle inner){Rectangle card;Measure(content,settings,even,Padding.Empty,out frame,out card,out inner);}
        public static void Measure(Size content,Settings settings,bool even,Padding inset,out Size frame,out Rectangle card,out Rectangle inner)
        {
            int cardWidth=checked(content.Width+inset.Horizontal),cardHeight=checked(content.Height+inset.Vertical),pad=PaddingFor(new Size(cardWidth,cardHeight),settings);
            int width=checked(cardWidth+pad*2),height=checked(cardHeight+pad*2);double ratio=settings.BgRatio=="16:9"?16.0/9:settings.BgRatio=="4:3"?4.0/3:settings.BgRatio=="1:1"?1:0;
            if(ratio>0){width=Math.Max(width,(int)Math.Ceiling(height*ratio));height=Math.Max(height,(int)Math.Ceiling(width/ratio));}
            if(even){width=(width+1)&~1;height=(height+1)&~1;}frame=new Size(width,height);
            int align=Math.Max(0,Math.Min(8,settings.BgAlign)),col=align%3,row=align/3;
            int x=col==0?0:col==2?width-cardWidth:(width-cardWidth)/2,y=row==0?0:row==2?height-cardHeight:(height-cardHeight)/2;
            if(even){x&=~1;y&=~1;}card=new Rectangle(x,y,cardWidth,cardHeight);inner=new Rectangle(x+inset.Left,y+inset.Top,content.Width,content.Height);
        }
        public static Bitmap Compose(Bitmap content,Settings settings){return Compose(content,settings,null);}
        public static Bitmap Compose(Bitmap content,Settings settings,ShotInfo borrowed)
        {
            bool owns=borrowed==null;var info=borrowed??Study(content,new Rectangle(Point.Empty,content.Size),settings,null);
            Size size;Rectangle card,inner;Measure(content.Size,settings,false,info.Inset,out size,out card,out inner);var result=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            try{using(var g=Graphics.FromImage(result)){
                g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                if(settings.BgKind==1)g.Clear(Rgb(settings.BgColor));
                else if(settings.BgKind==2){var blur=BlurOf(info,info.Source,info.Area);g.DrawImage(blur,new Rectangle(Point.Empty,size));}
                else{int preset=Math.Max(0,Math.Min(Count-1,settings.BgPreset));using(var gradient=new LinearGradientBrush(new Rectangle(Point.Empty,size),Rgb(gradients[preset,0]),Rgb(gradients[preset,1]),45))g.FillRectangle(gradient,new Rectangle(Point.Empty,size));}
                int radius=RadiusFor(card.Size,settings);
                if(settings.BgShadow&&settings.BgShadowSize>0){int spread=Math.Max(2,Percent(settings.BgShadowSize,28));for(int level=spread;level>0;level--){var shadow=new RectangleF(card.X-level,card.Y-level+spread*.25f,card.Width+level*2,card.Height+level*2);using(var path=Theme.Round(shadow,radius+level))using(var ink=new SolidBrush(Color.FromArgb(Math.Max(1,8*(spread-level+1)/spread),Color.Black)))g.FillPath(ink,path);}}
                using(var path=Theme.Round(card,radius))using(var brush=new SolidBrush(info.Edge))g.FillPath(brush,path);
                var state=g.Save();using(var path=Theme.Round(card,radius))g.SetClip(path);g.DrawImage(content,inner,new Rectangle(Point.Empty,content.Size),GraphicsUnit.Pixel);g.Restore(state);
            }return result;}catch{result.Dispose();throw;}finally{if(owns)info.Dispose();}
        }
    }
}
