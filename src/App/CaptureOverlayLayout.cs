using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenCraft
{
    public sealed partial class CaptureOverlay
    {
        struct StripItem
        {
            internal Control Control;
            internal Size Size;
            internal Padding Margin;
        }

        readonly List<StripItem> stripItems=new List<StripItem>();
        Rectangle scaleArea;
        float chromeScale=1;

        void RememberStripSizes(Control strip)
        {
            foreach(Control child in strip.Controls)
                stripItems.Add(new StripItem{Control=child,Size=child.Size,Margin=child.Margin});
        }

        int Px(int value){return (int)Math.Round(value*chromeScale);}

        void ApplyMonitorScale(Rectangle area)
        {
            if(area==scaleArea)return;
            scaleArea=area;
            var screenArea=area;
            screenArea.Offset(desktop.Location);
            SetChromeScale(CaptureHost.ScaleFor(Screen.FromRectangle(screenArea)));
        }

        internal void SetChromeScale(float scale)
        {
            chromeScale=scale;
            canvas.Ui=scale;
            var font=Fonts.Get("Microsoft YaHei UI",12*scale);
            dimensions.Font=font;
            tools.SuspendLayout();
            options.SuspendLayout();
            try{
                tools.Font=options.Font=font;
                foreach(var item in stripItems){
                    var m=item.Margin;
                    int spare=Px(36)-Px(item.Size.Height);
                    item.Control.Margin=new Padding(Px(m.Left),spare/2,Px(m.Right),spare-spare/2);
                    item.Control.Size=new Size(Px(item.Size.Width),Px(item.Size.Height));
                }
            }finally{options.ResumeLayout(true);tools.ResumeLayout(true);}
        }

        void FitMainToolbarWidth(int available)
        {
            int width=0,iconCount=0;
            foreach(var item in stripItems){
                if(item.Control.Parent!=tools)continue;
                width+=Px(item.Size.Width)+Px(item.Margin.Left)+Px(item.Margin.Right);
                if(item.Control is CaptureIconButton)iconCount++;
            }
            int reduction=iconCount==0?0:Math.Max(0,(int)Math.Ceiling((width-available)/(double)iconCount));
            // Shrink only enough to fit one row. Narrower screens retain the full-sized wrap.
            if(reduction>Px(5))reduction=0;
            foreach(var item in stripItems){
                if(item.Control.Parent!=tools||!(item.Control is CaptureIconButton))continue;
                int buttonWidth=Px(item.Size.Width)-reduction;
                if(item.Control.Width!=buttonWidth)item.Control.Width=buttonWidth;
            }
        }

        Size LayoutStrip(FlowLayoutPanel strip,int available,int padding)
        {
            if(strip==tools)FitMainToolbarWidth(available-2*padding);
            int natural=0;
            foreach(Control item in strip.Controls)
                if(!hiddenOptions.Contains(item))natural+=item.Width+item.Margin.Horizontal;
            int width=Math.Max(1,Math.Min(natural,available-2*padding));
            strip.SuspendLayout();
            try{
                strip.Location=new Point(padding,padding);
                strip.Width=width;
                // Keep each caption with its slider when a narrow monitor requires another row.
                foreach(Control item in strip.Controls)strip.SetFlowBreak(item,false);
                Control previous=null;int used=0,rows=1;
                foreach(Control item in strip.Controls){
                    if(hiddenOptions.Contains(item))continue;
                    int cell=item.Width+item.Margin.Horizontal,total=cell;Control partner=null;
                    if(item==anchorValue)partner=anchorSize;else if(item==strokeValue)partner=stroke;else if(item==widthValue)partner=labelWidth;else if(item==radiusLabel)partner=radiusSlider;
                    if(partner!=null&&!hiddenOptions.Contains(partner))total+=partner.Width+partner.Margin.Horizontal;
                    if(item==anchorValue)total+=anchorField.Width+anchorField.Margin.Horizontal;
                    if(item==strokeValue)total+=strokeField.Width+strokeField.Margin.Horizontal;
                    if(item==radiusLabel)total+=radiusField.Width+radiusField.Margin.Horizontal;
                    if(used>0&&used+total>width){strip.SetFlowBreak(previous,true);used=0;rows++;}
                    used+=cell;previous=item;
                }
                strip.Height=rows*Px(36);
            }finally{strip.ResumeLayout(true);}
            return new Size(width+2*padding,strip.Height+2*padding);
        }
    }

    internal sealed class CaptureLabel:Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            PreferencesLook.Text(e.Graphics,Text,Font,ClientRectangle,ForeColor);
        }
    }

    // A transparent WinForms flow panel asks its parent to repaint the whole card.
    // The client row already has precisely the material it needs in the cached bar.
    internal sealed class CaptureFlow:FlowLayoutPanel
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if(!FrozenGlass.PaintChild(this,e.Graphics,ClientRectangle))base.OnPaintBackground(e);
        }
    }
}
