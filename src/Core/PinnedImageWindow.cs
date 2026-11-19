using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenCraft
{
    public partial class PinWindow:Form
    {
        static readonly PinRegistry<PinWindow> registry=new PinRegistry<PinWindow>();
        public static System.Collections.Generic.List<PinWindow> All{get{return registry.Values;}}
        public readonly PinState State=new PinState();
        public string PinId{get{return State.Id;}}
        readonly Rectangle? testArea;
        readonly float scale;
        readonly int Pad;
        AnnotationDocument document;
        Bitmap img,painted;
        Dib surface;
        string drawnFor;
        Size body;
        float zoom=1,minZoom,maxZoom,zoomBeforeThumbnail=1;
        bool released,over,closeOver;
        int P(float dip){return Math.Max(1,(int)Math.Round(dip*scale));}
        public static PinWindow Open(Bitmap owned){return Open(AnnotationDocument.FromImage(owned));}
        public static PinWindow Open(AnnotationDocument owned){var window=CreateHidden(owned,null);try{window.Show();return window;}catch{window.Dispose();throw;}}
        internal static PinWindow CreateHidden(AnnotationDocument owned,Rectangle? workArea){try{return new PinWindow(owned,workArea);}catch{owned.Dispose();throw;}}
        PinWindow(AnnotationDocument owned,Rectangle? workArea)
        {
            document=owned;testArea=workArea;State.QuarterTurns=owned.PinQuarterTurns;State.FlipX=owned.PinFlipX;State.FlipY=owned.PinFlipY;
            try{
            using(var raw=owned.Render())img=PinViews.Render(raw,State.QuarterTurns,State.FlipX,State.FlipY);
            var screen=workArea.HasValue?null:Grabber.CurrentScreen();scale=screen==null?1:CaptureHost.ScaleFor(screen);Pad=P(24);var area=workArea??screen.WorkingArea;
            FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;TopMost=true;KeyPreview=true;Text="ScreenCraft · 贴图 "+PinId.Substring(0,6);Icon=CaptureHost.AppIcon;
            zoom=FitZoom(img.Size,area.Size,out minZoom);UpdateMaxZoom(area);body=ScaledSize(img.Size,zoom);Bounds=PinGeometry.Place(new Size(body.Width+Pad*2,body.Height+Pad*2),area,workArea.HasValue?new Point(area.Left+area.Width/2,area.Top+area.Height/2):MousePosition,registry.Count);State.Zoom=zoom;State.Bounds=Bounds;
            if(!workArea.HasValue&&CaptureHost.Running!=null&&CaptureHost.Running.ColorPreferences!=null){var settings=CaptureHost.Running.ColorPreferences;ApplyPinAppearance(settings);SetOpacityPercent(settings.PinOpacity);if(settings.PinZoomPercent>0)SetZoom(settings.PinZoomPercent/100f,Point.Empty);}
            ContextMenuStrip=BuildMenu();registry.Add(PinId,this);if(!workArea.HasValue)Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
            }catch{ReleasePin();throw;}
        }
        internal static float SurfaceZoomLimit(Size image,int margin)
        {
            double a=(double)image.Width*image.Height,b=2.0*margin*(image.Width+image.Height),c=4.0*margin*margin-24000000;
            return (float)((Math.Sqrt(b*b-4*a*c)-b)/(2*a));
        }
        void UpdateMaxZoom(Rectangle area){maxZoom=Math.Min(SurfaceZoomLimit(img.Size,Pad),Math.Max(1,Math.Min(4,Math.Min(area.Width*1.5f/img.Width,area.Height*1.5f/img.Height))));}
        internal static float FitZoom(Size image,Size work,out float minimum)
        {
            float whole=Math.Min(Math.Max(1,work.Width)/(float)Math.Max(1,image.Width),Math.Max(1,work.Height)/(float)Math.Max(1,image.Height));
            float usable=Math.Min(whole,Math.Max(40f/Math.Max(1,image.Width),30f/Math.Max(1,image.Height)));
            float fit=Math.Max(usable,Math.Min(1,whole*.7f));minimum=Math.Max(usable,Math.Min(.1f,fit));return fit;
        }
        internal static Size ScaledSize(Size image,float value){return new Size(Math.Max(1,(int)Math.Round(image.Width*value)),Math.Max(1,(int)Math.Round(image.Height*value)));}
        protected override CreateParams CreateParams{get{var parameters=base.CreateParams;parameters.ExStyle|=0x80088;return parameters;}}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.SetWindowDisplayAffinity(Handle,0x11);SetClickThrough(State.ClickThrough);Redraw();}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnPaint(PaintEventArgs e){}
        protected override void OnMove(EventArgs e){base.OnMove(e);if(State!=null)State.Bounds=Bounds;}
        Rectangle CloseButton{get{return new Rectangle(Pad+body.Width-P(32),Pad+P(8),P(24),P(24));}}
        void Redraw()
        {
            if(!IsHandleCreated||IsDisposed||released)return;int width=body.Width+Pad*2,height=body.Height+Pad*2;
            string key=width+"/"+height+"/"+pinPreferences.PinRadius+"/"+pinPreferences.PinBorderWidth+"/"+pinPreferences.PinBorderColor+"/"+pinPreferences.PinShadow;
            if(painted==null||drawnFor!=key){
                if(painted!=null)painted.Dispose();painted=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
                using(var g=Graphics.FromImage(painted)){
                    g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;float radius=Math.Min(P(pinPreferences.PinRadius),Math.Min(body.Width,body.Height)/2f);var bodyRect=new Rectangle(Pad,Pad,body.Width,body.Height);
                    if(pinPreferences.PinShadow)for(int blur=P(12);blur>0;blur--)using(var path=Theme.Round(new RectangleF(Pad-blur,Pad-blur+P(3),body.Width+blur*2,body.Height+blur*2),radius+blur))using(var fill=new SolidBrush(Color.FromArgb(4,Color.Black)))g.FillPath(fill,path);
                    using(var path=Theme.Round(bodyRect,radius)){var state=g.Save();g.SetClip(path);g.InterpolationMode=zoom==1?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(img,bodyRect,new Rectangle(Point.Empty,img.Size),GraphicsUnit.Pixel);g.Restore(state);
                        if(pinPreferences.PinBorderWidth>0)using(var border=new Pen(ColorTranslator.FromHtml(pinPreferences.PinBorderColor),pinPreferences.PinBorderWidth)){border.Alignment=PenAlignment.Inset;g.DrawPath(border,path);}}
                }drawnFor=key;
            }
            if(surface==null||surface.Width!=width||surface.Height!=height){if(surface!=null)surface.Dispose();surface=new Dib(width,height,true);}
            using(var g=surface.Graphics()){
                g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(painted,0,0);g.CompositingMode=CompositingMode.SourceOver;
                if(over){g.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(Color.FromArgb(225,closeOver?Color.FromArgb(210,56,66):Color.FromArgb(45,49,57))))g.FillEllipse(fill,CloseButton);CaptureLook.Icon(g,"cancel",Rectangle.Inflate(CloseButton,-P(4),-P(4)),Color.White);}
            }
            LayeredFrame.Present(Handle,surface,Location,(byte)Math.Round(State.OpacityPercent*2.55));State.Bounds=Bounds;
        }
        internal Bitmap SurfaceSnapshot(){var handle=Handle;Redraw();return surface.Bitmap.Clone(new Rectangle(0,0,surface.Width,surface.Height),PixelFormat.Format32bppArgb);}
        void SetZoom(float value,Point anchor)
        {
            if(float.IsNaN(value)||float.IsInfinity(value))throw new ArgumentOutOfRangeException("value");value=Math.Max(minZoom,Math.Min(maxZoom,value));if(value==zoom)return;
            var next=ScaledSize(img.Size,value);int x=Left+anchor.X-(int)Math.Round(anchor.X*(double)next.Width/body.Width),y=Top+anchor.Y-(int)Math.Round(anchor.Y*(double)next.Height/body.Height);
            zoom=value;State.Zoom=value;body=next;drawnFor=null;Bounds=new Rectangle(x,y,body.Width+Pad*2,body.Height+Pad*2);State.Bounds=Bounds;Redraw();
        }
        public void ZoomAt(float value,Point anchor){SetZoom(value,anchor);}
        public void SetOpacityPercent(int percent){State.OpacityPercent=Math.Max(20,Math.Min(100,percent));Redraw();}
        public void SetClickThrough(bool enabled){State.ClickThrough=enabled;if(IsHandleCreated){int style=Native.GetWindowLong(Handle,-20);Native.SetWindowLong(Handle,-20,enabled?style|0x20:style&~0x20);}}
        public void ToggleThumbnail(){bool restore=State.Thumbnail;if(!restore)zoomBeforeThumbnail=zoom;State.Thumbnail=!restore;SetZoom(restore?zoomBeforeThumbnail:Math.Min(zoom,160f/Math.Max(img.Width,img.Height)),new Point(body.Width/2,body.Height/2));}
        public void KeepReachable(){Bounds=PinGeometry.KeepReachable(Bounds,Array.ConvertAll(Screen.AllScreens,s=>s.WorkingArea));State.Bounds=Bounds;}
        void DisplayChanged(object sender,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)KeepReachable);}
        public void RestoreInteraction(){State.HiddenByUser=false;SetClickThrough(false);KeepInArea();Show();TopMost=true;State.TopMost=true;Activate();}
        public AnnotationDocument ExportDocument(){var copy=document.Clone();copy.PinQuarterTurns=State.QuarterTurns;copy.PinFlipX=State.FlipX;copy.PinFlipY=State.FlipY;return copy;}
        public void EditImage(){if(!IsDisposed)new CraftEditor(ExportDocument(),ReplaceDocument).Show();}
        public void ReplaceDocument(AnnotationDocument owned)
        {
            if(IsDisposed){owned.Dispose();return;}Bitmap image=null;
            try{using(var raw=owned.Render())image=PinViews.Render(raw,State.QuarterTurns,State.FlipX,State.FlipY);}catch{owned.Dispose();if(image!=null)image.Dispose();throw;}
            var old=document;var oldImage=img;document=owned;img=image;old.Dispose();oldImage.Dispose();var area=testArea??Screen.FromRectangle(Bounds).WorkingArea;FitZoom(img.Size,area.Size,out minZoom);UpdateMaxZoom(area);zoom=Math.Max(minZoom,Math.Min(maxZoom,zoom));State.Zoom=zoom;body=ScaledSize(img.Size,zoom);Size=new Size(body.Width+Pad*2,body.Height+Pad*2);drawnFor=null;Redraw();
        }
        static void Tell(string title,string text,string folder){if(CaptureHost.Running!=null)CaptureHost.Running.Notify(title,text,folder);}
        void Copy(){try{CaptureHost.CopyImage(img);}catch(Exception ex){Tell("复制失败",ex.Message,null);}}
        void SaveAs(){using(var dialog=new SaveFileDialog{Filter="PNG 图片|*.png",FileName="ScreenCraft-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png"})if(dialog.ShowDialog(this)==DialogResult.OK)try{ImageFiles.SavePng(img,dialog.FileName);}catch(Exception ex){Tell("保存失败",ex.Message,null);}}
        void SaveProject(){using(var dialog=new SaveFileDialog{Filter="ScreenCraft 工程|*.sccraft",FileName="ScreenCraft.sccraft"})if(dialog.ShowDialog(this)==DialogResult.OK)try{using(var copy=ExportDocument())copy.Save(dialog.FileName);}catch(Exception ex){Tell("保存工程失败",ex.Message,null);}}
        ContextMenuStrip BuildMenu()
        {
            var menu=TrayMenu.Create();menu.Items.Add(TrayMenu.Item("复制图片  Ctrl+C","copy",Copy));menu.Items.Add(TrayMenu.Item("重新编辑标注","edit",EditImage));menu.Items.Add(TrayMenu.Item("保存 PNG…","save",SaveAs));menu.Items.Add(TrayMenu.Item("保存可编辑工程…","project",SaveProject));
            menu.Items.Add(TrayMenu.Item("原始大小  Z","actualsize",delegate{SetZoom(1,new Point(body.Width/2,body.Height/2));}));menu.Items.Add(TrayMenu.Item("缩略／恢复  S","image",ToggleThumbnail));
            var top=new ToolStripMenuItem("置顶"){Checked=true,CheckOnClick=true};top.Click+=delegate{TopMost=top.Checked;State.TopMost=TopMost;};menu.Items.Add(top);
            var pass=new ToolStripMenuItem("鼠标穿透（从贴图管理恢复）"){CheckOnClick=true};pass.Click+=delegate{SetClickThrough(pass.Checked);};menu.Items.Add(pass);
            var opacity=new CraftMenuItem("透明度","more",delegate{});foreach(int number in new[]{20,40,60,80,100}){int value=number;opacity.DropDownItems.Add(value+"%",null,delegate{SetOpacityPercent(value);});}menu.Items.Add(opacity);
            if(CaptureHost.Running!=null)menu.Items.Add(TrayMenu.Item("识别文字（本地 OCR）","textscan",delegate{CaptureHost.Running.CopyText(img,null);}));
            AddDailyMenu(menu);menu.Items.Add(TrayMenu.Item("关闭此图","cancel",Close));menu.Items.Add(TrayMenu.Item("关闭全部贴图","cancel",delegate{foreach(var pin in All)pin.Close();}));menu.Opening+=delegate{top.Checked=TopMost;pass.Checked=State.ClickThrough;};return menu;
        }
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(e.Clicks>1||over&&CloseButton.Contains(e.Location)){Close();return;}if((ModifierKeys&Keys.Control)!=0){DragImageFile();return;}if(ResizeGrip.Contains(e.Location)){BeginProportionalResize(e.Location);Capture=true;return;}Native.ReleaseCapture();Native.SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(resizing){UpdateProportionalResize(e.Location);return;}bool close=CloseButton.Contains(e.Location);Cursor=ResizeGrip.Contains(e.Location)?Cursors.SizeNWSE:close?Cursors.Hand:Cursors.SizeAll;if(!over||close!=closeOver){over=true;closeOver=close;Redraw();}}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);over=false;closeOver=false;Redraw();}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(resizing&&e.Button==MouseButtons.Left){UpdateProportionalResize(e.Location);EndProportionalResize();Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&resizing)CancelProportionalResize();}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ApplyPinWheel(e.Delta,new Point(e.X-Pad,e.Y-Pad),ModifierKeys);}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(ApplyPinKey(e.KeyData)){e.Handled=true;e.SuppressKeyPress=true;}}
        void ReleasePin(){if(released)return;released=true;registry.Remove(PinId);Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;if(surface!=null)surface.Dispose();if(painted!=null)painted.Dispose();if(img!=null)img.Dispose();if(document!=null)document.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}
        protected override void OnFormClosed(FormClosedEventArgs e){ReleasePin();base.OnFormClosed(e);}
        protected override void Dispose(bool disposing){if(disposing)ReleasePin();base.Dispose(disposing);}
    }
}
