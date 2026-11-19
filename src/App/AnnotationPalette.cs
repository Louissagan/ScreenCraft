using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class PaletteSwatch:PreferencesPaintButton
    {
        internal Color Ink;internal bool Selected;
        internal PaletteSwatch(){Size=new Size(30,30);BackColor=Color.White;AccessibleRole=AccessibleRole.PushButton;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;float scale=Width/30f;PreferencesLook.Fill(g,ClientRectangle,BackColor);if(Selected||Focused||Hover)PreferencesLook.Round(g,Rectangle.Inflate(ClientRectangle,-1,-1),PreferencesLook.Input,(int)(5*scale),Focused?(Color?)CaptureLook.Blue:null);var box=new Rectangle((int)(5*scale),(int)(5*scale),(int)(20*scale),(int)(20*scale));PreferencesLook.Round(g,box,Ink,(int)(3*scale),Ink.R>235&&Ink.G>235&&Ink.B>235?(Color?)Color.FromArgb(175,178,184):null);}
    }
    internal sealed class SpectrumLaunchButton:PreferencesPaintButton
    {
        internal SpectrumLaunchButton(){BackColor=Color.White;AccessibleName="自定义颜色";}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);int inset=(int)(Width/6f);if(Focused||Hover)PreferencesLook.Round(g,Rectangle.Inflate(ClientRectangle,-1,-1),PreferencesLook.Input,5,Focused?(Color?)CaptureLook.Blue:null);var r=Rectangle.Inflate(ClientRectangle,-inset,-inset);ColorSpectrum.PaintRainbow(g,r);using(var white=new System.Drawing.Drawing2D.LinearGradientBrush(r,Color.Transparent,Color.White,90f))g.FillRectangle(white,r);}
    }
    internal sealed class AnnotationPalette:UserControl
    {
        readonly ToolTip tips=new ToolTip();readonly List<Control> presets=new List<Control>();
        readonly Label current=new Label();readonly CaptureIconButton sample=new CaptureIconButton{Glyph="color",AccessibleName="从原图取色 C",BackColor=Color.White};
        readonly Label customTitle;readonly PreferencesButton back;readonly float scale;
        internal readonly ColorEditor Editor;internal bool Expanded{get;private set;}internal Color Current{get;private set;}
        internal event EventHandler ExpandedChanged;
        internal Action Sample;
        int Px(float n){return (int)Math.Round(n*scale);}
        internal AnnotationPalette(Color color,Action<Color> choose,Action pick,float displayScale=1)
        {
            scale=displayScale;AutoScaleMode=AutoScaleMode.None;BackColor=Color.White;Font=new Font("Microsoft YaHei UI",13*scale,FontStyle.Regular,GraphicsUnit.Pixel);DoubleBuffered=true;Sample=pick;
            Heading("常用标注",8);Row(32,AnnotationColors.CommonNames,AnnotationColors.CommonHex,"常用",choose);
            Heading("分析图",108);Row(132,AnnotationColors.AnalysisNames,AnnotationColors.AnalysisHex,"分析图",choose);
            var custom=new SpectrumLaunchButton{Bounds=new Rectangle(Px(262),Px(4),Px(30),Px(30))};custom.Click+=delegate{SetExpanded(true);};tips.SetToolTip(custom,"自定义颜色 · 色相、RGB、HEX");Controls.Add(custom);
            current.Bounds=new Rectangle(Px(12),Px(212),Px(230),Px(22));current.ForeColor=PreferencesLook.Muted;Controls.Add(current);
            sample.Bounds=new Rectangle(Px(260),Px(206),Px(32),Px(32));sample.Click+=delegate{if(Sample!=null)Sample();};tips.SetToolTip(sample,"从原图取色 C");Controls.Add(sample);
            foreach(Control control in Controls)presets.Add(control);
            customTitle=new Label{Text="自定义颜色",Bounds=new Rectangle(Px(12),Px(8),Px(168),Px(24)),ForeColor=PreferencesLook.Ink};Controls.Add(customTitle);
            back=new PreferencesButton{Text="返回预设",AccessibleName="返回预设颜色",Bounds=new Rectangle(Px(212),Px(4),Px(80),Px(28)),BackColor=Color.White};back.Click+=delegate{SetExpanded(false);};Controls.Add(back);
            Editor=new ColorEditor(scale,delegate(Color selected){SetCurrent(selected);choose(selected);}){Location=new Point(0,Px(36))};Controls.Add(Editor);
            SetCurrent(color);SetExpanded(false);
        }
        void Heading(string text,int y){Controls.Add(new Label{Text=text,Bounds=new Rectangle(Px(12),Px(y),Px(230),Px(22)),ForeColor=PreferencesLook.Muted});}
        void Row(int y,string[] names,string[] values,string group,Action<Color> choose){for(int i=0;i<values.Length;i++){var color=ColorTranslator.FromHtml("#"+values[i]);var button=new PaletteSwatch{Ink=color,Bounds=new Rectangle(Px(10+(i%8)*36),Px(y+(i/8)*36),Px(30),Px(30)),AccessibleName=group+" · "+names[i]+" #"+values[i],Tag=group};tips.SetToolTip(button,button.AccessibleName);button.Click+=delegate{SetCurrent(color);choose(color);};Controls.Add(button);}}
        internal void SetExpanded(bool value)
        {
            Expanded=value;SuspendLayout();foreach(var control in presets)control.Visible=!value;sample.Enabled=Sample!=null;sample.Visible=Sample!=null;sample.Bounds=value?new Rectangle(Px(168),Px(3),Px(32),Px(30)):new Rectangle(Px(260),Px(206),Px(32),Px(32));customTitle.Visible=back.Visible=Editor.Visible=value;Size=new Size(Px(304),Px(value?370:246));ResumeLayout();
            var handler=ExpandedChanged;if(handler!=null)handler(this,EventArgs.Empty);
        }
        internal void SetCurrent(Color color){Current=Color.FromArgb(255,color);current.Text="当前 "+ColorValues.Format(color,"HEX",true);Editor.SetCurrent(Current);foreach(Control control in Controls){var swatch=control as PaletteSwatch;if(swatch!=null){swatch.Selected=swatch.Ink.ToArgb()==Current.ToArgb();swatch.Invalidate();}}}
        internal static bool TryHex(string text,out Color color){color=Color.Empty;if(text==null)return false;text=text.Trim().TrimStart('#');if(text.Length!=6)return false;foreach(char c in text)if(!Uri.IsHexDigit(c))return false;color=ColorTranslator.FromHtml("#"+text);return true;}
        internal Bitmap RenderOffscreen(){var result=new Bitmap(Width,Height);using(var g=Graphics.FromImage(result))PreferencesPreview.Draw(g,this,this);return result;}
        protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
    }
}
