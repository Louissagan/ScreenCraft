using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class StartupToast:Form
    {
        readonly Timer timer=new Timer{Interval=16};
        readonly System.Diagnostics.Stopwatch elapsed=new System.Diagnostics.Stopwatch();
        readonly Rectangle destination;
        readonly string title,detail;
        readonly float scale;
        readonly Font heading;
        int Px(float logical){return (int)Math.Round(logical*scale);}
        internal static Rectangle Placement(Rectangle workArea,Size size){return Placement(workArea,size,20);}
        static Rectangle Placement(Rectangle workArea,Size size,int margin)
        {
            int width=Math.Min(size.Width,workArea.Width),height=Math.Min(size.Height,workArea.Height);
            return new Rectangle(Math.Max(workArea.Left,workArea.Right-width-margin),Math.Max(workArea.Top,workArea.Bottom-height-margin),width,height);
        }
        internal StartupToast(Rectangle workArea,string captureKey,bool alreadyRunning=false)
            :this(workArea,captureKey,alreadyRunning,ShotStack.ScaleFor(Screen.FromRectangle(workArea))){}
        internal StartupToast(Rectangle workArea,string captureKey,bool alreadyRunning,float displayScale)
        {
            scale=Math.Max(1,Math.Min(4,displayScale));
            title=alreadyRunning?"ScreenCraft 已在运行":"ScreenCraft 已启动";
            detail=string.IsNullOrEmpty(captureKey)?"左键托盘图标截图，右键打开设置。":"按 "+captureKey+" 截图 · 右键托盘打开设置";
            Text=title;Icon=ShotStack.AppIcon;AccessibleName=title;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;TopMost=true;AutoScaleMode=AutoScaleMode.None;
            // Use physical pixels consistently for fonts, icon and layout at fractional DPI.
            Font=new Font("Microsoft YaHei UI",13*scale,FontStyle.Regular,GraphicsUnit.Pixel);
            heading=new Font(Font.FontFamily,14*scale,FontStyle.Bold,GraphicsUnit.Pixel);
            BackColor=Color.White;DoubleBuffered=true;
            destination=Placement(workArea,new Size(Px(380),Px(92)),Px(20));Bounds=destination;
            var close=new CaptureIconButton{Glyph="cancel",Size=new Size(Px(24),Px(24)),Location=new Point(Width-Px(31),Px(7)),BackColor=Color.White,AccessibleName="关闭启动提示"};
            close.Click+=delegate{Close();};Controls.Add(close);
            timer.Tick+=delegate{if(!Advance(elapsed.Elapsed))Close();};
        }
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
        protected override void OnShown(EventArgs e){base.OnShown(e);elapsed.Start();Advance(TimeSpan.Zero);timer.Start();}
        internal bool Advance(TimeSpan time)
        {
            double t=Math.Max(0,Math.Min(1,time.TotalMilliseconds/180)),ease=1-Math.Pow(1-t,3);
            Location=new Point(destination.X,destination.Y+(int)Math.Round(Px(12)*(1-ease)));
            // Keep text opaque throughout the slide, avoiding layered-window alpha resampling.
            return time.TotalMilliseconds<2670;
        }
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(!PopupChrome.Apply(Handle))using(var path=Theme.Round(ClientRectangle,Px(12)))Region=new Region(path);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(Color.White);
            using(var icon=BrandArtwork.Render(Px(32)))g.DrawImageUnscaled(icon,Px(16),Px(20));
            g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
            using(var ink=new SolidBrush(PreferencesLook.Ink))
            using(var format=new StringFormat{LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter})
                g.DrawString(title,heading,ink,new Rectangle(Px(62),Px(13),Math.Max(1,Width-Px(105)),Px(24)),format);
            using(var ink=new SolidBrush(PreferencesLook.Muted))
            using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter})
                g.DrawString(detail,Font,ink,new Rectangle(Px(62),Px(42),Math.Max(1,Width-Px(78)),Px(40)),format);
            base.OnPaint(e);
        }
        internal Bitmap RenderOffscreen(){var image=new Bitmap(Width,Height);using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,this,this);return image;}
        protected override void Dispose(bool disposing){if(disposing){timer.Dispose();heading.Dispose();Font.Dispose();}base.Dispose(disposing);}
    }
}
