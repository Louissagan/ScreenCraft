using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class CraftMenuItem:ToolStripMenuItem
    {
        internal readonly string Glyph;readonly Bitmap artwork;
        internal CraftMenuItem(string text,string glyph,Action action):base(text){Glyph=glyph;artwork=new Bitmap(20,20);Image=artwork;Click+=delegate{action();};}
        protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)artwork.Dispose();}
    }
    internal sealed class CraftMenuHeader:ToolStripMenuItem
    {
        internal CraftMenuHeader(){Text="ScreenCraft";Enabled=false;AccessibleName="ScreenCraft "+typeof(ScreenCraftProgram).Assembly.GetName().Version.ToString(3);}
    }
    internal sealed class CraftMenuRenderer:ToolStripRenderer
    {
        internal static readonly Color Surface=Color.White;
        internal void PaintMenu(Graphics g,StyledMenu menu)
        {
            PreferencesLook.Fill(g,menu.ClientRectangle,Surface);float s=menu.DeviceScale;
            foreach(ToolStripItem item in menu.Items){if(!item.Available)continue;Rectangle r=item.Bounds;
                if(item is ToolStripControlHost)continue;
                if(item is ToolStripSeparator){using(var pen=new Pen(PreferencesLook.Line))g.DrawLine(pen,r.Left+12*s,r.Top+r.Height/2,r.Right-12*s,r.Top+r.Height/2);continue;}
                if(item is CraftMenuHeader){using(var artwork=BrandArtwork.Render((int)(22*s)))g.DrawImage(artwork,new Rectangle(r.Left+(int)(10*s),r.Top+(r.Height-artwork.Height)/2,artwork.Width,artwork.Height));using(var font=new Font(menu.Font.FontFamily,9.75f,FontStyle.Bold))PreferencesLook.Text(g,"ScreenCraft",font,new Rectangle(r.Left+(int)(42*s),r.Top,r.Width-(int)(124*s),r.Height),PreferencesLook.Ink,StringAlignment.Near);PreferencesLook.Text(g,typeof(ScreenCraftProgram).Assembly.GetName().Version.ToString(3),menu.Font,new Rectangle(r.Right-(int)(72*s),r.Top,(int)(58*s),r.Height),PreferencesLook.Muted,StringAlignment.Far);continue;}
                var command=item as ToolStripMenuItem;if(command==null)continue;bool danger=Destructive(item);Color ink=!item.Enabled?Color.FromArgb(155,158,166):danger?Color.FromArgb(192,41,63):PreferencesLook.Ink;
                if(item.Selected&&item.Enabled)PreferencesLook.Round(g,Rectangle.Inflate(r,-2,-1),danger?Color.FromArgb(255,235,238):CaptureLook.Selected,(int)(7*s),null);
                var icon=new Rectangle(r.Left+(int)(10*s),r.Top+(r.Height-(int)(20*s))/2,(int)(20*s),(int)(20*s));var custom=item as CraftMenuItem;
                if(command.Checked)CaptureLook.Icon(g,"done",icon,CaptureLook.Blue);else if(custom!=null)CaptureLook.Icon(g,custom.Glyph,icon,item.Selected?CaptureLook.Blue:ink);
                string shortcut=command.ShortcutKeyDisplayString??"";int right=(int)(14*s),reserve=shortcut.Length==0?0:(int)Math.Ceiling(g.MeasureString(shortcut,menu.Font).Width)+(int)(22*s);
                int left=r.Left+(int)(42*s);PreferencesLook.Text(g,command.Text,menu.Font,new Rectangle(left,r.Top,Math.Max(1,r.Right-right-left-reserve),r.Height),ink,StringAlignment.Near);
                if(shortcut.Length>0)PreferencesLook.Text(g,shortcut,menu.Font,new Rectangle(r.Right-right-reserve,r.Top,reserve,r.Height),PreferencesLook.Muted,StringAlignment.Far);
                if(command.HasDropDownItems){using(var pen=new Pen(PreferencesLook.Muted,1.5f)){int x=r.Right-(int)(14*s),y=r.Top+r.Height/2;g.DrawLines(pen,new[]{new Point(x-2,y-4),new Point(x+2,y),new Point(x-2,y+4)});}}
            }
            PreferencesLook.Round(g,menu.ClientRectangle,Color.Transparent,10,PreferencesLook.Line);
        }
        static bool Destructive(ToolStripItem item){return item.Name=="exit"||item.Text.StartsWith("关闭");}
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){PreferencesLook.Fill(e.Graphics,e.AffectedBounds,Color.White);}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){PreferencesLook.Round(e.Graphics,new Rectangle(Point.Empty,e.ToolStrip.Size),Color.Transparent,10,PreferencesLook.Line);}
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){var bounds=new Rectangle(Point.Empty,e.Item.Size);PreferencesLook.Fill(e.Graphics,bounds,Color.White);if(e.Item.Enabled&&e.Item.Selected)PreferencesLook.Round(e.Graphics,Rectangle.Inflate(bounds,-2,-1),Destructive(e.Item)?Color.FromArgb(255,235,238):CaptureLook.Selected,7,null);}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if(e.Item is CraftMenuHeader){using(var font=new Font(e.TextFont.FontFamily,10,FontStyle.Bold)){PreferencesLook.Text(e.Graphics,"ScreenCraft",font,new Rectangle(10,0,150,e.Item.Height),PreferencesLook.Ink,StringAlignment.Near);PreferencesLook.Text(e.Graphics,typeof(ScreenCraftProgram).Assembly.GetName().Version.ToString(3),e.TextFont,new Rectangle(e.Item.Width-80,0,65,e.Item.Height),PreferencesLook.Muted,StringAlignment.Far);}return;}
            bool right=(e.TextFormat&TextFormatFlags.Right)!=0;Color ink=!e.Item.Enabled?Color.FromArgb(150,150,158):Destructive(e.Item)?Color.FromArgb(192,41,63):right?PreferencesLook.Muted:PreferencesLook.Ink;
            PreferencesLook.Text(e.Graphics,e.Text,e.TextFont,e.TextRectangle,ink,right?StringAlignment.Far:StringAlignment.Near);
        }
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e){var item=e.Item as CraftMenuItem;if(item==null){base.OnRenderItemImage(e);return;}Color ink=!item.Enabled?Color.Gray:Destructive(item)?Color.FromArgb(192,41,63):item.Selected?CaptureLook.Blue:PreferencesLook.Muted;CaptureLook.Icon(e.Graphics,item.Glyph,e.ImageRectangle,ink);}
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(PreferencesLook.Line))e.Graphics.DrawLine(pen,12,e.Item.Height/2,e.Item.Width-12,e.Item.Height/2);}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){CaptureLook.Icon(e.Graphics,"done",e.ImageRectangle,CaptureLook.Blue);}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){var r=e.ArrowRectangle;using(var pen=new Pen(PreferencesLook.Muted,1.5f))e.Graphics.DrawLines(pen,new[]{new Point(r.Left+r.Width/2-2,r.Top+r.Height/2-4),new Point(r.Left+r.Width/2+2,r.Top+r.Height/2),new Point(r.Left+r.Width/2-2,r.Top+r.Height/2+4)});}
    }
    internal sealed class StyledMenu:ContextMenuStrip
    {
        bool systemCorners;internal int MinimumContentWidth=284;internal float DeviceScale{get{return DeviceDpi/96f;}}
        internal StyledMenu(){Renderer=new CraftMenuRenderer();BackColor=CraftMenuRenderer.Surface;ForeColor=PreferencesLook.Ink;Font=new Font("Microsoft YaHei UI",9.25f);Padding=new Padding(6);ImageScalingSize=new Size(20,20);ShowImageMargin=true;DropShadowEnabled=true;AutoSize=false;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~0x00800000;p.ExStyle&=~(0x100|0x200|0x1);return p;}}
        internal void PrepareLayout()
        {
            float scale=DeviceDpi/96f;Padding=new Padding((int)(6*scale));ImageScalingSize=new Size((int)(20*scale),(int)(20*scale));int width=(int)(MinimumContentWidth*scale),height=Padding.Vertical;
            foreach(ToolStripItem item in Items){var host=item as ToolStripControlHost;if(host!=null)width=Math.Max(width,host.Control.Width);var command=item as ToolStripMenuItem;if(command!=null&&!(command is CraftMenuHeader)){int label=TextRenderer.MeasureText(command.Text,Font).Width,shortcut=string.IsNullOrEmpty(command.ShortcutKeyDisplayString)?0:TextRenderer.MeasureText(command.ShortcutKeyDisplayString,Font).Width;width=Math.Max(width,label+shortcut+(int)(90*scale));}}
            SuspendLayout();try{foreach(ToolStripItem item in Items){int row=(int)((item is CraftMenuHeader?42:item is ToolStripSeparator?10:34)*scale);if(item is ToolStripControlHost)row=item.Height;else{item.AutoSize=false;item.Size=new Size(width,row);item.Margin=Padding.Empty;}var sub=item as ToolStripDropDownItem;if(sub!=null&&sub.HasDropDownItems){var styled=sub.DropDown as StyledMenu;if(styled==null){var old=sub.DropDown;styled=new StyledMenu{MinimumContentWidth=180};while(old.Items.Count>0){var nested=old.Items[0];old.Items.Remove(nested);styled.Items.Add(nested);}sub.DropDown=styled;old.Dispose();}styled.PrepareLayout();}height+=row;}Size=new Size(width+Padding.Horizontal,height);}finally{ResumeLayout(true);}
        }
        protected override void OnOpening(CancelEventArgs e){base.OnOpening(e);if(!e.Cancel)PrepareLayout();}
        protected override void OnPaintBackground(PaintEventArgs e){PreferencesLook.Fill(e.Graphics,ClientRectangle,BackColor);}
        protected override void OnPaint(PaintEventArgs e){((CraftMenuRenderer)Renderer).PaintMenu(e.Graphics,this);}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);var previous=Region;Region=null;if(previous!=null)previous.Dispose();systemCorners=PopupChrome.Apply(Handle);UpdateCorners();}
        void UpdateCorners(){if(Width<=12||Height<=12)return;var old=Region;if(systemCorners)Region=null;else using(var path=Theme.Round(ClientRectangle,(int)(10*DeviceScale)))Region=new Region(path);if(old!=null)old.Dispose();}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);UpdateCorners();}
        internal Bitmap RenderOffscreen(){PrepareLayout();var image=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);var handle=Handle;DrawToBitmap(image,ClientRectangle);return image;}
    }
    internal static class TrayActions
    {
        internal static StyledMenu Create(Func<Preferences> preferences,Action<string> invoke)
        {
            var menu=new StyledMenu();menu.Items.Add(new CraftMenuHeader());
            Add(menu,"capture","截图并标注","crop",invoke);var capture=new CraftMenuItem("更多截图方式","crop",delegate{});foreach(var pair in new[]{new[]{"fullscreen","当前屏全屏截图"},new[]{"window","当前窗口截图"},new[]{"repeat","重复上次区域"}}){string id=pair[0];capture.DropDownItems.Add(new CraftMenuItem(pair[1],"crop",delegate{invoke(id);}){Name=id});}menu.Items.Add(capture);Add(menu,"ocr","本地 OCR","ocr",invoke);Add(menu,"color","屏幕取色","color",invoke);Add(menu,"scroll","滚动长截图","scroll",invoke);
            menu.Items.Add(new ToolStripSeparator());Add(menu,"open","打开图片并标注…","open",invoke);Add(menu,"files","从文件创建贴图…","image",invoke);Add(menu,"paste","贴剪贴板图片","paste",invoke);Add(menu,"pins","贴图管理","stack",invoke);
            Add(menu,"memo","文字备忘贴图…","text",invoke);Add(menu,"history","截图历史…","history",invoke);Add(menu,"board","拼图画板…","board",invoke);Add(menu,"folder","打开保存目录","open",invoke);Add(menu,"togglepins","隐藏／恢复全部贴图","stack",invoke);
            menu.Items.Add(new ToolStripSeparator());Add(menu,"settings","设置","gear",invoke);Add(menu,"exit","退出 ScreenCraft","exit",invoke);
            menu.Opening+=delegate{Refresh(menu,preferences());};Refresh(menu,preferences());return menu;
        }
        static void Add(StyledMenu menu,string id,string label,string glyph,Action<string> invoke){menu.Items.Add(new CraftMenuItem(label,glyph,delegate{invoke(id);}){Name=id,AccessibleName=label});}
        internal static void Refresh(StyledMenu menu,Preferences preferences){RefreshItems(menu.Items,preferences);menu.PrepareLayout();}
        static void RefreshItems(ToolStripItemCollection items,Preferences preferences){foreach(ToolStripItem item in items){var command=item as ToolStripMenuItem;if(command==null||command is CraftMenuHeader)continue;string key;command.ShortcutKeyDisplayString=preferences.Hotkeys().TryGetValue(command.Name,out key)?key:"";if(command.HasDropDownItems)RefreshItems(command.DropDownItems,preferences);}}
    }
}
