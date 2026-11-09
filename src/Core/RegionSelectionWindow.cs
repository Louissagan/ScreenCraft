using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenCraft
{
    public partial class RegionPicker:Form
    {
        public enum Mode { Image,Video,Gif,Scroll,Text }
        readonly Dib bright;
        readonly Bitmap frozen;
        readonly CaptureScene scene;
        readonly Rectangle vs;
        readonly List<Grabber.Win> wins;
        readonly Mode mode;
        readonly bool retainSelection;
        internal Func<Keys> ModifierSource=delegate{return Control.ModifierKeys;};
        internal Action Accepted;
        internal Rectangle Result{get{return result;}}
        Point cur,start;
        bool down,dragging,adjusting,loupe,measuring,measured,resourcesReleased;
        Point measureStart,measureEnd;
        Rectangle sel,hover,curMon,barRect,result;
        IntPtr hoverHandle,resultWindow;
        Bitmap crop;
        Rectangle cropAt;
        int activeHandle=-1;
        Point resizeStart;
        Rectangle resizeOriginal;
        float monitorScale=1;
        Font labelFont;
        Mode chosen;
        RegionPicker(Dib pixels,Rectangle bounds,List<Grabber.Win> windows,Mode purpose):this(pixels,bounds,windows,purpose,false,false){}
        internal RegionPicker(Dib pixels,Rectangle bounds,List<Grabber.Win> windows,Mode purpose,bool allInOne,bool adjustFirst)
        {
            bright=pixels;vs=bounds;wins=windows??new List<Grabber.Win>();mode=chosen=purpose;retainSelection=allInOne||adjustFirst;
            try{
            frozen=bright.ToBitmap();var preferences=CaptureHost.Running==null?null:CaptureHost.Running.ColorPreferences;
            scene=new CaptureScene(frozen,preferences==null?SelectionAppearance.DefaultDimPercent:preferences.SelectionDimPercent);
            if(preferences!=null){colorFormat=preferences.ColorFormat;colorUpper=preferences.ColorUpper;}
            labelFont=new Font("Microsoft YaHei UI",12,GraphicsUnit.Pixel);Text="ScreenCraft · 选择截图范围";FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;TopMost=true;KeyPreview=true;Bounds=bounds;Cursor=Cursors.Cross;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            var mouse=MousePosition;cur=new Point(mouse.X-bounds.X,mouse.Y-bounds.Y);TrackMonitor();UpdateHover();
            }catch{ReleasePixels();throw;}
        }
        public static Rectangle Pick(Mode purpose,out Bitmap image,out Rectangle imageBounds,out IntPtr window){Mode chosen;return Pick(purpose,false,false,out image,out imageBounds,out window,out chosen);}
        public static Rectangle Pick(Mode purpose,bool allInOne,out Bitmap image,out Rectangle imageBounds,out IntPtr window,out Mode chosen){return Pick(purpose,allInOne,false,out image,out imageBounds,out window,out chosen);}
        public static Rectangle Pick(Mode purpose,bool allInOne,bool adjustFirst,out Bitmap image,out Rectangle imageBounds,out IntPtr window,out Mode chosen)
        {
            image=null;imageBounds=Rectangle.Empty;window=IntPtr.Zero;chosen=purpose;var bounds=SystemInformation.VirtualScreen;var pixels=Dib.FromScreen(bounds);
            using(var picker=new RegionPicker(pixels,bounds,Grabber.Windows(),purpose,allInOne,adjustFirst)){
                picker.ShowDialog();image=picker.crop;picker.crop=null;imageBounds=picker.cropAt;window=picker.resultWindow;chosen=picker.chosen;return picker.result;
            }
        }
        protected override CreateParams CreateParams{get{var parameters=base.CreateParams;parameters.ExStyle|=0x88;return parameters;}}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.SetWindowDisplayAffinity(Handle,0x11);}
        Rectangle Area{get{return new Rectangle(0,0,vs.Width,vs.Height);}}
        int Pz(float value){return Math.Max(1,(int)Math.Round(value*monitorScale));}
        void TrackMonitor()
        {
            var screen=Screen.FromPoint(new Point(cur.X+vs.X,cur.Y+vs.Y));curMon=Rectangle.Intersect(screen.Bounds,vs);curMon.Offset(-vs.X,-vs.Y);if(curMon.Width<1||curMon.Height<1)curMon=Area;
            float scale=CaptureHost.ScaleFor(screen);if(Math.Abs(scale-monitorScale)>.01f){monitorScale=scale;var old=labelFont;labelFont=new Font("Microsoft YaHei UI",Pz(12),GraphicsUnit.Pixel);if(old!=null)old.Dispose();}
        }
        void UpdateHover()
        {
            if(down||adjusting||loupe||measuring)return;Point physical=new Point(cur.X+vs.X,cur.Y+vs.Y);hover=curMon;hoverHandle=IntPtr.Zero;
            foreach(var window in wins)if(window.Bounds.Contains(physical)){var region=SmartHover(window.Bounds,window.Handle,physical);hover=Rectangle.Intersect(region,vs);hover.Offset(-vs.X,-vs.Y);hoverHandle=region==window.Bounds?window.Handle:IntPtr.Zero;break;}
        }
        void Present(){if(!IsDisposed)Invalidate();}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnPaint(PaintEventArgs e)
        {
            if(resourcesReleased)return;Rectangle selected=down&&dragging||adjusting?sel:loupe||measuring||measured?Rectangle.Empty:hover;scene.Paint(e.Graphics,selected);
            if(GuidesVisible)SelectionAppearance.PaintCrosshair(e.Graphics,cur,Area,CaptureLook.Blue);
            if(!selected.IsEmpty){SelectionAppearance.PaintFrame(e.Graphics,selected);if(adjusting)foreach(var point in CaptureOverlayGeometry.Handles(selected))SelectionAppearance.PaintHandle(e.Graphics,new Rectangle(point.X-Pz(3),point.Y-Pz(3),Pz(6),Pz(6)));
                var label=new Rectangle(selected.Left,Math.Max(curMon.Top,selected.Top-Pz(28)),Pz(128),Pz(24));PreferencesLook.Round(e.Graphics,label,Color.FromArgb(225,32,38,49),Pz(4),null);PreferencesLook.Text(e.Graphics,selected.Width+" × "+selected.Height,labelFont,label,Color.White,StringAlignment.Center);
            }
            if(adjusting){barRect=new Rectangle(Math.Max(curMon.Left,Math.Min(curMon.Right-Pz(270),sel.Right-Pz(270))),Math.Max(curMon.Top,Math.Min(curMon.Bottom-Pz(36),sel.Bottom+Pz(8))),Pz(270),Pz(32));PreferencesLook.FloatingCard(e.Graphics,barRect,Pz(6));PreferencesLook.Text(e.Graphics,"Enter 完成  ·  Esc 取消",labelFont,barRect,PreferencesLook.Ink,StringAlignment.Center);}
            if(loupe)PaintLoupe(e.Graphics);
            if(measuring||measured){using(var pen=new Pen(CaptureLook.Blue,Pz(2)))e.Graphics.DrawLine(pen,measureStart,measureEnd);double length=Math.Sqrt(Math.Pow(measureEnd.X-measureStart.X,2)+Math.Pow(measureEnd.Y-measureStart.Y,2));var label=new Rectangle(measureEnd.X+Pz(12),measureEnd.Y+Pz(12),Pz(180),Pz(28));PreferencesLook.Round(e.Graphics,label,Color.White,Pz(4),null);PreferencesLook.Text(e.Graphics,length.ToString("0.0")+" px",labelFont,label,PreferencesLook.Ink,StringAlignment.Center);}
        }
        void PaintLoupe(Graphics g)
        {
            int cell=Pz(12),side=cell*9,width=Math.Max(side+Pz(16),Pz(220)),height=side+Pz(70);int x=cur.X+Pz(24),y=cur.Y+Pz(24);if(x+width>curMon.Right)x=cur.X-width-Pz(24);if(y+height>curMon.Bottom)y=cur.Y-height-Pz(24);x=Math.Max(curMon.Left,Math.Min(curMon.Right-width,x));y=Math.Max(curMon.Top,Math.Min(curMon.Bottom-height,y));
            var card=new Rectangle(x,y,width,height);PreferencesLook.FloatingCard(g,card,Pz(8));
            var state=g.Save();try{for(int row=-4;row<=4;row++)for(int col=-4;col<=4;col++){using(var brush=new SolidBrush(PixelAt(cur.X+col,cur.Y+row)))g.FillRectangle(brush,x+Pz(8)+(col+4)*cell,y+Pz(8)+(row+4)*cell,cell,cell);}using(var line=new Pen(CaptureLook.Blue,Pz(2)))g.DrawRectangle(line,x+Pz(8)+4*cell,y+Pz(8)+4*cell,cell,cell);}finally{g.Restore(state);}
            PreferencesLook.Text(g,ColorValues.Format(PixelAt(cur.X,cur.Y),colorFormat,colorUpper),labelFont,new Rectangle(x,y+side+Pz(14),width,Pz(24)),PreferencesLook.Ink,StringAlignment.Center);PreferencesLook.Text(g,(cur.X+vs.X)+", "+(cur.Y+vs.Y),labelFont,new Rectangle(x,y+side+Pz(38),width,Pz(22)),PreferencesLook.Muted,StringAlignment.Center);
        }
        Color PixelAt(int x,int y){return frozen.GetPixel(Math.Max(0,Math.Min(frozen.Width-1,x)),Math.Max(0,Math.Min(frozen.Height-1,y)));}
        void CopyColor()
        {
            string value=ColorValues.Format(PixelAt(cur.X,cur.Y),colorFormat,colorUpper);
            try{if(WriteColorToClipboard!=null)WriteColorToClipboard(value);pickedColor=value;Cancel();}catch(System.Runtime.InteropServices.ExternalException ex){if(CaptureHost.Running!=null)CaptureHost.Running.Notify("复制颜色失败",ex.Message);}
        }
        Point Clamp(Point point){return new Point(Math.Max(0,Math.Min(vs.Width,point.X)),Math.Max(0,Math.Min(vs.Height,point.Y)));}
        void DragTo(Point point)
        {
            cur=Clamp(point);if(activeHandle>=0){sel=CaptureOverlayGeometry.DragSelection(resizeOriginal,activeHandle,new Point(cur.X-resizeStart.X,cur.Y-resizeStart.Y),Area);return;}
            if(Math.Abs(cur.X-start.X)+Math.Abs(cur.Y-start.Y)>=3)dragging=true;
            if(dragging)sel=Rectangle.FromLTRB(Math.Min(start.X,cur.X),Math.Min(start.Y,cur.Y),Math.Max(start.X,cur.X),Math.Max(start.Y,cur.Y));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button==MouseButtons.Right){Cancel();return;}if(e.Button!=MouseButtons.Left)return;cur=Clamp(e.Location);guideSeen=true;
            if(loupe){CopyColor();return;}if((ModifierSource()&Keys.Shift)!=0){measuring=true;measured=false;measureStart=measureEnd=cur;Capture=true;Present();return;}
            if(adjusting){var handles=CaptureOverlayGeometry.Handles(sel);activeHandle=-1;for(int i=0;i<handles.Length;i++)if(Math.Abs(handles[i].X-cur.X)<=Pz(7)&&Math.Abs(handles[i].Y-cur.Y)<=Pz(7)){activeHandle=i;break;}if(activeHandle<0&&sel.Contains(cur))activeHandle=8;if(activeHandle>=0){resizeStart=cur;resizeOriginal=sel;down=true;Capture=true;return;}adjusting=false;}
            start=cur;down=true;dragging=false;Capture=true;ClearSmartHover();Present();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);cur=Clamp(e.Location);guideSeen=true;TrackMonitor();
            if(measuring)measureEnd=cur;else if(down)DragTo(cur);else UpdateHover();Present();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;
            if(measuring){measureEnd=Clamp(e.Location);measuring=false;measured=true;Capture=false;Present();return;}if(!down)return;
            DragTo(e.Location);down=false;Capture=false;
            if(activeHandle>=0){activeHandle=-1;Present();return;}
            var selected=dragging?sel:hover;var window=dragging?IntPtr.Zero:hoverHandle;dragging=false;
            if(retainSelection){sel=selected;adjusting=true;Present();return;}Accept(selected,window);
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&down){down=false;dragging=false;activeHandle=-1;sel=Rectangle.Empty;UpdateHover();Present();}if(!Capture&&measuring){measuring=false;measured=false;Present();}}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){if(!colorOnly&&(loupe||measured)){loupe=measured=false;Present();}else Cancel();e.Handled=true;return;}
            if(e.KeyCode==Keys.C&&!colorOnly){loupe=!loupe;Present();e.Handled=true;return;}
            if(e.KeyCode==Keys.Enter){if(loupe)CopyColor();else Accept(adjusting?sel:hover,hoverHandle);e.Handled=true;return;}
            if(adjusting&&(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right||e.KeyCode==Keys.Up||e.KeyCode==Keys.Down)){sel=CaptureKeyboard.AdjustRange(sel,e.KeyData,Area);Present();e.Handled=true;}
        }
        void Accept(Rectangle selected,IntPtr window)
        {
            var local=Rectangle.Intersect(selected,Area);if(local.Width<1||local.Height<1){Cancel();return;}ClearSmartHover();result=local;result.Offset(vs.X,vs.Y);resultWindow=window;
            if(annotationHandoff){var image=bright.ToBitmap();if(annotationEditor==null)annotationDesktop=image;else try{annotationEditor(image,vs,result,this);}catch(Exception ex){image.Dispose();annotationError=ex;}}
            else if(chosen==Mode.Image||chosen==Mode.Text){crop=frozen.Clone(local,PixelFormat.Format32bppArgb);cropAt=result;}
            if(Accepted!=null)Accepted();DialogResult=DialogResult.OK;Close();
        }
        void Cancel(){result=Rectangle.Empty;DialogResult=DialogResult.Cancel;Close();}
        void ReleasePixels(){if(resourcesReleased)return;resourcesReleased=true;if(scene!=null)scene.Dispose();if(frozen!=null)frozen.Dispose();if(bright!=null)bright.Dispose();if(labelFont!=null)labelFont.Dispose();if(crop!=null){crop.Dispose();crop=null;}if(annotationDesktop!=null){annotationDesktop.Dispose();annotationDesktop=null;}}
        protected override void OnFormClosed(FormClosedEventArgs e){DisposeSmartRegions();base.OnFormClosed(e);}
    }
}
