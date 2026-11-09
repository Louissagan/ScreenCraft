using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    public sealed partial class CaptureOverlay
    {
        readonly CaptureSlider anchorSize=new CaptureSlider{Width=78,Minimum=8,Maximum=80,Value=28,AccessibleName="标记大小与引线"};
        readonly Label anchorValue=new CaptureLabel{Width=68,Height=32,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent};
        CaptureIconButton watermarkProperties;
        void BuildAnnotationControls()
        {
            options.Controls.Add(anchorValue);options.Controls.Add(anchorSize);
            anchorSize.ValueChanged+=delegate{if(syncing)return;if(canvas.ActiveKind==Tool.Cloud)Style(s=>s.Width=anchorSize.Value,d=>d.Width=anchorSize.Value);else canvas.SetMarkerSize(anchorSize.Value);};
            tips.SetToolTip(anchorSize,"点大小与引线粗细按比例联动；云线模式调整边界粗细。");
            watermarkProperties=Add(options,"settings","水印参数：内容、角度、间距、透明度",delegate{using(var dialog=new WatermarkDialog(canvas))dialog.ShowDialog(this);});
            // Shared order: source, color, source size, label typography, wrapping width.
            foreach(var control in new Control[]{anchorValue,anchorSize,strokeValue,stroke,widthValue,labelWidth,autoWidth})options.Controls.SetChildIndex(control,options.Controls.Count-1);
        }
        void RefreshContextualControls()
        {
            var kind=canvas.ActiveKind;var s=canvas.SelectedShape;var d=canvas.DefaultStyle;
            bool note=canvas.ActiveHasNote,text=kind==Tool.Text||note,mosaic=kind==Tool.Pixelate,watermark=kind==Tool.Watermark,spotlight=kind==Tool.Spotlight,crop=kind==Tool.Crop;
            foreach(var pair in toolButtons)pair.Value.Selected=pair.Key==Tool.Callout?note:pair.Key==kind;
            if(note)annotationTool=kind;pointMode.Selected=kind==Tool.Callout;counterMode.Selected=kind==Tool.Counter;cloudMode.Selected=kind==Tool.Cloud;
            OptionVisible(pointMode,note);OptionVisible(counterMode,note);OptionVisible(cloudMode,note);OptionVisible(noteDivider,note);
            propertyTitle.Text=canvas.SelectionCount>1?"已选 "+canvas.SelectionCount:note?"注释":crop?"导出":ToolbarSettings.Name(kind.ToString());
            swatch.Swatch=PaletteColor;OptionVisible(swatch,!mosaic&&!crop&&!spotlight&&kind!=Tool.Redact);
            OptionVisible(anchorValue,note);OptionVisible(anchorSize,note);anchorSize.Minimum=kind==Tool.Cloud?1:8;anchorSize.Maximum=kind==Tool.Cloud?128:200;
            float marker=kind==Tool.Cloud?(s==null?d.Width:s.Width):canvas.ActiveMarkerSize;anchorSize.Value=(int)Math.Round(marker);anchorValue.Text=(kind==Tool.Cloud?"云线 ":"点大小 ")+marker.ToString("0.#");
            float value=mosaic?(s==null?d.MosaicBlock:s.MosaicBlock==0?Math.Max(12,Math.Min(canvas.Img.Width,canvas.Img.Height)/45):s.MosaicBlock):spotlight?(s==null?d.EffectOpacity:s.EffectOpacity>0?s.EffectOpacity:68):text||watermark?canvas.ActiveTextSize:(s==null?d.Width:s.Width);
            stroke.Minimum=mosaic?12:text||watermark?8:1;stroke.Maximum=Math.Max(spotlight?95:mosaic?64:text||watermark?300:128,(int)Math.Ceiling(value));stroke.Value=(int)Math.Round(value);
            strokeValue.Text=(mosaic?"色块 ":spotlight?"压暗 ":text||watermark?"字号 ":"线宽 ")+value.ToString("0.#")+(spotlight?"%":"");
            OptionVisible(stroke,!crop&&kind!=Tool.Redact);OptionVisible(strokeValue,!crop&&kind!=Tool.Redact);
            OptionVisible(labelWidth,text);OptionVisible(widthValue,text);OptionVisible(autoWidth,text);labelWidth.Value=(int)canvas.ActiveNoteWidth;autoWidth.Selected=s==null?d.NoteAutoWidth:s.NoteAutoWidth;widthValue.Text=(autoWidth.Selected?"上限 ":"宽 ")+labelWidth.Value;
            OptionVisible(transparentText,false);OptionVisible(solidText,false);OptionVisible(circleNumber,false);OptionVisible(leaderNumber,false);
            int radius=(int)(crop?canvas.OutputCornerRadius:s==null?d.Radius:s.CornerRadius);radiusSlider.Maximum=Math.Max(200,radius);radiusSlider.Value=radius;radiusLabel.Text="圆角 "+radius;OptionVisible(radiusSlider,crop||kind==Tool.Rect);OptionVisible(radiusLabel,crop||kind==Tool.Rect);
            OptionVisible(square,kind==Tool.Rect);OptionVisible(round,kind==Tool.Rect);
            bool geometry=kind==Tool.Rect||kind==Tool.Ellipse||kind==Tool.Cloud;OptionVisible(emptyFill,geometry);OptionVisible(filled,geometry);emptyFill.Selected=!(s==null?d.FillEnabled:s.FillEnabled);filled.Selected=!emptyFill.Selected;
            OptionVisible(solid,!text&&!mosaic&&!crop&&!watermark&&!spotlight&&kind!=Tool.Redact);OptionVisible(dash,!text&&!mosaic&&!crop&&!watermark&&!spotlight&&kind!=Tool.Redact);solid.Selected=(s==null?d.Dash:s.Dash)==System.Drawing.Drawing2D.DashStyle.Solid;dash.Selected=!solid.Selected;
            OptionVisible(arrowThin,kind==Tool.Arrow);OptionVisible(arrowBold,kind==Tool.Arrow);OptionVisible(outlineArrow,kind==Tool.Arrow);arrowThin.Selected=s==null?d.SimpleArrow:s.SimpleArrow;outlineArrow.Selected=s==null?d.ArrowWhiteOutline:s.ArrowWhiteOutline;arrowBold.Selected=!arrowThin.Selected&&!outlineArrow.Selected;
            OptionVisible(rawPen,kind==Tool.Pen);OptionVisible(smoothPen,kind==Tool.Pen);OptionVisible(freeArrow,kind==Tool.Pen);smoothPen.Selected=s==null?d.SmoothPen:s.SmoothPen;rawPen.Selected=!smoothPen.Selected;freeArrow.Selected=s==null?d.FreehandArrow:s.FreehandArrow;
            OptionVisible(mosaicBlocks,mosaic);OptionVisible(mosaicBlur,mosaic);OptionVisible(mosaicDots,mosaic);int mode=s==null?d.RedactionMode:s.RedactionMode;mosaicBlocks.Selected=mode==0;mosaicBlur.Selected=mode==1;mosaicDots.Selected=mode==2;
            OptionVisible(watermarkProperties,watermark);optionsShown=kind!=Tool.Redact;
        }
    }
}
