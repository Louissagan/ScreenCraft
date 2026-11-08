using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    internal static class BackgroundStyle
    {
        internal static void Validate(Settings settings,Size image)
        {
            if(settings==null||settings.BgKind<0||settings.BgKind>2||settings.BgPreset<0||settings.BgPreset>=Backdrop.Count||settings.BgPadding<0||settings.BgPadding>100||settings.BgRadius<0||settings.BgRadius>100||settings.BgInset<0||settings.BgInset>100||settings.BgShadowSize<0||settings.BgShadowSize>100)throw new ArgumentException("美化参数超出范围。");
            Size size;Rectangle rect;Backdrop.Measure(image,settings,false,out size,out rect);if(size.Width>32767||size.Height>32767||(long)size.Width*size.Height>64000000)throw new ArgumentException("美化后的图片超过 6400 万像素或尺寸上限，请减少边距或分段导出。");
        }
    }
    public partial class Canvas
    {
        public void ApplyBackground(bool enabled,Settings settings)
        {
            CommitText();if(enabled)BackgroundStyle.Validate(settings,OutputSizeRaw);Commit();BgOn=enabled;Bg=settings.Clone();BackdropChanged();Fire(Changed);Fire(StateChanged);
        }
    }
    internal sealed class BackgroundDialog:Form
    {
        readonly Canvas canvas;readonly AnnotationDocument original;readonly PictureBox preview=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};readonly PreferencesGroup group=new PreferencesGroup();readonly Label status=new Label{ForeColor=PreferencesLook.Muted};
        readonly PreferencesSwitch enabled=new PreferencesSwitch{AccessibleName="启用背景美化"},shadow=new PreferencesSwitch{AccessibleName="美化阴影"};
        readonly PreferencesChoice kind=new PreferencesChoice("渐变背景","纯色背景"){AccessibleName="美化背景类型"},preset=new PreferencesChoice("蓝紫","日落","薄荷","海洋","蜜桃","薰衣草","玫红","极光","夜色","石墨","浅灰","深灰"){AccessibleName="美化渐变"},ratio=new PreferencesChoice("自动","16:9","4:3","1:1"){AccessibleName="美化输出比例"};
        readonly CaptureSlider padding=Slider("美化边距"),radius=Slider("美化圆角"),border=Slider("美化边框"),strength=Slider("美化阴影强度");readonly InspectorColorButton color=new InspectorColorButton{Text="背景颜色…"};readonly PreferencesButton apply=new PreferencesButton{Text="应用",Primary=true},cancel=new PreferencesButton{Text="取消"};readonly Timer debounce=new Timer{Interval=120};
        static CaptureSlider Slider(string name){return new CaptureSlider{Minimum=0,Maximum=100,Width=250,AccessibleName=name};}
        internal BackgroundDialog(Canvas target)
        {
            canvas=target;original=canvas.ExportDocument();var bg=original.Background;enabled.Checked=original.BackgroundOn;shadow.Checked=bg.BgShadow;kind.SelectedIndex=bg.BgKind==1?1:0;preset.SelectedIndex=Math.Max(0,Math.Min(11,bg.BgPreset));ratio.SelectedIndex=Math.Max(0,Array.IndexOf(new[]{"auto","16:9","4:3","1:1"},bg.BgRatio));padding.Value=bg.BgPadding;radius.Value=bg.BgRadius;border.Value=bg.BgInset;strength.Value=bg.BgShadowSize;color.Swatch=Color.FromArgb(255,(bg.BgColor>>16)&255,(bg.BgColor>>8)&255,bg.BgColor&255);
            Text="ScreenCraft · 图片美化";Icon=ShotStack.AppIcon;Font=new Font("Microsoft YaHei UI",9);ClientSize=new Size(920,710);MinimumSize=new Size(780,660);StartPosition=FormStartPosition.CenterParent;BackColor=PreferencesLook.Background;DoubleBuffered=true;
            group.AddRow(new PreferencesRow("启用背景美化",enabled,42));group.AddRow(new PreferencesRow("背景类型",kind,180));group.AddRow(new PreferencesRow("渐变预设",preset,180));group.AddRow(new PreferencesRow("背景颜色",color,180));group.AddRow(new PreferencesRow("输出比例",ratio,180));group.AddRow(new PreferencesRow("边距",padding,180));group.AddRow(new PreferencesRow("圆角",radius,180));group.AddRow(new PreferencesRow("边框",border,180));group.AddRow(new PreferencesRow("阴影",shadow,42));group.AddRow(new PreferencesRow("阴影强度",strength,180));Controls.AddRange(new Control[]{group,preview,status,apply,cancel});
            EventHandler change=delegate{debounce.Stop();debounce.Start();};enabled.CheckedChanged+=change;shadow.CheckedChanged+=change;kind.SelectionChanged+=change;preset.SelectionChanged+=change;ratio.SelectionChanged+=change;padding.ValueChanged+=change;radius.ValueChanged+=change;border.ValueChanged+=change;strength.ValueChanged+=change;debounce.Tick+=delegate{debounce.Stop();RefreshPreview();};
            color.Click+=delegate{CraftColors.Show(color,color.Swatch,delegate(Color chosen){color.Swatch=chosen;color.Invalidate();RefreshPreview();});};
            apply.Click+=delegate{try{canvas.ApplyBackground(enabled.Checked,Value());DialogResult=DialogResult.OK;Close();}catch(Exception ex){status.Text=ex.Message;}};cancel.Click+=delegate{Close();};PerformLayout();RefreshPreview();
        }
        Settings Value(){return new Settings{BgKind=kind.SelectedIndex==1?1:0,BgPreset=preset.SelectedIndex,BgColor=color.Swatch.ToArgb()&0xFFFFFF,BgPadding=padding.Value,BgRadius=radius.Value,BgInset=border.Value,BgShadow=shadow.Checked,BgShadowSize=strength.Value,BgRatio=new[]{"auto","16:9","4:3","1:1"}[ratio.SelectedIndex],BgAlign=4};}
        internal void RefreshPreview(){try{var bg=Value();if(enabled.Checked)BackgroundStyle.Validate(bg,Size.Round(original.Crop.Size));using(var document=new AnnotationDocument(new Bitmap(original.Source),original.Shapes,original.Crop,enabled.Checked,bg)){var image=document.Render();var old=preview.Image;preview.Image=image;if(old!=null)old.Dispose();}status.Text="美化作用于复制、保存和贴图；标注与原图保留，可撤销。";apply.Enabled=true;}catch(Exception ex){status.Text=ex.Message;apply.Enabled=false;}}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(group==null)return;group.Bounds=new Rectangle(24,24,350,group.Height);preview.Bounds=new Rectangle(394,24,Math.Max(1,ClientSize.Width-418),Math.Max(1,ClientSize.Height-112));status.Bounds=new Rectangle(24,ClientSize.Height-74,ClientSize.Width-48,26);cancel.Location=new Point(ClientSize.Width-260,ClientSize.Height-42);apply.Location=new Point(ClientSize.Width-136,ClientSize.Height-42);}
        protected override void Dispose(bool disposing){if(disposing){debounce.Dispose();original.Dispose();if(preview.Image!=null){preview.Image.Dispose();preview.Image=null;}}base.Dispose(disposing);}
    }
}
