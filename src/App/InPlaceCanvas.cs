using System;
using System.Drawing;
using System.Drawing.Imaging;
namespace ScreenCraft
{
    public partial class Canvas
    {
        public bool InPlaceViewport;
        public void ChangeDefaultStyle(Action<AnnotationStyle> change){change(DefaultStyle);RefreshPolygonStyle();PlaceBox();Fire(StateChanged);Invalidate();}
        public void SetCaptureSelection(Rectangle selection,bool checkpoint)
        {
            var bounds=new Rectangle(0,0,Img.Width,Img.Height);
            if(selection.Width<1||selection.Height<1||!bounds.Contains(selection))throw new ArgumentException("截图范围超出冻结图像。");
            CommitText();if(Crop==selection)return;if(checkpoint)Commit();Crop=selection;Fire(StateChanged);Invalidate();
        }
        AnnotationDocument ExportCaptureDocument()
        {
            Rectangle area=Rectangle.Round(Crop);var marks=AnnotationSnapshot;foreach(var shape in marks){if(shape.Kind==Tool.Pixelate&&shape.MosaicBlock==0)shape.MosaicBlock=Math.Max(12,Math.Min(Img.Width,Img.Height)/45);shape.Offset(shape,-area.X,-area.Y);}
            return new AnnotationDocument(Img.Clone(area,PixelFormat.Format32bppArgb),marks,new RectangleF(0,0,area.Width,area.Height),BgOn,Bg){OutputCornerRadius=OutputCornerRadius};
        }
    }
    public static class CaptureOverlayGeometry
    {
        public static Rectangle LocalSelection(Rectangle screenSelection,Rectangle desktop)
        {
            if(desktop.Width<1||desktop.Height<1||screenSelection.Width<1||screenSelection.Height<1||!desktop.Contains(screenSelection))throw new ArgumentException("截图范围不在冻结桌面内。");screenSelection.Offset(-desktop.X,-desktop.Y);return screenSelection;
        }
        public static Rectangle WorkArea(Rectangle selection,Rectangle[] monitors,Rectangle fallback)
        {
            Rectangle best=fallback;long largest=-1;if(monitors!=null)foreach(var area in monitors){var visible=Rectangle.Intersect(selection,area);long size=(long)visible.Width*visible.Height;if(size>largest){largest=size;best=area;}}return best;
        }
        public static Rectangle PlaceToolbar(Rectangle selection,Size requested,Rectangle area,bool preferTop=false)
        {
            int width=Math.Max(1,Math.Min(requested.Width,area.Width)),height=Math.Max(1,Math.Min(requested.Height,area.Height));int x=Math.Max(area.Left,Math.Min(area.Right-width,selection.Right-width)),y=preferTop?selection.Top-height-8:selection.Bottom+8;if(preferTop&&y<area.Top)y=selection.Bottom+8;else if(!preferTop&&y+height>area.Bottom)y=selection.Top-height-8;y=Math.Max(area.Top,Math.Min(area.Bottom-height,y));return new Rectangle(x,y,width,height);
        }
        public static Point[] Handles(Rectangle r){return new[]{new Point(r.Left,r.Top),new Point(r.Left+r.Width/2,r.Top),new Point(r.Right,r.Top),new Point(r.Right,r.Top+r.Height/2),new Point(r.Right,r.Bottom),new Point(r.Left+r.Width/2,r.Bottom),new Point(r.Left,r.Bottom),new Point(r.Left,r.Top+r.Height/2)};}
        internal static Rectangle PlaceDimensions(Rectangle selection,Size size,Rectangle tools,Rectangle properties,Rectangle area)
        {
            var candidates=new[]{new Point(selection.Left,selection.Top-size.Height-5),new Point(selection.Right-size.Width,selection.Top-size.Height-5),new Point(selection.Left,selection.Bottom+5),new Point(selection.Right-size.Width,selection.Bottom+5),new Point(selection.Left+8,selection.Top+8),new Point(selection.Left+8,selection.Bottom-size.Height-8)};Rectangle result=Rectangle.Empty;
            foreach(var at in candidates){result=new Rectangle(Math.Max(area.Left,Math.Min(area.Right-size.Width,at.X)),Math.Max(area.Top,Math.Min(area.Bottom-size.Height,at.Y)),size.Width,size.Height);if(!result.IntersectsWith(tools)&&!result.IntersectsWith(properties))return result;}return result;
        }
        public static Rectangle DragSelection(Rectangle original,int handle,Point delta,Rectangle bounds)
        {
            if(handle==8){int x=Math.Max(bounds.Left,Math.Min(bounds.Right-original.Width,original.X+delta.X)),y=Math.Max(bounds.Top,Math.Min(bounds.Bottom-original.Height,original.Y+delta.Y));return new Rectangle(x,y,original.Width,original.Height);}
            int left=original.Left,top=original.Top,right=original.Right,bottom=original.Bottom;
            if(handle==0||handle==6||handle==7)left=Math.Max(bounds.Left,Math.Min(right-1,left+delta.X));if(handle==0||handle==1||handle==2)top=Math.Max(bounds.Top,Math.Min(bottom-1,top+delta.Y));if(handle==2||handle==3||handle==4)right=Math.Min(bounds.Right,Math.Max(left+1,right+delta.X));if(handle==4||handle==5||handle==6)bottom=Math.Min(bounds.Bottom,Math.Max(top+1,bottom+delta.Y));return Rectangle.FromLTRB(left,top,right,bottom);
        }
        public static void PaintSurround(Graphics graphics,Bitmap frozen,Rectangle selection,int dimPercent=SelectionAppearance.DefaultDimPercent)
        {
            graphics.DrawImageUnscaled(frozen,0,0);using(var dim=new SolidBrush(Color.FromArgb(SelectionAppearance.DimAlpha(dimPercent),0,0,0))){graphics.FillRectangle(dim,0,0,frozen.Width,selection.Top);graphics.FillRectangle(dim,0,selection.Bottom,frozen.Width,Math.Max(0,frozen.Height-selection.Bottom));graphics.FillRectangle(dim,0,selection.Top,selection.Left,selection.Height);graphics.FillRectangle(dim,selection.Right,selection.Top,Math.Max(0,frozen.Width-selection.Right),selection.Height);}
        }
    }
}
