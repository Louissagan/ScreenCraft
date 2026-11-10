using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal static class AnnotationColors
    {
        // Keep the supplied QQ and diagram colors, with distinct complementary choices.
        internal static readonly string[] CommonNames={"红色","橙色","黄色","黄绿","绿色","青绿","青色","蓝色","深蓝","靛蓝","紫色","玫红","棕色","白色","灰色","黑色"};
        internal static readonly string[] CommonHex={"FB3838","FF9500","FFC23D","94D23D","34A853","009688","00BCD4","0099FF","2455A4","5856D6","9C4DCC","E83E8C","8D5A3B","FFFFFF","999999","000000"};
        internal static readonly string[] AnalysisNames={"青绿","暖黄","珊瑚红","灰蓝","草木绿","浅赭","粉紫","砖红","橄榄","鼠尾草","石板蓝","灰紫"};
        internal static readonly string[] AnalysisHex={"85C4C2","EACD5D","E75A5D","59899F","96B759","ECC199","E15498","B96D56","7B8E3D","8CA193","465D73","8F7D9D"};
        internal static readonly Color Default=ColorTranslator.FromHtml("#"+CommonHex[0]);
        internal static Color FromHsv(double hue,double saturation,double value)
        {
            hue=(hue%360+360)%360;saturation=Math.Max(0,Math.Min(1,saturation));value=Math.Max(0,Math.Min(1,value));
            double c=value*saturation,x=c*(1-Math.Abs(hue/60%2-1)),m=value-c,r=0,g=0,b=0;
            if(hue<60){r=c;g=x;}else if(hue<120){r=x;g=c;}else if(hue<180){g=c;b=x;}else if(hue<240){g=x;b=c;}else if(hue<300){r=x;b=c;}else{r=c;b=x;}
            return Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));
        }
        internal static void ToHsv(Color color,ref double hue,out double saturation,out double value)
        {
            double max=Math.Max(color.R,Math.Max(color.G,color.B)),min=Math.Min(color.R,Math.Min(color.G,color.B));
            value=max/255;saturation=max==0?0:(max-min)/max;if(max!=min)hue=color.GetHue();
        }
    }

    internal sealed class ColorSpectrum:Control
    {
        internal readonly bool HueOnly;
        internal double Hue,Saturation,Value;
        internal event EventHandler ValueChanged;
        bool dragging;
        float ScaleFactor{get{return Math.Max(1,Font.Size/13f);}}
        internal ColorSpectrum(bool hueOnly)
        {
            HueOnly=hueOnly;TabStop=true;Cursor=Cursors.Cross;AccessibleRole=AccessibleRole.Slider;
            AccessibleName=hueOnly?"色相，左右方向键调整":"饱和度与明度，方向键调整";
            SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        }
        internal void SetHsv(double hue,double saturation,double value){Hue=hue;Saturation=saturation;Value=value;Invalidate();}
        static ColorBlend Rainbow(){return new ColorBlend{Colors=new[]{Color.Red,Color.Yellow,Color.Lime,Color.Cyan,Color.Blue,Color.Magenta,Color.Red},Positions=new[]{0f,1/6f,2/6f,3/6f,4/6f,5/6f,1f}};}
        internal static void PaintRainbow(Graphics g,Rectangle area){if(area.Width<1||area.Height<1)return;using(var gradient=new LinearGradientBrush(area,Color.Red,Color.Red,0f)){gradient.InterpolationColors=Rainbow();g.FillRectangle(gradient,area);}}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(Color.White);float scale=ScaleFactor;var r=HueOnly?new Rectangle(0,(int)(7*scale),Width,Math.Max(1,Height-(int)(14*scale))):ClientRectangle;
            if(HueOnly)PaintRainbow(g,r);
            else{
                using(var hue=new LinearGradientBrush(r,Color.White,AnnotationColors.FromHsv(Hue,1,1),0f))g.FillRectangle(hue,r);
                using(var value=new LinearGradientBrush(r,Color.FromArgb(0,0,0,0),Color.Black,90f))g.FillRectangle(value,r);
            }
            g.SmoothingMode=SmoothingMode.AntiAlias;
            float radius=5*scale,x=HueOnly?(float)(Hue/360)*(Width-1):(float)Saturation*(Width-1),y=HueOnly?Height/2f:(float)(1-Value)*(Height-1);
            // Keep the entire cursor visible at pure-white, pure-black and full-saturation edges.
            x=Math.Max(radius+1,Math.Min(Width-radius-1,x));y=Math.Max(radius+1,Math.Min(Height-radius-1,y));
            using(var dark=new Pen(Color.FromArgb(70,73,78),3*scale))using(var light=new Pen(Color.White,1.5f*scale)){g.DrawEllipse(dark,x-radius,y-radius,2*radius,2*radius);g.DrawEllipse(light,x-radius,y-radius,2*radius,2*radius);}
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-2,-2));
        }
        void Notify(){Invalidate();var changed=ValueChanged;if(changed!=null)changed(this,EventArgs.Empty);}
        void Pointer(Point point)
        {
            double x=Math.Max(0,Math.Min(1,point.X/(double)Math.Max(1,Width-1)));
            if(HueOnly)Hue=x*360;else{Saturation=x;Value=1-Math.Max(0,Math.Min(1,point.Y/(double)Math.Max(1,Height-1)));}Notify();
        }
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();dragging=true;Capture=true;Pointer(e.Location);}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)Pointer(e.Location);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left||!dragging)return;Pointer(e.Location);dragging=false;Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override bool IsInputKey(Keys key){var code=key&Keys.KeyCode;return code==Keys.Left||code==Keys.Right||code==Keys.Up||code==Keys.Down||base.IsInputKey(key);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);double step=e.Shift?.1:.01;
            if(e.KeyCode!=Keys.Left&&e.KeyCode!=Keys.Right&&e.KeyCode!=Keys.Up&&e.KeyCode!=Keys.Down)return;
            if(HueOnly)Hue=(Hue+(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down?-1:1)*step*360+360)%360;
            else{if(e.KeyCode==Keys.Left)Saturation-=step;if(e.KeyCode==Keys.Right)Saturation+=step;if(e.KeyCode==Keys.Up)Value+=step;if(e.KeyCode==Keys.Down)Value-=step;Saturation=Math.Max(0,Math.Min(1,Saturation));Value=Math.Max(0,Math.Min(1,Value));}
            e.Handled=e.SuppressKeyPress=true;Notify();
        }
    }

    internal sealed class ColorEditor:UserControl
    {
        internal readonly ColorSpectrum Spectrum=new ColorSpectrum(false),HueStrip=new ColorSpectrum(true);
        internal readonly TextBox Hex=new TextBox{MaxLength=7,AccessibleName="HEX 颜色"};
        internal readonly TextBox[] Rgb=new[]{new TextBox{AccessibleName="红 R"},new TextBox{AccessibleName="绿 G"},new TextBox{AccessibleName="蓝 B"}};
        readonly Label status=new Label();readonly PreferencesButton apply=new PreferencesButton{Text="应用",AccessibleName="应用自定义颜色",Primary=true};
        readonly float scale;Color original;internal Color Preview{get;private set;}bool syncing;double hue,saturation,value;
        int Px(float n){return (int)Math.Round(n*scale);}
        internal ColorEditor(float displayScale,Action<Color> choose)
        {
            scale=displayScale;AutoScaleMode=AutoScaleMode.None;BackColor=Color.White;Font=new Font("Microsoft YaHei UI",13*scale,FontStyle.Regular,GraphicsUnit.Pixel);Size=new Size(Px(304),Px(330));DoubleBuffered=true;
            Spectrum.Bounds=new Rectangle(Px(12),Px(8),Px(280),Px(160));HueStrip.Bounds=new Rectangle(Px(56),Px(180),Px(236),Px(28));Controls.Add(Spectrum);Controls.Add(HueStrip);
            Spectrum.ValueChanged+=delegate{hue=Spectrum.Hue;saturation=Spectrum.Saturation;value=Spectrum.Value;UpdatePreview(AnnotationColors.FromHsv(hue,saturation,value),true);};
            HueStrip.ValueChanged+=delegate{hue=HueStrip.Hue;UpdatePreview(AnnotationColors.FromHsv(hue,saturation,value),true);};
            for(int i=0;i<3;i++){
                var text=Rgb[i];text.MaxLength=3;text.TextAlign=HorizontalAlignment.Center;
                Controls.Add(new Label{Text=new[]{"R","G","B"}[i],Bounds=new Rectangle(Px(12+i*96),Px(214),Px(88),Px(20)),ForeColor=PreferencesLook.Muted,TextAlign=ContentAlignment.MiddleCenter});
                Field(text,12+i*96,236,88);text.TextChanged+=RgbChanged;
            }
            Field(Hex,12,278,172);Hex.TextChanged+=HexChanged;
            apply.Bounds=new Rectangle(Px(196),Px(278),Px(96),Px(32));Controls.Add(apply);apply.Click+=delegate{if(apply.Enabled)choose(Preview);};
            status.Bounds=new Rectangle(Px(12),Px(312),Px(280),Px(18));status.Font=new Font(Font.FontFamily,11*scale,FontStyle.Regular,GraphicsUnit.Pixel);status.ForeColor=Color.FromArgb(177,40,49);Controls.Add(status);
            foreach(var text in Rgb)text.Enter+=delegate(object sender,EventArgs e){((TextBox)sender).SelectAll();};Hex.Enter+=delegate{Hex.SelectAll();};
        }
        void Field(TextBox text,int x,int y,int width){var field=new PreferencesField(text,Px(width)){Bounds=new Rectangle(Px(x),Px(y),Px(width),Px(32)),Padding=new Padding(Px(9),Px(6),Px(9),Px(4))};Controls.Add(field);}
        internal void SetCurrent(Color color){original=Color.FromArgb(255,color);UpdatePreview(original,false);}
        void Error(string text){status.Text=text;apply.Enabled=text.Length==0;}
        void RgbChanged(object sender,EventArgs e)
        {
            if(syncing)return;int[] c=new int[3];for(int i=0;i<3;i++)if(!int.TryParse(Rgb[i].Text,out c[i])||c[i]<0||c[i]>255){Error("RGB 请输入 0–255 的整数");return;}
            UpdatePreview(Color.FromArgb(c[0],c[1],c[2]),false,1);
        }
        void HexChanged(object sender,EventArgs e){if(syncing)return;Color color;if(!AnnotationPalette.TryHex(Hex.Text,out color)){Error("HEX 请输入 6 位，例如 #85C4C2");return;}UpdatePreview(color,false,2);}
        void UpdatePreview(Color color,bool keepHsv,int preserveInput=0)
        {
            Preview=color;if(!keepHsv)AnnotationColors.ToHsv(color,ref hue,out saturation,out value);
            syncing=true;try{if(preserveInput!=1){Rgb[0].Text=color.R.ToString();Rgb[1].Text=color.G.ToString();Rgb[2].Text=color.B.ToString();}if(preserveInput!=2)Hex.Text=ColorValues.Format(color,"HEX",true);}finally{syncing=false;}
            Spectrum.SetHsv(hue,saturation,value);HueStrip.SetHsv(hue,saturation,value);Error("");Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;var r=new Rectangle(Px(12),Px(180),Px(30),Px(28));
            using(var a=new SolidBrush(original))using(var b=new SolidBrush(Preview)){g.FillRectangle(a,r.X,r.Y,r.Width/2,r.Height);g.FillRectangle(b,r.X+r.Width/2,r.Y,r.Width-r.Width/2,r.Height);}using(var pen=new Pen(PreferencesLook.Line))g.DrawRectangle(pen,r);
        }
        protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Enter){if(apply.Enabled)apply.PerformClick();return true;}return base.ProcessCmdKey(ref message,key);}
    }
}
