using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    // Immutable frozen pixels and a single cached dim layer. Moving the crop never moves this surface HWND.
    internal sealed class CaptureScene:IDisposable
    {
        readonly Bitmap source,dimmed;
        internal CaptureScene(Bitmap borrowedSource,int dim){source=borrowedSource;dimmed=new Bitmap(source.Width,source.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(dimmed)){g.DrawImageUnscaled(source,0,0);using(var b=new SolidBrush(Color.FromArgb(SelectionAppearance.DimAlpha(dim),Color.Black)))g.FillRectangle(b,0,0,source.Width,source.Height);}}
        internal void Paint(Graphics g,Rectangle selection)
        {
            var visible=Rectangle.Intersect(Rectangle.Ceiling(g.VisibleClipBounds),new Rectangle(Point.Empty,source.Size));
            if(visible.Width<=0||visible.Height<=0)return;
            var state=g.Save();
            g.CompositingMode=CompositingMode.SourceCopy;
            g.DrawImage(dimmed,visible,visible,GraphicsUnit.Pixel);
            var bright=Rectangle.Intersect(selection,visible);
            if(bright.Width>0&&bright.Height>0)g.DrawImage(source,bright,bright,GraphicsUnit.Pixel);
            g.Restore(state);
        }
        public void Dispose(){dimmed.Dispose();}
    }
    public partial class Canvas
    {
        internal bool FullCaptureSurface;
        internal int CaptureDim=SelectionAppearance.DefaultDimPercent;
        CaptureScene captureScene;
        internal int SceneBuildCount;
        internal Func<System.Windows.Forms.MouseEventArgs,bool> CaptureDown,CaptureMove,CaptureUp;
        internal Action<Graphics> PaintCaptureChrome;
        bool PaintFrozenSurface(System.Windows.Forms.PaintEventArgs e)
        {
            if(!FullCaptureSurface)return false;Fit();var g=e.Graphics;PaintCaptureBackdrop(g);var state=g.Save();using(var path=Theme.Round(Crop,Math.Min(OutputCornerRadius,Math.Min(Crop.Width,Crop.Height)/2)))g.SetClip(path,CombineMode.Intersect);Painter.DrawMarks(g,shapes,cur!=null&&cur.Kind!=Tool.Crop?cur:null,editing,Img,Crop);PaintEditingAnchor(g);g.Restore(state);
            if(box!=null){if(BoxStale())PlaceBox();BoxLabel(g);}if(hover!=null&&hover!=selected&&drag==Drag.None&&box==null)Outline(g,hover,false);if(selected!=null&&box==null){if(SelectionCount>1)OutlineGroup(g);else Outline(g,selected,true);}if(PaintCaptureChrome!=null)PaintCaptureChrome(g);return true;
        }
        internal void PaintCaptureBackdrop(Graphics g)
        {
            if(captureScene==null){captureScene=new CaptureScene(Img,CaptureDim);SceneBuildCount++;}captureScene.Paint(g,Rectangle.Round(Crop));
            if(OutputCornerRadius>0)using(var outside=new Region(Crop))using(var path=Theme.Round(Crop,Math.Min(OutputCornerRadius,Math.Min(Crop.Width,Crop.Height)/2)))using(var dim=new SolidBrush(Color.FromArgb(SelectionAppearance.DimAlpha(CaptureDim),Color.Black))){outside.Exclude(path);g.FillRegion(dim,outside);}
        }
        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e){if(!FullCaptureSurface)base.OnPaintBackground(e);}
    }
}
