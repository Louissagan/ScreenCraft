using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace Stackshot
{
    public partial class Canvas
    {
        public float OutputCornerRadius{get;private set;}
        public void SetOutputCornerRadius(float radius){radius=Math.Max(0,Math.Min(1000,radius));if(radius==OutputCornerRadius)return;CommitText();Commit();OutputCornerRadius=radius;Fire(Changed);Fire(StateChanged);Invalidate();}
        static Bitmap RoundOutput(Bitmap owned,float radius)
        {
            if(radius<=0)return owned;var rounded=new Bitmap(owned.Width,owned.Height,PixelFormat.Format32bppArgb);
            try{using(var g=Graphics.FromImage(rounded))using(var brush=new TextureBrush(owned,WrapMode.Clamp))using(var path=Theme.Round(new RectangleF(0,0,owned.Width,owned.Height),Math.Min(radius,Math.Min(owned.Width,owned.Height)/2f))){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.FillPath(brush,path);}return rounded;}catch{rounded.Dispose();throw;}finally{owned.Dispose();}
        }
    }
}
