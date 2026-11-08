using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class TextMemoStyle
    {
        internal Color TextColor=Color.White,BackgroundColor=Color.FromArgb(124,58,237);internal string FontFamily="Microsoft YaHei UI";internal int FontPx=18,Padding=18,MaxWidth=600;
    }
    internal static class TextMemoRenderer
    {
        internal static Bitmap Render(string text,TextMemoStyle style)
        {
            if(string.IsNullOrWhiteSpace(text))throw new ArgumentException("请输入或读取需要贴到桌面的文字。");if(style.MaxWidth<=style.Padding*2+20)throw new ArgumentException("最大宽度需要大于两侧边距加 20 像素。");if(text.Length>20000||style.FontPx<8||style.FontPx>96||style.Padding<4||style.Padding>64||style.MaxWidth<120||style.MaxWidth>1200)throw new ArgumentException("文字长度或样式参数超出范围。");
            using(var measure=new Bitmap(1,1))using(var g=Graphics.FromImage(measure))using(var font=new Font(style.FontFamily,style.FontPx,GraphicsUnit.Pixel)){
                float natural=0;foreach(var line in text.Split('\n'))natural=Math.Max(natural,g.MeasureString(line,font).Width);int width=Math.Max(120,Math.Min(style.MaxWidth,(int)Math.Ceiling(natural)+style.Padding*2));var size=g.MeasureString(text,font,new SizeF(width-style.Padding*2,20000));int height=(int)Math.Ceiling(size.Height)+style.Padding*2;if(height>6000||(long)width*height>6000000)throw new ArgumentException("备忘内容过长，请分段创建贴图。");var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var paint=Graphics.FromImage(image)){paint.Clear(Color.Transparent);paint.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=Theme.Round(new Rectangle(0,0,width-1,height-1),12))using(var background=new SolidBrush(style.BackgroundColor))paint.FillPath(background,path);paint.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var ink=new SolidBrush(style.TextColor))paint.DrawString(text,font,ink,new RectangleF(style.Padding,style.Padding,width-style.Padding*2,height-style.Padding*2));}return image;
            }
        }
    }
    internal sealed class TextMemoDialog:Form
    {
        readonly TextBox text=new TextBox{Multiline=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,MaxLength=20000,AcceptsReturn=true,AccessibleName="备忘文字"};
        readonly NumericUpDown fontSize=new NumericUpDown{Minimum=8,Maximum=96,Value=18,BorderStyle=BorderStyle.None,AccessibleName="备忘字号"},padding=new NumericUpDown{Minimum=4,Maximum=64,Value=18,BorderStyle=BorderStyle.None,AccessibleName="备忘边距"},maxWidth=new NumericUpDown{Minimum=120,Maximum=1200,Value=600,Increment=20,BorderStyle=BorderStyle.None,AccessibleName="备忘最大宽度"};
        readonly PreferencesChoice family=new PreferencesChoice("Microsoft YaHei UI","Segoe UI","Arial"){AccessibleName="备忘字体"};readonly InspectorColorButton ink=new InspectorColorButton{Text="文字颜色…",Swatch=Color.White},background=new InspectorColorButton{Text="背景颜色…",Swatch=Color.FromArgb(124,58,237)};
        readonly PictureBox preview=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,BackColor=PreferencesLook.Background};readonly PreferencesGroup group=new PreferencesGroup();readonly PreferencesField input;readonly Label heading=new Label{Text="文字备忘",AutoSize=true},hint=new Label{Text="读取剪贴板，或在这里直接输入。"},status=new Label();readonly PreferencesButton read=new PreferencesButton{Text="读取剪贴板"},pin=new PreferencesButton{Text="钉到桌面",Primary=true};readonly Timer debounce=new Timer{Interval=120};
        internal Func<string> ReadClipboardText=Clipboard.GetText;internal Action<Bitmap> PinOutput=delegate(Bitmap image){PinWindow.Open(image);};
        internal TextMemoDialog(string initialText)
        {
            Text="ScreenCraft · 文字备忘贴图";Icon=ShotStack.AppIcon;Font=new Font("Microsoft YaHei UI",9);BackColor=PreferencesLook.Background;ClientSize=new Size(720,660);MinimumSize=new Size(680,640);StartPosition=FormStartPosition.CenterParent;DoubleBuffered=true;text.Text=initialText??"";
            heading.Font=new Font(Font.FontFamily,16,FontStyle.Bold);heading.ForeColor=PreferencesLook.Ink;hint.ForeColor=PreferencesLook.Muted;status.ForeColor=Color.FromArgb(181,47,56);input=new PreferencesField(text,660){Height=148};
            group.AddRow(new PreferencesRow("字体",family,160));group.AddRow(new PreferencesRow("字号（px）",new PreferencesField(fontSize,100),100));group.AddRow(new PreferencesRow("边距（px）",new PreferencesField(padding,100),100));group.AddRow(new PreferencesRow("最大宽度（px）",new PreferencesField(maxWidth,100),100));group.AddRow(new PreferencesRow("文字颜色",ink,140));group.AddRow(new PreferencesRow("背景颜色",background,140));Controls.AddRange(new Control[]{heading,hint,input,group,preview,status,read,pin});
            EventHandler changed=delegate{debounce.Stop();debounce.Start();};text.TextChanged+=changed;fontSize.ValueChanged+=changed;padding.ValueChanged+=changed;maxWidth.ValueChanged+=changed;family.SelectionChanged+=changed;debounce.Tick+=delegate{debounce.Stop();RefreshPreview();};
            ink.Click+=delegate{CraftColors.Show(ink,ink.Swatch,delegate(Color chosen){ink.Swatch=chosen;ink.Invalidate();RefreshPreview();});};background.Click+=delegate{CraftColors.Show(background,background.Swatch,delegate(Color chosen){background.Swatch=chosen;background.Invalidate();RefreshPreview();});};
            read.Click+=delegate{try{text.Text=ReadClipboardText()??"";RefreshPreview();}catch(Exception ex){status.Text="无法读取剪贴板："+ex.Message;}};pin.Click+=delegate{Bitmap image=null;try{image=TextMemoRenderer.Render(text.Text,Style());PinOutput(image);image=null;Close();}catch(Exception ex){status.Text=ex.Message;}finally{if(image!=null)image.Dispose();}};PerformLayout();RefreshPreview();
        }
        TextMemoStyle Style(){return new TextMemoStyle{FontFamily=family.SelectedItem,FontPx=(int)fontSize.Value,Padding=(int)padding.Value,MaxWidth=(int)maxWidth.Value,TextColor=ink.Swatch,BackgroundColor=background.Swatch};}
        internal void RefreshPreview(){Bitmap image=null;try{if(!string.IsNullOrWhiteSpace(text.Text))image=TextMemoRenderer.Render(text.Text,Style());status.Text="";}catch(Exception ex){status.Text=ex.Message;}var old=preview.Image;preview.Image=image;if(old!=null)old.Dispose();}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(input==null)return;int width=ClientSize.Width,height=ClientSize.Height,column=(width-60)/2;heading.Location=new Point(24,20);hint.Bounds=new Rectangle(24,58,width-48,24);input.Bounds=new Rectangle(24,90,width-48,142);group.Bounds=new Rectangle(24,246,column,group.Height);preview.Bounds=new Rectangle(36+column,246,width-column-60,Math.Max(100,height-312));read.Location=new Point(24,height-46);pin.Location=new Point(width-pin.Width-24,height-46);status.Bounds=new Rectangle(read.Right+14,height-50,Math.Max(1,pin.Left-read.Right-28),42);}
        internal Bitmap RenderOffscreen(){var image=new Bitmap(ClientSize.Width,ClientSize.Height);using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,this,this);return image;}
        protected override void Dispose(bool disposing){if(disposing){debounce.Dispose();if(preview.Image!=null){preview.Image.Dispose();preview.Image=null;}}base.Dispose(disposing);}
    }
}
