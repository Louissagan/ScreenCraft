using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScreenCraft
{
    public partial class RegionPicker
    {
        const int MagnifierColumns=19, MagnifierRows=13;
        bool colorCopied;
        internal bool SelectionMagnifierVisible
        {
            get{return mode==Mode.Image&&!annotationSelected&&!adjusting&&!measuring&&(!measured||down)&&Area.Contains(cur);}
        }

        internal static Rectangle PlaceMagnifier(Rectangle monitor,Point pointer,Size size,int gap)
        {
            int x=pointer.X+gap,y=pointer.Y+gap;
            if(x+size.Width>monitor.Right)x=pointer.X-size.Width-gap;
            if(y+size.Height>monitor.Bottom)y=pointer.Y-size.Height-gap;
            return new Rectangle(Math.Max(monitor.Left,Math.Min(monitor.Right-size.Width,x)),
                Math.Max(monitor.Top,Math.Min(monitor.Bottom-size.Height,y)),size.Width,size.Height);
        }

        void PaintLoupe(Graphics g)
        {
            int inset=Pz(8),footer=Pz(62);
            int cell=Math.Min(Pz(8),Math.Min((curMon.Width-2*inset)/MagnifierColumns,(curMon.Height-2*inset-footer)/MagnifierRows));
            if(cell<2)return;
            int lensWidth=MagnifierColumns*cell,lensHeight=MagnifierRows*cell;
            var size=new Size(Math.Min(curMon.Width,Math.Max(lensWidth+2*inset,Pz(180))),lensHeight+2*inset+footer);
            var card=PlaceMagnifier(curMon,cur,size,Pz(20));
            PreferencesLook.FloatingCard(g,card,Pz(10));
            var lens=new Rectangle(card.Left+(card.Width-lensWidth)/2,card.Top+inset,lensWidth,lensHeight);
            var pixel=PixelPoint();
            PaintMagnifiedPixels(g,lens,pixel,cell);

            Color color=frozen.GetPixel(pixel.X,pixel.Y);
            int left=lens.Left,top=lens.Bottom+Pz(6),line=Pz(18),chip=Pz(12);
            PreferencesLook.Round(g,new Rectangle(left,top+(line-chip)/2,chip,chip),color,Pz(3),PreferencesLook.Line);
            PreferencesLook.Text(g,ColorValues.Format(color,colorFormat,colorUpper),labelFont,
                new Rectangle(left+chip+Pz(6),top,lens.Width-chip-Pz(6),line),PreferencesLook.Ink,StringAlignment.Near);
            PreferencesLook.Text(g,"X "+(pixel.X+vs.X)+"   Y "+(pixel.Y+vs.Y),labelFont,
                new Rectangle(left,top+line,lens.Width,line),PreferencesLook.Muted,StringAlignment.Near);
            string hint=loupe?"单击复制色值":colorCopied?"已复制色值":"Ctrl+C 复制色值";
            PreferencesLook.Text(g,hint,labelFont,new Rectangle(left,top+2*line,lens.Width,line),
                colorCopied&&!loupe?CaptureLook.Blue:PreferencesLook.Muted,StringAlignment.Near);
        }

        void PaintMagnifiedPixels(Graphics g,Rectangle lens,Point pixel,int cell)
        {
            var requested=new Rectangle(pixel.X-MagnifierColumns/2,pixel.Y-MagnifierRows/2,MagnifierColumns,MagnifierRows);
            var sample=Rectangle.Intersect(requested,curMon);
            var target=new Rectangle(lens.Left+(sample.X-requested.X)*cell,lens.Top+(sample.Y-requested.Y)*cell,sample.Width*cell,sample.Height*cell);
            var state=g.Save();
            try{
                PreferencesLook.Fill(g,lens,PreferencesLook.Input);
                g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawImage(frozen,target,sample,GraphicsUnit.Pixel);
                g.SetClip(lens,CombineMode.Intersect);
                g.SmoothingMode=SmoothingMode.None;g.PixelOffsetMode=PixelOffsetMode.Default;
                var center=new Rectangle(lens.Left+MagnifierColumns/2*cell,lens.Top+MagnifierRows/2*cell,cell,cell);
                using(var halo=new Pen(Color.FromArgb(160,0,0,0),Pz(3)))PaintReticle(g,halo,lens,center);
                using(var ink=new Pen(Color.FromArgb(235,224,239,255),Pz(1)))PaintReticle(g,ink,lens,center);
                using(var accent=new Pen(CaptureLook.Blue,Pz(1)))g.DrawRectangle(accent,center.X,center.Y,center.Width-1,center.Height-1);
            }finally{g.Restore(state);}
        }

        static void PaintReticle(Graphics g,Pen pen,Rectangle lens,Rectangle center)
        {
            int x=center.Left+center.Width/2,y=center.Top+center.Height/2;
            g.DrawLine(pen,lens.Left,y,center.Left,y);g.DrawLine(pen,center.Right,y,lens.Right,y);
            g.DrawLine(pen,x,lens.Top,x,center.Top);g.DrawLine(pen,x,center.Bottom,x,lens.Bottom);
        }

        Point PixelPoint()
        {
            return new Point(Math.Max(curMon.Left,Math.Min(curMon.Right-1,cur.X)),
                Math.Max(curMon.Top,Math.Min(curMon.Bottom-1,cur.Y)));
        }

        void CopyColor(bool finish)
        {
            var pixel=PixelPoint();
            string value=ColorValues.Format(frozen.GetPixel(pixel.X,pixel.Y),colorFormat,colorUpper);
            try{
                if(WriteColorToClipboard!=null)WriteColorToClipboard(value);
                if(finish){pickedColor=value;Cancel();}
                else{colorCopied=true;Present();}
            }catch(System.Runtime.InteropServices.ExternalException ex){if(CaptureHost.Running!=null)CaptureHost.Running.Notify("复制颜色失败",ex.Message);}
        }
    }
}
