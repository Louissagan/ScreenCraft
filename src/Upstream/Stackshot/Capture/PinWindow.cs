// Stackshot - Pin a capture on screen as an always-on-top window.
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using W = System.Windows;
using M = System.Windows.Media;
using MI = System.Windows.Media.Imaging;

namespace Stackshot
{
    // Drag moves it; wheel zooms; Ctrl+wheel changes opacity; double-click or Esc closes; right-click opens a menu.
    // A per-pixel layered window: rounded image, 1 px hairline and a soft shadow around it (Pad px of margin).
    // The image and its shadow are rendered once per size and theme; hovering only swaps the close button in and out.
    public partial class PinWindow : Form
    {
        static readonly PinRegistry<PinWindow> registry = new PinRegistry<PinWindow>();
        public static System.Collections.Generic.List<PinWindow> All { get { return registry.Values; } }
        public readonly PinState State = new PinState();
        public string PinId { get {return State.Id;} }
        AnnotationDocument document;
        Bitmap img;
        MI.BitmapSource src;
        bool resourcesReleased;
        readonly Rectangle? testArea;
        float zoomBeforeThumbnail=1;
        public void RestoreInteraction() { State.HiddenByUser=false;SetClickThrough(false); KeepInArea(); Show(); TopMost=true; State.TopMost=true; Activate(); }
        public void EditImage() { if(!IsDisposed)new CraftEditor(ExportDocument(),ReplaceDocument).Show(); }
        public AnnotationDocument ExportDocument() {var copy=document.Clone();copy.PinQuarterTurns=State.QuarterTurns;copy.PinFlipX=State.FlipX;copy.PinFlipY=State.FlipY;return copy;}
        public void ReplaceDocument(AnnotationDocument ownedDocument) {
            if(IsDisposed){ownedDocument.Dispose();return;}
            Bitmap rendered=null;MI.BitmapSource newSource;
            try{using(var raw=ownedDocument.Render())rendered=PinViews.Render(raw,State.QuarterTurns,State.FlipX,State.FlipY);newSource=Ink.FromGdi(rendered);}catch{if(rendered!=null)rendered.Dispose();ownedDocument.Dispose();throw;}
            var old=document;var oldImage=img;document=ownedDocument;img=rendered;src=newSource;
            old.Dispose();oldImage.Dispose();
            var wa=testArea.HasValue?testArea.Value:Screen.FromRectangle(Bounds).WorkingArea;
            FitZoom(img.Size,wa.Size,out minZoom);maxZoom=Math.Max(1f,Math.Min(4f,Math.Min(wa.Width*1.5f/img.Width,wa.Height*1.5f/img.Height)));
            zoom=Math.Max(minZoom,Math.Min(maxZoom,zoom));State.Zoom=zoom;body=Scaled();drawnFor=null;Redraw();
        }
        readonly float s;
        readonly int Pad;
        float maxZoom;
        float zoom = 1f;
        bool hover, hotClose;
        byte opacity = 255;
        Size body;
        Dib dib;                                  // what the window shows (premultiplied)
        string drawnFor;                          // size and theme the image layer in dib was rendered for
        Dib under;                                // image pixels under the close button while it is shown
        readonly Dib[] closeButton = new Dib[2];  // normal and hot
        readonly int[] closeButtonFor = new int[2];  // the palette each was painted with (HighContrast.Generation)
        int buttonShown = -1;
        MI.BitmapSource shadowTile;
        string shadowKey;
        readonly Action restyle;

        public static PinWindow Open(Bitmap image) {return Open(AnnotationDocument.FromImage(image));}
        public static PinWindow Open(AnnotationDocument ownedDocument) {var pin=CreateHidden(ownedDocument,null);try{pin.Show();return pin;}catch{pin.Dispose();throw;}}
        internal static PinWindow CreateHidden(AnnotationDocument ownedDocument,Rectangle? testWorkArea) {try{return new PinWindow(ownedDocument,testWorkArea);}catch{ownedDocument.Dispose();throw;}}

        PinWindow(AnnotationDocument ownedDocument,Rectangle? testWorkArea)
        {
            testArea=testWorkArea;
            document=ownedDocument;State.QuarterTurns=document.PinQuarterTurns;State.FlipX=document.PinFlipX;State.FlipY=document.PinFlipY;
            using(var raw=document.Render())img=PinViews.Render(raw,State.QuarterTurns,State.FlipX,State.FlipY); src = Ink.FromGdi(img);
            if(!testWorkArea.HasValue&&ShotStack.Running!=null&&ShotStack.Running.ColorPreferences!=null)ApplyPinAppearance(ShotStack.Running.ColorPreferences);
            Screen scr = testWorkArea.HasValue?null:Grabber.CurrentScreen();
            s = testWorkArea.HasValue?1:ShotStack.ScaleFor(scr);
            Pad = P(24);
            Text = "ScreenCraft · 贴图 " + PinId.Substring(0,6);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            if (ShotStack.AppIcon != null) Icon = ShotStack.AppIcon;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            // Fit within 70% of the screen, never upscale. Zooming in stops at 4x, or at 1.5 screens for large
            // captures (never below their real size): beyond that the window would only cost memory.
            Rectangle wa = testWorkArea.HasValue?testWorkArea.Value:scr.WorkingArea;
            zoom = FitZoom(img.Size, wa.Size, out minZoom);
            maxZoom = Math.Max(zoom, Math.Max(1f, Math.Min(4f, Math.Min(wa.Width * 1.5f / img.Width, wa.Height * 1.5f / img.Height))));
            body = Scaled();
            Point m = testWorkArea.HasValue?new Point(wa.Left+wa.Width/2,wa.Top+wa.Height/2):Control.MousePosition;
            Bounds = PinGeometry.Place(new Size(body.Width+2*Pad,body.Height+2*Pad),wa,m,registry.Count);
            State.Bounds=Bounds;State.Zoom=zoom;
            if(!testWorkArea.HasValue&&ShotStack.Running!=null&&ShotStack.Running.ColorPreferences!=null){var p=ShotStack.Running.ColorPreferences;SetOpacityPercent(p.PinOpacity);if(p.PinZoomPercent>0)SetZoom(p.PinZoomPercent/100f,Point.Empty);}
            ContextMenuStrip = BuildMenu();
            restyle = delegate { if (IsHandleCreated && !IsDisposed) BeginInvoke((Action)Redraw); };
            Ds.Changed += restyle;
            registry.Add(PinId,this);
            if(!testWorkArea.HasValue)Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
        }

        int P(float v) { return (int)Math.Round(v * s); }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80 | 0x80000;  // WS_EX_TOOLWINDOW (no taskbar or Alt+Tab entry) | WS_EX_LAYERED
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetWindowDisplayAffinity(Handle,0x11);
            SetClickThrough(State.ClickThrough);
            Redraw();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        Size Scaled() { return ScaledSize(img.Size, zoom); }

        const int MinW = 40, MinH = 30;
        float minZoom;

        // Fits within 70% of the work area without upscaling, and grows to the smallest usable window when that still
        // fits on screen: the zoom is clamped, not each side, so a very tall or wide capture keeps its proportions.
        // Its long side never exceeds the work area, even if the short one ends up thinner than MinW/MinH, so it can
        // always be seen whole. minZoom: the floor for zooming out.
        internal static float FitZoom(Size image, Size workArea, out float minZoom)
        {
            int w = Math.Max(1, image.Width), h = Math.Max(1, image.Height);
            float fit = Math.Min(1f, Math.Min(workArea.Width * 0.7f / w, workArea.Height * 0.7f / h));
            float whole = Math.Min((float)Math.Max(1, workArea.Width) / w, (float)Math.Max(1, workArea.Height) / h);
            float usable = Math.Min(whole, Math.Max((float)MinW / w, (float)MinH / h));
            float zoom = Math.Max(fit, usable);
            minZoom = Math.Max(usable, Math.Min(0.1f, zoom));
            return zoom;
        }

        internal static Size ScaledSize(Size image, float zoom)
        {
            return new Size(Math.Max(1, (int)Math.Round(image.Width * zoom)), Math.Max(1, (int)Math.Round(image.Height * zoom)));
        }

        ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip m = TrayMenu.Create();
            m.Items.Add(TrayMenu.Item("复制图片", "copy", delegate { Copy(); }));
            m.Items.Add(TrayMenu.Item("重新编辑标注", "edit", EditImage));
            if (ShotStack.Running != null) m.Items.Add(TrayMenu.Item("识别文字（本地 OCR）", "textscan", delegate { ShotStack.Running.CopyText(img, null); }));
            m.Items.Add(TrayMenu.Item("保存 PNG…", "save", delegate { SaveAs(); }));
            m.Items.Add(TrayMenu.Item("保存可编辑工程…", "project", SaveProject));
            m.Items.Add(TrayMenu.Item("原始大小", "actualsize", delegate { SetZoom(1f, new Point(body.Width / 2, body.Height / 2)); }));
            m.Items.Add("等比放大  Ctrl++",null,delegate{ApplyPinKey(Keys.Control|Keys.Oemplus);});m.Items.Add("等比缩小  Ctrl+-",null,delegate{ApplyPinKey(Keys.Control|Keys.OemMinus);});
            var top=new ToolStripMenuItem("置顶"){Checked=true,CheckOnClick=true};top.Click+=delegate{TopMost=top.Checked;State.TopMost=TopMost;};m.Items.Add(top);
            var pass=new ToolStripMenuItem("鼠标穿透（从托盘贴图管理恢复）"){CheckOnClick=true};pass.Click+=delegate{SetClickThrough(pass.Checked);};m.Items.Add(pass);
            var opacityMenu=new ToolStripMenuItem("透明度");foreach(int n in new[]{20,40,60,80,100}){int percent=n;opacityMenu.DropDownItems.Add(percent+"%",null,delegate{SetOpacityPercent(percent);});}m.Items.Add(opacityMenu);
            m.Items.Add("缩略／恢复",null,delegate{ToggleThumbnail();});
            AddDailyMenu(m);
            m.Items.Add(TrayMenu.Separator());
            m.Items.Add(TrayMenu.Item("关闭此图", "close", delegate { BeginInvoke((Action)Close); }));
            m.Items.Add("关闭全部贴图",null,delegate{foreach(var pin in All)pin.Close();});
            m.Opening+=delegate{top.Checked=TopMost;pass.Checked=State.ClickThrough;};
            return m;
        }
        void SaveProject(){using(var d=new SaveFileDialog{Filter="可编辑工程（保留原图）|*.sccraft",FileName="贴图.sccraft"})if(d.ShowDialog(this)==DialogResult.OK)try{using(var copy=ExportDocument())copy.Save(d.FileName);}catch(Exception ex){MessageBox.Show(this,"保存失败："+ex.Message);}}
        public void SetClickThrough(bool value){State.ClickThrough=value;if(IsHandleCreated){int flags=Native.GetWindowLong(Handle,-20);Native.SetWindowLong(Handle,-20,value?flags|0x20:flags&~0x20);}}
        public void SetOpacityPercent(int value){State.OpacityPercent=Math.Max(20,Math.Min(100,value));opacity=(byte)Math.Round(State.OpacityPercent*255.0/100);if(IsHandleCreated)Native.FadeLayered(Handle,opacity);}
        public void ToggleThumbnail(){if(State.Thumbnail){State.Thumbnail=false;SetZoom(zoomBeforeThumbnail,new Point(body.Width/2,body.Height/2));}else{zoomBeforeThumbnail=zoom;State.Thumbnail=true;SetZoom(Math.Min(zoom,160f/Math.Max(img.Width,img.Height)),new Point(body.Width/2,body.Height/2));}}
        public void ZoomAt(float value,Point anchor){SetZoom(value,anchor);}
        public void KeepReachable(){var areas=Array.ConvertAll(Screen.AllScreens,delegate(Screen screen){return screen.WorkingArea;});Bounds=PinGeometry.KeepReachable(Bounds,areas);State.Bounds=Bounds;}
        void DisplayChanged(object sender,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)KeepReachable);}
        protected override void OnMove(EventArgs e){base.OnMove(e);if(State!=null)State.Bounds=Bounds;}

        // The pinned picture has no room for a message of its own: the confirmation is a notice.
        static void Tell(string title, string text, string folder)
        {
            if (ShotStack.Running != null) ShotStack.Running.Notify(title, text, folder);
        }

        void Copy()
        {
            try
            {
                ShotStack.CopyImage(img);
            }
            catch (Exception ex)
            {
                ShotStack.Log("Fijada, copiar: " + ex.Message);
                Tell(I18n.T("No se pudo copiar", "Could not copy"), I18n.T("Otro programa est\u00E1 usando el portapapeles. Vuelve a intentarlo.", "Another program is using the clipboard. Please try again."), null);
            }
        }

        void SaveAs()
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = I18n.T("Imagen PNG|*.png", "PNG Image|*.png");
                d.FileName = "Stackshot " + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".png";
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        ImageFiles.SavePng(img,d.FileName);
                        Tell(I18n.T("Imagen guardada", "Image saved"), System.IO.Path.GetFileName(d.FileName), System.IO.Path.GetDirectoryName(d.FileName));
                    }
                    catch (Exception ex)
                    {
                        ShotStack.Log("Guardar como " + d.FileName + ": " + ex);
                        Dialogs.Show(this, I18n.T("No se pudo guardar la imagen.\n\n", "Could not save image.\n\n") + ShotStack.Explain(ex, d.FileName),
                                        "Stackshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // Zoom around the point under the cursor (body coordinates): resized, moved and repainted in one update.
        void SetZoom(float z, Point anchor)
        {
            if(float.IsNaN(z)||float.IsInfinity(z))throw new ArgumentOutOfRangeException("z");
            z = Math.Max(minZoom, Math.Min(maxZoom, z));
            if (z == zoom) return;
            if(!IsHandleCreated){zoom=z;State.Zoom=zoom;body=Scaled();Size=new Size(body.Width+2*Pad,body.Height+2*Pad);State.Bounds=Bounds;return;}
            float fx = anchor.X / (float)Math.Max(1, body.Width), fy = anchor.Y / (float)Math.Max(1, body.Height);
            Native.RECT wr;
            Native.GetWindowRect(Handle, out wr);
            Point screenAnchor = new Point(wr.Left + Pad + anchor.X, wr.Top + Pad + anchor.Y);
            zoom = z;
            State.Zoom=zoom;
            body = Scaled();
            Redraw(screenAnchor.X - (int)Math.Round(fx * body.Width) - Pad, screenAnchor.Y - (int)Math.Round(fy * body.Height) - Pad);
        }

        double Radius { get { return Math.Min(P(pinPreferences.PinRadius), Math.Min(body.Width, body.Height) / 4.0); } }

        Rectangle CloseRect()
        {
            int d = P(24), m = P(8);
            return new Rectangle(body.Width - d - m, m, d, d);
        }

        // The close button's patch, in window coordinates (room for its faint shadow).
        Rectangle ButtonPatch()
        {
            Rectangle c = CloseRect();
            c.Offset(Pad, Pad);
            return Rectangle.Inflate(c, 3, 3);
        }

        void Redraw()
        {
            if (!IsHandleCreated || IsDisposed) return;
            Native.RECT wr;
            Native.GetWindowRect(Handle, out wr);
            Redraw(wr.Left, wr.Top);
        }

        void Redraw(int x, int y)
        {
            if (!IsHandleCreated || IsDisposed) return;
            int w = body.Width + 2 * Pad, h = body.Height + 2 * Pad;
            string key = w + "x" + h + "|" + zoom + "|" + Ds.Dark + "|" + HighContrast.Generation;
            if (key != drawnFor)
            {
                if (dib == null || dib.Width != w || dib.Height != h)
                {
                    if (dib != null) dib.Dispose();
                    dib = new Dib(w, h, true);
                }
                MI.RenderTargetBitmap rtb = Ink.Render(w, h, delegate(M.DrawingContext dc) { PaintImage(dc, w, h); }, null);
                Native.GdiFlush();
                rtb.CopyPixels(new W.Int32Rect(0, 0, w, h), dib.Bits, w * 4 * h, w * 4);
                drawnFor = key;
                buttonShown = -1;
            }
            ShowButton(hover ? (hotClose ? 1 : 0) : -1);
            Present(x, y, w, h);
        }

        // Shadow, the image clipped to the rounded body (sharp pixels when zoomed in) and its hairline.
        void PaintImage(M.DrawingContext dc, int w, int h)
        {
            double r = Radius;
            if(pinPreferences.PinShadow)PaintShadow(dc, w, h, r);
            dc.PushTransform(new M.TranslateTransform(Pad, Pad));
            W.Rect b = new W.Rect(0, 0, body.Width, body.Height);
            M.DrawingGroup pic = new M.DrawingGroup();
            M.RenderOptions.SetBitmapScalingMode(pic, zoom > 1.5f ? M.BitmapScalingMode.NearestNeighbor : M.BitmapScalingMode.HighQuality);
            using (M.DrawingContext pc = pic.Open())
            {
                pc.PushClip(new M.RectangleGeometry(b, r, r));
                pc.DrawImage(src, b);
                pc.Pop();
            }
            dc.DrawDrawing(pic);
            if(pinPreferences.PinBorderWidth>0){Color edge;if(!AnnotationPalette.TryHex(pinPreferences.PinBorderColor,out edge))edge=Color.FromArgb(99,104,114);var stroke=new M.Pen(Ds.Brush(Ds.Argb(1,edge.R,edge.G,edge.B)),P(pinPreferences.PinBorderWidth));dc.DrawRoundedRectangle(null,stroke,b,r,r);}
            if(body.Width>=40&&body.Height>=30){dc.DrawRoundedRectangle(Ds.Brush(Ds.Argb(.45,32,36,42)),null,new W.Rect(body.Width-23,body.Height-23,19,19),4,4);var grip=Ds.Brush(Ds.Argb(.9,255,255,255));var stroke=new M.Pen(grip,1.4);for(int i=0;i<3;i++)dc.DrawLine(stroke,new W.Point(body.Width-8-i*4,body.Height-8),new W.Point(body.Width-8,body.Height-8-i*4));}
            dc.Pop();
        }

        // -1 = none, 0 = close button, 1 = close button hot. The image underneath is kept to restore it.
        void ShowButton(int state)
        {
            if (state == buttonShown) return;
            Rectangle r = ButtonPatch();
            if (buttonShown >= 0 && under != null)
                Native.BitBlt(dib.Dc, r.X, r.Y, r.Width, r.Height, under.Dc, 0, 0, Native.SRCCOPY);
            if (state >= 0)
            {
                if (under == null || under.Width != r.Width || under.Height != r.Height)
                {
                    if (under != null) under.Dispose();
                    under = new Dib(r.Width, r.Height, true);
                }
                Native.BitBlt(under.Dc, 0, 0, r.Width, r.Height, dib.Dc, r.X, r.Y, Native.SRCCOPY);
                Dib b = Button(state == 1, r.Size);
                Native.AlphaBlend(dib.Dc, r.X, r.Y, r.Width, r.Height, b.Dc, 0, 0, r.Width, r.Height, (255 << 16) | (1 << 24)); // AC_SRC_ALPHA
            }
            Native.GdiFlush();
            buttonShown = state;
        }

        Dib Button(bool hot, Size size)
        {
            int i = hot ? 1 : 0;
            Dib d = closeButton[i];
            if (d != null && d.Width == size.Width && d.Height == size.Height && closeButtonFor[i] == HighContrast.Generation) return d;
            if (d != null) d.Dispose();
            d = new Dib(size.Width, size.Height, true);
            double cw = CloseRect().Width;
            MI.RenderTargetBitmap rtb = Ink.Render(size.Width, size.Height, delegate(M.DrawingContext dc)
            {
                W.Point center = new W.Point(size.Width / 2.0, size.Height / 2.0);
                bool high = Ds.Brushes.High;
                if (!high) dc.DrawEllipse(Ds.Brush(Ds.Argb(0.14, 0, 0, 0)), null, new W.Point(center.X, center.Y + 0.5), cw / 2.0 + 1, cw / 2.0 + 1);
                dc.DrawEllipse(Ds.Brush(hot ? (high ? Ds.Brushes.Accent : Ds.Argb(0.92, 58, 58, 62)) : Palette.Hud), null, center, cw / 2.0, cw / 2.0);
                M.Pen ring = new M.Pen(Ds.Brush(Palette.HudLine), 1);
                ring.Freeze();
                dc.DrawEllipse(null, ring, center, cw / 2.0 - 0.5, cw / 2.0 - 0.5);
                double gs = P(14);
                Glyph.Draw(dc, "close", center.X - gs / 2, center.Y - gs / 2, gs, high ? (hot ? Ds.Brushes.OnAccent : Palette.HudLabel) : hot ? Palette.HudLabel : Ds.Argb(0.85, 255, 255, 255), Math.Max(1.4, 1.7 * s));
            }, null);
            closeButtonFor[i] = HighContrast.Generation;
            Native.GdiFlush();
            rtb.CopyPixels(new W.Int32Rect(0, 0, size.Width, size.Height), d.Bits, size.Width * 4 * size.Height, size.Width * 4);
            closeButton[i] = d;
            return d;
        }

        [StructLayout(LayoutKind.Sequential)] struct Blend { public byte Op, Flags, Alpha, Format; }

        [DllImport("user32.dll", EntryPoint = "UpdateLayeredWindow")]
        static extern bool UpdateLayered(IntPtr hwnd, IntPtr hdcDst, ref Native.POINT dst, ref Native.SIZE size, IntPtr hdcSrc, ref Native.POINT src, int key, ref Blend blend, int flags);

        // Position, size, pixels and opacity in a single update, so zooming never shows a stretched or shifted frame.
        void Present(int x, int y, int w, int h)
        {
            Native.POINT dst; dst.X = x; dst.Y = y;
            Native.POINT from; from.X = 0; from.Y = 0;
            Native.SIZE size; size.cx = w; size.cy = h;
            Blend bf = new Blend();
            bf.Alpha = opacity;
            bf.Format = 1; // AC_SRC_ALPHA
            UpdateLayered(Handle, IntPtr.Zero, ref dst, ref size, dib.Dc, ref from, 0, ref bf, 2); // ULW_ALPHA
            if (Left != x || Top != y || Width != w || Height != h) UpdateBounds(); // WinForms is not told about it
            State.Bounds=Bounds;
        }

        // Shadow drawn as a nine-slice of one small pre-blurred tile, so zooming never re-blurs a large surface.
        void PaintShadow(M.DrawingContext dc, int w, int h, double r)
        {
            if (Ds.Brushes.High) return;   // no shadows in a contrast theme
            int blur = P(16), off = P(5);
            int c = (int)Math.Ceiling(r) + blur + off + 2;
            int t = 2 * Pad + 2 * c;
            M.Color col = Ds.Dark ? Ds.Argb(0.60, 0, 0, 0) : Ds.Argb(0.36, 0, 0, 0);
            if (body.Width < 2 * c || body.Height < 2 * c)
            {
                dc.DrawImage(Ink.Shadow(w, h, new W.Rect(Pad, Pad + off, body.Width, body.Height), r, blur, col), new W.Rect(0, 0, w, h));
                return;
            }
            if (shadowTile == null || shadowTile.PixelWidth != t || shadowKey != col.ToString() + r)
            {
                shadowTile = Ink.Shadow(t, t, new W.Rect(Pad, Pad + off, 2 * c, 2 * c), r, blur, col);
                shadowKey = col.ToString() + r;
            }
            int half = t / 2;
            int[] sx = { 0, half - 1, half + 1 }, sw = { half - 1, 2, t - half - 1 };
            int[] dx = { 0, half - 1, w - (t - half - 1) }, dw = { half - 1, w - t + 2, t - half - 1 };
            int[] dy = { 0, half - 1, h - (t - half - 1) }, dh = { half - 1, h - t + 2, t - half - 1 };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (i == 1 && j == 1) continue; // hidden under the image
                    MI.CroppedBitmap part = new MI.CroppedBitmap(shadowTile, new W.Int32Rect(sx[i], sx[j], sw[i], sw[j]));
                    dc.DrawImage(part, new W.Rect(dx[i], dy[j], dw[i], dh[j]));
                }
        }

        Point Local(Point p) { return new Point(p.X - Pad, p.Y - Pad); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if(resizing){UpdateProportionalResize(e.Location);return;}if(ResizeGrip.Contains(e.Location)){Cursor=Cursors.SizeNWSE;return;}
            bool h = CloseRect().Contains(Local(e.Location));
            Cursor=h?Cursors.Hand:Cursors.SizeAll;
            if (!hover || h != hotClose) { hover = true; hotClose = h; Cursor = h ? Cursors.Hand : Cursors.SizeAll; Redraw(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!hover) return;
            hover = false;
            hotClose = false;
            Redraw();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            // Double-click closes. Handled here: the native move below takes the button-up, so no double-click event
            // would ever arrive.
            if (e.Clicks >= 2 || CloseRect().Contains(Local(e.Location))) { Close(); return; }
            if((ModifierKeys&Keys.Control)!=0){DragImageFile();return;}
            if(ResizeGrip.Contains(e.Location)){BeginProportionalResize(e.Location);Capture=true;return;}
            // Native move: smooth and with edge snapping.
            Native.ReleaseCapture();
            Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); // WM_NCLBUTTONDOWN on HTCAPTION
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ApplyPinWheel(e.Delta,Local(e.Location),ModifierKeys);
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left&&resizing){UpdateProportionalResize(e.Location);EndProportionalResize();Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&resizing)EndProportionalResize();}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(ApplyPinKey(e.KeyData)){e.Handled=true;e.SuppressKeyPress=true;}
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            ReleasePinResources();
        }
        void ReleasePinResources(){
            if(resourcesReleased)return;resourcesReleased=true;
            registry.Remove(PinId);
            Ds.Changed -= restyle;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;
            if (dib != null) { dib.Dispose(); dib = null; }
            if (under != null) { under.Dispose(); under = null; }
            for (int i = 0; i < closeButton.Length; i++) if (closeButton[i] != null) { closeButton[i].Dispose(); closeButton[i] = null; }
            if(img!=null)img.Dispose();
            if(document!=null)document.Dispose();
            if (ContextMenuStrip != null) ContextMenuStrip.Dispose();
        }
        protected override void Dispose(bool disposing){if(disposing)ReleasePinResources();base.Dispose(disposing);}
    }
}
