using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ScreenCraft
{
    internal sealed class AnnotationSizeInput:TextBox
    {
        decimal editMinimum=1,editMaximum=24;
        decimal value=4;
        internal int DecimalPlaces=2;
        string ValueText{get{return decimal.Round(value,DecimalPlaces).ToString("0.##",CultureInfo.CurrentCulture);}}
        internal decimal Minimum{get;set;}
        internal decimal Maximum{get;set;}
        internal decimal Value
        {
            get{return value;}
            set{
                decimal next=Math.Max(Minimum,Math.Min(Maximum,value));
                bool changed=next!=this.value;this.value=next;CancelEdit();
                if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);
            }
        }
        internal event EventHandler ValueChanged;
        internal AnnotationSizeInput(){BorderStyle=BorderStyle.None;Width=54;Minimum=1;Maximum=24;TextAlign=HorizontalAlignment.Center;AccessibleName="线宽或字号数值";CancelEdit();}
        internal void Display(float value,int minimum,int maximum)
        {
            editMinimum=minimum;editMaximum=maximum;
            // Older projects can display their actual value without silently changing it.
            Minimum=Math.Min(minimum,(decimal)value);Maximum=Math.Max(maximum,(decimal)value);this.value=(decimal)value;CancelEdit();
        }
        internal void CommitEdit()
        {
            if(Text==ValueText)return;
            decimal parsed;
            if(!decimal.TryParse(Text,NumberStyles.Number,CultureInfo.CurrentCulture,out parsed)){CancelEdit();return;}
            decimal next=Math.Max(editMinimum,Math.Min(editMaximum,decimal.Round(parsed,DecimalPlaces)));
            bool changed=next!=value;value=next;CancelEdit();
            if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);
        }
        internal void CancelEdit(){Text=ValueText;}
        protected override void OnLeave(EventArgs e){CommitEdit();base.OnLeave(e);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(HandleKey(e.KeyData,delegate{})){e.Handled=true;e.SuppressKeyPress=true;}
            base.OnKeyDown(e);
        }
        internal bool HandleKey(Keys keys,Action returnFocus)
        {
            if(keys==Keys.Enter){CommitEdit();returnFocus();return true;}if(keys==Keys.Escape){CancelEdit();returnFocus();return true;}return false;
        }
    }
    internal sealed class CaptureNumberField:Panel
    {
        readonly AnnotationSizeInput input;
        Color surface=PreferencesLook.Background;
        internal CaptureNumberField(AnnotationSizeInput editor)
        {
            input=editor;
            DoubleBuffered=true;
            Size=new Size(22,24);
            Margin=new Padding(2,0,3,0);
            input.ForeColor=PreferencesLook.Ink;
            Controls.Add(input);
            input.GotFocus+=delegate{UpdateFace();};
            input.LostFocus+=delegate{UpdateFace();};
            UpdateFace();
            PerformLayout();
        }
        internal void MatchMaterial(CaptureBar bar,Point stripOrigin)
        {
            if(bar.Material==null)return;
            int x=stripOrigin.X+Left+Width/2,y=stripOrigin.Y+Top+Height/2;
            if(x<0||y<0||x>=bar.Material.Width||y>=bar.Material.Height)return;
            var sample=bar.Material.GetPixel(x,y);
            if(surface.ToArgb()==sample.ToArgb())return;
            surface=sample;
            UpdateFace();
        }
        void UpdateFace()
        {
            input.BackColor=input.Focused?Color.FromArgb((surface.R+255)/2,(surface.G+255)/2,(surface.B+255)/2):
                Color.FromArgb(surface.R*97/100,surface.G*97/100,surface.B*97/100);
            Invalidate();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if(input==null)return;
            int inset=Math.Max(1,Height/24);
            input.SetBounds(inset,(Height-input.PreferredHeight)/2,Math.Max(1,Width-2*inset),input.PreferredHeight);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            input.Focus();input.SelectAll();
        }
        internal void PaintField(Graphics g,Rectangle bounds,bool includeText)
        {
            if(!FrozenGlass.PaintChild(this,g,bounds))PreferencesLook.Fill(g,bounds,surface);
            PreferencesLook.Round(g,bounds,input.BackColor,Math.Max(3,Height/4),
                input.Focused?CaptureLook.Blue:Color.FromArgb(36,65,74,84));
            if(includeText)PreferencesLook.Text(g,input.Text,input.Font,bounds,input.ForeColor);
        }
        protected override void OnPaint(PaintEventArgs e){PaintField(e.Graphics,ClientRectangle,false);}
    }

    internal static class AnnotationSizing
    {
        internal const int DefaultWidth=4,MaximumWidth=24;
        internal static void Range(Canvas canvas,out float value,out int minimum,out int maximum,out string label)
        {
            var shape=canvas.SelectedShape;var style=canvas.DefaultStyle;var kind=canvas.ActiveKind;
            minimum=1;maximum=MaximumWidth;label="线宽";value=shape==null?style.Width:shape.Width;
            if(kind==Tool.PenPath){value=canvas.ActivePathWidth;if(canvas.HasPathAnchor)label="段线宽";}
            if(kind==Tool.Crop){minimum=0;maximum=200;label="圆角";value=canvas.OutputCornerRadius;}
            else if(kind==Tool.Pixelate){minimum=12;maximum=64;label="色块";value=shape==null?style.MosaicBlock:shape.MosaicBlock==0?Math.Max(12,Math.Min(canvas.Img.Width,canvas.Img.Height)/45):shape.MosaicBlock;}
            else if(kind==Tool.Spotlight){maximum=95;label="压暗 %";value=shape==null?style.EffectOpacity:shape.EffectOpacity>0?shape.EffectOpacity:68;}
            else if(kind==Tool.Text||kind==Tool.Watermark||canvas.ActiveHasNote){minimum=8;maximum=300;label="字号";value=canvas.ActiveTextSize;}
        }
        internal static void Apply(Canvas canvas,float value)
        {
            var kind=canvas.ActiveKind;
            if(kind==Tool.Crop){canvas.SetOutputCornerRadius(Math.Max(0,Math.Min(200,value)));return;}
            if(kind==Tool.Text||kind==Tool.Watermark||canvas.ActiveHasNote){canvas.SetTextSize(Math.Max(8,Math.Min(300,value)));return;}
            if(kind==Tool.Pixelate){int block=(int)Math.Round(Math.Max(12,Math.Min(64,value)));if(canvas.HasSelection)canvas.EditSelected(s=>s.MosaicBlock=block);else canvas.ChangeDefaultStyle(d=>d.MosaicBlock=block);return;}
            if(kind==Tool.Spotlight){value=Math.Max(1,Math.Min(95,value));if(canvas.HasSelection)canvas.EditSelected(s=>s.EffectOpacity=value);else canvas.ChangeDefaultStyle(d=>d.EffectOpacity=value);return;}
            value=Math.Max(1,Math.Min(MaximumWidth,value));
            if(kind==Tool.PenPath){canvas.SetPathWidth(value);return;}
            if(canvas.HasSelection)canvas.EditSelected(s=>{if(!s.Polygon)s.Width=value;});else canvas.ChangeDefaultStyle(d=>d.Width=value);
        }
    }
}
