using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed class CraftEditor : Form
    {
        readonly Canvas canvas;
        readonly AnnotationInspector inspector;
        readonly Action<AnnotationDocument> updatePin;readonly int pinTurns;readonly bool pinFlipX,pinFlipY;
        public Action RestoreSourceFocus;internal Action<Bitmap> CopyOutput=ShotStack.CopyImage;internal Action<string> CommandError;
        readonly StatusStrip status=new StatusStrip();
        readonly ToolStripStatusLabel hint=new ToolStripStatusLabel();
        readonly ToolTip tips=new ToolTip();readonly FlowLayoutPanel secondary=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(8,4,8,4),BackColor=Color.White};readonly CaptureSlider quickSize=new CaptureSlider{Minimum=1,Maximum=100,Width=120};readonly Label quickValue=new Label{Width=90,Height=34,TextAlign=ContentAlignment.MiddleCenter};bool syncingQuick;
        readonly Dictionary<Tool,CaptureIconButton> toolButtons=new Dictionary<Tool,CaptureIconButton>();
        public CraftEditor(Bitmap image):this(AnnotationDocument.FromImage(image),null){}
        public CraftEditor(AnnotationDocument ownedDocument,Action<AnnotationDocument> update)
        {
            pinTurns=ownedDocument.PinQuarterTurns;pinFlipX=ownedDocument.PinFlipX;pinFlipY=ownedDocument.PinFlipY;updatePin=update;Icon=ShotStack.AppIcon;Text="ScreenCraft · 标注图片";AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
            ClientSize=new Size(1180,760);MinimumSize=new Size(820,540);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);KeyPreview=true;
            try{canvas=new Canvas(new Bitmap(ownedDocument.Source)){Dock=DockStyle.Fill,Ui=DeviceDpi/96f,Bg=new Settings()};canvas.LoadDocument(ownedDocument);}finally{ownedDocument.Dispose();}
            inspector=new AnnotationInspector(canvas);
            BackColor=PreferencesLook.Background;var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(0,50),Height=52,Padding=new Padding(8),BackColor=Color.White,WrapContents=true};
            Tool[] kinds={Tool.Rect,Tool.Ellipse,Tool.Arrow,Tool.Line,Tool.Pen,Tool.Text,Tool.Callout,Tool.Pixelate,Tool.Redact,Tool.Crop};
            string[] names={"矩形 R","椭圆 E","箭头 A","直线 L","画笔 B","文字 T","标注","马赛克 M","实色遮盖","裁剪"};
            for(int i=0;i<kinds.Length;i++){Tool kind=kinds[i];var button=Button(tools,names[i],delegate{canvas.SetTool(kind);canvas.Focus();});toolButtons.Add(kind,button);}
            var actions=tools;
            var undo=Button(actions,"撤销 Ctrl+Z",canvas.Undo);var redo=Button(actions,"重做 Ctrl+Y",canvas.Redo);Button(actions,"复制完成 Enter",Copy);
            Button(actions,update==null?"钉在屏幕 P":"更新此贴图 P",Pin);Button(actions,"保存 PNG",Save);Button(actions,"保存可编辑工程",SaveProject);
            Button(actions,"本地 OCR",delegate{using(var b=RenderPresentation()){if(ShotStack.Running!=null)ShotStack.Running.CopyText(b,null);}});
            Button(actions,"取原图色 C",PickColor);Button(actions,"滚动长截图",delegate{if(ShotStack.Running!=null&&ShotStack.Running.BeginScroll!=null)ShotStack.Running.BeginScroll();});
            Button(actions,"分段导出 PNG",SaveSegments);Button(actions,"图片美化",delegate{using(var dialog=new BackgroundDialog(canvas))dialog.ShowDialog(this);});
            Button(actions,"取消 Esc",Close);
            status.Items.Add(hint);Controls.Add(canvas);Controls.Add(inspector);Controls.Add(status);Controls.Add(secondary);Controls.Add(tools);
            canvas.StateChanged+=delegate{undo.Enabled=canvas.CanUndo;redo.Enabled=canvas.CanRedo;foreach(var pair in toolButtons)pair.Value.Selected=pair.Key==Tool.Callout?canvas.ActiveHasNote:canvas.ActiveKind==pair.Key;hint.Text=canvas.SelectedShape==null?"框选后标注；点选对象可移动，拖动控制点可调整；文字输入时快捷键暂停。":"已选中标注；Delete 删除，方向键移动，Shift+方向键 10px。";};
            BuildSecondary();canvas.SetTool(Tool.Rect);Shown+=delegate{canvas.Focus();};
            AllowDrop=true;DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;};
            FormClosed+=delegate{if(RestoreSourceFocus!=null)RestoreSourceFocus();};
            DragDrop+=delegate(object sender,DragEventArgs e){var paths=(string[])e.Data.GetData(DataFormats.FileDrop);foreach(var path in paths)try{OpenFile(path).Show();}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法打开图片");}};
        }
        internal Canvas AnnotationCanvas{get{return canvas;}}
        CaptureIconButton Button(FlowLayoutPanel strip,string name,Action action){var b=new CaptureIconButton{Glyph=EditorGlyph(name),AccessibleName=name};tips.SetToolTip(b,name);b.Click+=delegate{SafeCommand(action);};strip.Controls.Add(b);return b;}
        static string EditorGlyph(string name){if(name=="颜色")return "color";if(name=="标注"||name=="点标注")return "callout";if(name=="透明底文字")return "no-fill";if(name=="色底白字")return "fill";if(name.StartsWith("矩形"))return "rect";if(name.StartsWith("椭圆"))return "ellipse";if(name.StartsWith("箭头"))return "arrow";if(name.StartsWith("直线"))return "line";if(name.StartsWith("画笔"))return "pen";if(name.StartsWith("文字"))return "text";if(name.StartsWith("序号"))return "counter";if(name.StartsWith("云线"))return "cloud";if(name.StartsWith("马赛克"))return "pixelate";if(name.StartsWith("实色"))return "redact";if(name.StartsWith("裁剪"))return "crop";if(name.StartsWith("撤销"))return "undo";if(name.StartsWith("重做"))return "redo";if(name.StartsWith("复制"))return "copy";if(name.StartsWith("钉")||name.StartsWith("更新"))return "pin";if(name.StartsWith("保存可"))return "project";if(name.StartsWith("保存")||name.StartsWith("分段"))return "save";if(name.StartsWith("本地"))return "ocr";if(name.StartsWith("取原"))return "color";if(name.StartsWith("滚动"))return "scroll";if(name.StartsWith("图片美化"))return "image";if(name.StartsWith("取消"))return "cancel";return "settings";}
        void BuildSecondary(){
            var point=Button(secondary,"点标注",delegate{canvas.SetAnnotationMode(Tool.Callout);canvas.Focus();});
            var cloud=Button(secondary,"云线 W",delegate{canvas.SetAnnotationMode(Tool.Cloud);canvas.Focus();});
            var number=Button(secondary,"序号 N",delegate{canvas.SetAnnotationMode(Tool.Counter);canvas.Focus();});
            CaptureIconButton color=null;color=Button(secondary,"颜色",delegate{CraftColors.Show(color,canvas.ActiveColor,canvas.SetColor,delegate{canvas.BeginColorPick(canvas.SetColor);canvas.Focus();});});
            var marker=new CaptureSlider{Minimum=8,Maximum=200,Width=100,AccessibleName="标记大小与引线"};var markerLabel=new Label{Text="点大小",Width=64,Height=34,TextAlign=ContentAlignment.MiddleCenter};secondary.Controls.Add(markerLabel);secondary.Controls.Add(marker);marker.ValueChanged+=delegate{if(!syncingQuick)canvas.SetMarkerSize(marker.Value);};
            secondary.Controls.Add(quickSize);secondary.Controls.Add(quickValue);
            quickSize.AccessibleName="当前工具尺寸";
            quickSize.ValueChanged+=delegate{if(syncingQuick)return;if(canvas.ActiveKind==Tool.Crop){canvas.SetOutputCornerRadius(quickSize.Value);return;}if(canvas.ActiveKind==Tool.Pixelate){
                if(canvas.SelectedShape!=null)canvas.EditSelected(delegate(Shape shape){shape.MosaicBlock=quickSize.Value;});
                else canvas.ChangeDefaultStyle(delegate(AnnotationStyle style){style.MosaicBlock=quickSize.Value;});
            }else if(canvas.ActiveKind==Tool.Text||canvas.ActiveHasNote){canvas.SetTextSize(quickSize.Value);}else if(canvas.SelectedShape!=null)canvas.EditSelected(delegate(Shape shape){shape.Width=quickSize.Value;});else canvas.ChangeDefaultStyle(delegate(AnnotationStyle style){style.Width=quickSize.Value;});};
            canvas.StateChanged+=delegate{syncingQuick=true;try{bool text=canvas.ActiveKind==Tool.Text||canvas.ActiveHasNote;point.Visible=cloud.Visible=number.Visible=canvas.ActiveHasNote;color.Swatch=canvas.ActiveColor;marker.Visible=markerLabel.Visible=canvas.ActiveKind==Tool.Counter||canvas.ActiveKind==Tool.Callout;marker.Value=(int)canvas.ActiveMarkerSize;point.Selected=canvas.ActiveKind==Tool.Callout;cloud.Selected=canvas.ActiveKind==Tool.Cloud;number.Selected=canvas.ActiveKind==Tool.Counter;bool mosaic=canvas.ActiveKind==Tool.Pixelate;
                var shape=canvas.SelectedShape;

                int block=shape==null?canvas.DefaultStyle.MosaicBlock:shape.MosaicBlock;
                float value=canvas.ActiveKind==Tool.Crop?canvas.OutputCornerRadius:mosaic?(block==0?Math.Max(12,Math.Min(canvas.Img.Width,canvas.Img.Height)/45):block):text?canvas.ActiveTextSize:(shape==null?canvas.DefaultStyle.Width:shape.Width);
                quickSize.Minimum=canvas.ActiveKind==Tool.Crop?0:mosaic?12:text?8:1;quickSize.Maximum=Math.Max(canvas.ActiveKind==Tool.Crop?200:mosaic?64:300,(int)Math.Ceiling(value));
                quickSize.Value=(int)Math.Round(value);quickValue.Text=(canvas.ActiveKind==Tool.Crop?"导出圆角 ":mosaic?"色块 ":text?"字号 ":"线宽 ")+quickSize.Value;
                secondary.Visible=canvas.ActiveKind!=Tool.Redact;}finally{syncingQuick=false;}};
        }
        protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
        public static CraftEditor OpenFile(string path){return string.Equals(Path.GetExtension(path),".sccraft",StringComparison.OrdinalIgnoreCase)?new CraftEditor(AnnotationDocument.Open(path),null):new CraftEditor(ShotStack.LoadFull(path));}
        internal void SafeCommand(Action action){try{action();}catch(Exception ex){hint.Text="操作失败："+ex.Message;if(CommandError!=null)CommandError(hint.Text);}}
        AnnotationDocument ExportWithView(){var doc=canvas.ExportDocument();doc.PinQuarterTurns=pinTurns;doc.PinFlipX=pinFlipX;doc.PinFlipY=pinFlipY;return doc;}
        Bitmap RenderPresentation(){using(var raw=canvas.Render())return PinViews.Render(raw,pinTurns,pinFlipX,pinFlipY);}
        void Copy(){canvas.CommitText();using(var b=RenderPresentation())CopyOutput(b);Close();}
        void Pin(){canvas.CommitText();var doc=ExportWithView();try{if(updatePin!=null)updatePin(doc);else PinWindow.Open(doc);}catch{doc.Dispose();throw;}Close();}
        void Save(){canvas.CommitText();using(var d=new SaveFileDialog{Filter="PNG 图片|*.png",FileName="截图.png"})if(d.ShowDialog(this)==DialogResult.OK)using(var b=RenderPresentation())ImageFiles.SavePng(b,d.FileName);}
        void SaveProject(){canvas.CommitText();using(var d=new SaveFileDialog{Filter="可编辑工程（保留原图，分享请用 PNG）|*.sccraft",FileName="标注工程.sccraft"})if(d.ShowDialog(this)==DialogResult.OK)using(var doc=ExportWithView())doc.Save(d.FileName);}
        void PickColor(){canvas.BeginColorPick(delegate(Color c){var p=ShotStack.Running;var home=p==null?null:p.ColorPreferences;string value=ColorValues.Format(c,home==null?"RGB":home.ColorFormat,home==null||home.ColorUpper);try{Clipboard.SetText(value);hint.Text="已复制原图颜色 "+value;}catch(System.Runtime.InteropServices.ExternalException){hint.Text="颜色 "+value+"；剪贴板正被占用，请重新取色复制。";}});hint.Text="点击原图像素复制颜色；Esc 取消取色。";canvas.Focus();}
        void SaveSegments(){using(var d=new FolderBrowserDialog{Description="选择空目录，按每段 4000 行导出当前图片"})if(d.ShowDialog(this)==DialogResult.OK)using(var b=RenderPresentation()){var files=SegmentedPng.Export(b,d.SelectedPath,4000);hint.Text="已导出 "+files.Length+" 段 PNG。";}}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys)
        {
            if(canvas.Typing || inspector.ContainsFocus)return base.ProcessCmdKey(ref msg,keys);
            if(keys==(Keys.Control|Keys.Z)){canvas.Undo();return true;}if(keys==(Keys.Control|Keys.Y)){canvas.Redo();return true;}
            if(keys==(Keys.Control|Keys.Shift|Keys.Enter)){SafeCommand(delegate{canvas.CommitText();using(var image=RenderPresentation())CopyOutput(image);});return true;}if(keys==(Keys.Control|Keys.C)||keys==Keys.Enter){SafeCommand(Copy);return true;}if(keys==(Keys.Control|Keys.S)){SafeCommand(Save);return true;}
            if(keys==Keys.Escape){if(canvas.Busy){canvas.CancelGesture();return true;}Close();return true;}
            if(keys==(Keys.Control|Keys.A)){canvas.SelectAll();return true;}if(keys==Keys.Delete){canvas.DeleteSelected();return true;}if(keys==(Keys.Control|Keys.D)){canvas.DuplicateSelected();return true;}
            if(keys==Keys.P){SafeCommand(Pin);return true;}
            if(keys==Keys.C){PickColor();return true;}
            var baseKey=keys&Keys.KeyCode;if(baseKey==Keys.Left||baseKey==Keys.Right||baseKey==Keys.Up||baseKey==Keys.Down){int amount=(keys&Keys.Shift)!=0?10:1;canvas.Nudge(baseKey==Keys.Left?-amount:baseKey==Keys.Right?amount:0,baseKey==Keys.Up?-amount:baseKey==Keys.Down?amount:0);return true;}
            Keys[] letters={Keys.R,Keys.E,Keys.A,Keys.L,Keys.B,Keys.T,Keys.N,Keys.W,Keys.M};Tool[] kinds={Tool.Rect,Tool.Ellipse,Tool.Arrow,Tool.Line,Tool.Pen,Tool.Text,Tool.Counter,Tool.Cloud,Tool.Pixelate};
            int index=Array.IndexOf(letters,keys);if(index>=0){canvas.SetTool(kinds[index]);canvas.Focus();return true;}return base.ProcessCmdKey(ref msg,keys);
        }
    }
    internal static class ImageFiles
    {
        public static void SavePng(Bitmap image,string path)
        {
            string full=Path.GetFullPath(path),temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{using(var stream=FilePaths.Create(temp))image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);FilePaths.Commit(temp,full,true);}finally{if(FilePaths.Exists(temp))FilePaths.Delete(temp);}
        }
    }
}
