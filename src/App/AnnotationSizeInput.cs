using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ScreenCraft
{
    internal sealed class AnnotationSizeInput:NumericUpDown
    {
        decimal editMinimum=1,editMaximum=24;
        internal AnnotationSizeInput(){BorderStyle=BorderStyle.None;Width=54;DecimalPlaces=2;Increment=.5m;Minimum=1;Maximum=24;Value=4;TextAlign=HorizontalAlignment.Center;AccessibleName="线宽或字号数值";}
        internal void Display(float value,int minimum,int maximum)
        {
            editMinimum=minimum;editMaximum=maximum;
            // Older projects can display their actual value without silently changing it.
            Minimum=Math.Min(minimum,(decimal)value);Maximum=Math.Max(maximum,(decimal)value);Value=(decimal)value;
        }
        internal void CommitEdit(){decimal parsed;bool valid=decimal.TryParse(Text,NumberStyles.Number,CultureInfo.CurrentCulture,out parsed);UserEdit=false;if(valid)Value=Math.Max(editMinimum,Math.Min(editMaximum,parsed));UpdateEditText();}
        internal void CancelEdit(){UserEdit=false;UpdateEditText();}
        internal bool HandleKey(Keys keys,Action returnFocus)
        {
            if(keys==Keys.Enter){CommitEdit();returnFocus();return true;}if(keys==Keys.Escape){CancelEdit();returnFocus();return true;}return false;
        }
    }
    internal static class AnnotationSizing
    {
        internal const int DefaultWidth=4,MaximumWidth=24;
        internal static void Range(Canvas canvas,out float value,out int minimum,out int maximum,out string label)
        {
            var shape=canvas.SelectedShape;var style=canvas.DefaultStyle;var kind=canvas.ActiveKind;
            minimum=1;maximum=MaximumWidth;label="线宽";value=shape==null?style.Width:shape.Width;
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
            value=Math.Max(1,Math.Min(MaximumWidth,value));if(canvas.HasSelection)canvas.EditSelected(s=>s.Width=value);else canvas.ChangeDefaultStyle(d=>d.Width=value);
        }
    }
}
