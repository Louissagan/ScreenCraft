using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class BoardView:Control
    {
        internal readonly BoardDocument Document;
        string selectedId;
        internal event EventHandler SelectionChanged;
        internal string SelectedId{
            get{return selectedId;}
            set{if(selectedId==value)return;selectedId=value;if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}
        }
       internal float Zoom=1;internal PointF Pan=new PointF(40,40);internal float? GuideX,GuideY;
        bool dragging,resizing,panning,checkpoint;Point startScreen;PointF startWorld,startPan;BoardItem original;
        internal BoardView(BoardDocument document){Document=document;DoubleBuffered=true;BackColor=PreferencesLook.Background;TabStop=true;SetStyle(ControlStyles.Selectable|ControlStyles.ResizeRedraw,true);Document.Changed+=Changed;}
        void Changed(object sender,EventArgs e){if(Document.Find(SelectedId)==null)SelectedId=null;Invalidate();}
        internal PointF World(Point screen){return new PointF((screen.X-Pan.X)/Zoom,(screen.Y-Pan.Y)/Zoom);}
        internal RectangleF ScreenBounds(BoardItem item){var bounds=Document.Bounds(item);return new RectangleF(bounds.X*Zoom+Pan.X,bounds.Y*Zoom+Pan.Y,bounds.Width*Zoom,bounds.Height*Zoom);}
        internal void Fit(){var bounds=Document.ContentBounds;Zoom=Math.Max(.001f,Math.Min(4,Math.Min(Math.Max(1,Width-80)/bounds.Width,Math.Max(1,Height-80)/bounds.Height)));Pan=new PointF(Width/2f-(bounds.Left+bounds.Width/2)*Zoom,Height/2f-(bounds.Top+bounds.Height/2)*Zoom);Invalidate();}
        internal void ZoomAt(float factor,Point screen){var world=World(screen);Zoom=Math.Max(.001f,Math.Min(8,Zoom*factor));Pan=new PointF(screen.X-world.X*Zoom,screen.Y-world.Y*Zoom);Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(BackColor);var transform=g.Save();g.TranslateTransform(Pan.X,Pan.Y);g.ScaleTransform(Zoom,Zoom);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            if(Document.Background.A>0)using(var fill=new SolidBrush(Document.Background))g.FillRectangle(fill,Document.ContentBounds);Document.Draw(g);g.Restore(transform);
            var selected=Document.Find(SelectedId);if(selected!=null){var bounds=ScreenBounds(selected);using(var pen=new Pen(CaptureLook.Blue,2))g.DrawRectangle(pen,bounds.X,bounds.Y,bounds.Width,bounds.Height);var grip=new RectangleF(bounds.Right-5,bounds.Bottom-5,10,10);g.FillRectangle(Brushes.White,grip);using(var pen=new Pen(CaptureLook.Blue))g.DrawRectangle(pen,grip.X,grip.Y,grip.Width,grip.Height);}
            using(var pen=new Pen(CaptureLook.Blue,1)){if(GuideX.HasValue)g.DrawLine(pen,GuideX.Value*Zoom+Pan.X,0,GuideX.Value*Zoom+Pan.X,Height);if(GuideY.HasValue)g.DrawLine(pen,0,GuideY.Value*Zoom+Pan.Y,Width,GuideY.Value*Zoom+Pan.Y);}
            if(Document.Items.Length==0)PreferencesLook.Text(g,"拖入图片、粘贴，或加入当前贴图开始排版",Font,ClientRectangle,PreferencesLook.Muted);
            base.OnPaint(e);
        }
        internal void PointerDown(Point at,bool right)
        {
            Focus();startScreen=at;startWorld=World(at);checkpoint=false;GuideX=GuideY=null;if(right){panning=true;startPan=Pan;return;}
            var chosen=Document.Items.Reverse().FirstOrDefault(delegate(BoardItem item){return Document.Bounds(item).Contains(startWorld);});SelectedId=chosen==null?null:chosen.Id;original=chosen;
            dragging=chosen!=null;resizing=chosen!=null&&Math.Abs(ScreenBounds(chosen).Right-at.X)<10&&Math.Abs(ScreenBounds(chosen).Bottom-at.Y)<10;Invalidate();
        }
        internal void PointerMove(Point at,bool disableSnap)
        {
            if(panning){Pan=new PointF(startPan.X+at.X-startScreen.X,startPan.Y+at.Y-startScreen.Y);Invalidate();return;}if(!dragging||original==null||(!checkpoint&&at==startScreen))return;
            var now=World(at);if(!checkpoint){Document.Checkpoint();checkpoint=true;}
            if(resizing){var bounds=Document.Bounds(original);float factor=PinGeometry.ResizeFactor(Size.Round(bounds.Size),new Point((int)(now.X-startWorld.X),(int)(now.Y-startWorld.Y)));Document.PreviewChange(SelectedId,delegate(BoardItem item){item.Scale=Math.Max(.01f,Math.Min(10,original.Scale*factor));});}
            else{var bounds=Document.Bounds(original);bounds.Offset(now.X-startWorld.X,now.Y-startWorld.Y);PointF location=bounds.Location;GuideX=GuideY=null;if(!disableSnap){var others=Document.Items.Where(delegate(BoardItem item){return item.Id!=SelectedId;}).Select(Document.Bounds).ToArray();location=BoardSnap.Move(bounds,others,8/Zoom,out GuideX,out GuideY);}Document.PreviewChange(SelectedId,delegate(BoardItem item){item.X=location.X;item.Y=location.Y;});}
        }
        internal void PointerUp(){dragging=resizing=panning=false;original=null;GuideX=GuideY=null;Invalidate();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left||e.Button==MouseButtons.Right){PointerDown(e.Location,e.Button==MouseButtons.Right);Capture=true;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);PointerMove(e.Location,(ModifierKeys&Keys.Alt)!=0);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==(panning?MouseButtons.Right:MouseButtons.Left)){PointerMove(e.Location,(ModifierKeys&Keys.Alt)!=0);PointerUp();Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)PointerUp();}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ZoomAt((float)Math.Pow(1.12,e.Delta/120.0),e.Location);}
        internal bool ApplyKey(Keys key){if(key==Keys.Delete){Document.Delete(SelectedId);return true;}if(key==(Keys.Control|Keys.D)){SelectedId=Document.Duplicate(SelectedId);Invalidate();return true;}if(key==(Keys.Control|Keys.Z)){Document.Undo();return true;}if(key==(Keys.Control|Keys.Y)){Document.Redo();return true;}var code=key&Keys.KeyCode;if((key&Keys.Modifiers)==Keys.None||(key&Keys.Modifiers)==Keys.Shift){int step=(key&Keys.Shift)!=0?10:1;if(code==Keys.Left||code==Keys.Right||code==Keys.Up||code==Keys.Down){Document.Change(SelectedId,delegate(BoardItem item){item.X+=code==Keys.Left?-step:code==Keys.Right?step:0;item.Y+=code==Keys.Up?-step:code==Keys.Down?step:0;});return true;}}return false;}
        protected override bool ProcessCmdKey(ref Message message,Keys key){if(ApplyKey(key))return true;return base.ProcessCmdKey(ref message,key);}
        protected override void Dispose(bool disposing){if(disposing)Document.Changed-=Changed;base.Dispose(disposing);}
    }
    internal sealed class BoardWindow:Form
    {
        readonly List<CaptureIconButton> selectionActions=new List<CaptureIconButton>();
        readonly CaptureIconButton undo,redo;
        internal readonly BoardDocument Document;internal readonly BoardView View;readonly FlowLayoutPanel tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),MinimumSize=new Size(0,52),BackColor=Color.White};readonly Label status=new Label{Dock=DockStyle.Bottom,Height=34,Padding=new Padding(12,6,0,0),ForeColor=PreferencesLook.Muted};readonly ToolTip tips=new ToolTip();string path;
        internal Func<Bitmap> ReadClipboard=delegate{using(var image=Clipboard.GetImage())return image==null?null:new Bitmap(image);};internal Action<Bitmap> PinImage=delegate(Bitmap image){PinWindow.Open(image);};
        internal BoardWindow(BoardDocument owned=null,string projectPath=null)
        {
            Document=owned??new BoardDocument();path=projectPath;View=new BoardView(Document){Dock=DockStyle.Fill};Text="ScreenCraft · 拼图画板";Icon=ShotStack.AppIcon;Font=new Font("Microsoft YaHei UI",9);ClientSize=new Size(1100,760);MinimumSize=new Size(760,520);StartPosition=FormStartPosition.CenterParent;BackColor=PreferencesLook.Background;
            Add("open","打开画板工程",Open);Add("project","保存画板工程 Ctrl+S",Save);Add("image","添加图片",Import);Add("paste","粘贴图片 Ctrl+V",Paste);Add("stack","加入当前全部贴图",delegate{foreach(var pin in PinWindow.All){var id=Document.AddImage(pin.ViewImage(),new PointF(Document.Items.Length*28,Document.Items.Length*28));View.SelectedId=id;}View.Fit();});
            undo=Add("undo","撤销 Ctrl+Z",Document.Undo);redo=Add("redo","重做 Ctrl+Y",Document.Redo);Add("copy","复制选中图片 Ctrl+D",delegate{View.SelectedId=Document.Duplicate(View.SelectedId);},true);Add("front","上移一层",delegate{Document.Reorder(View.SelectedId,1);},true);Add("back","下移一层",delegate{Document.Reorder(View.SelectedId,-1);},true);Add("rotate","顺时针旋转 90°",delegate{Document.Change(View.SelectedId,delegate(BoardItem item){item.Turns=(item.Turns+1)%4;});},true);Add("cancel","删除选中图片",delegate{Document.Delete(View.SelectedId);},true);
            CaptureIconButton backgroundColor=null;backgroundColor=Add("color","画板背景颜色",delegate{CraftColors.Show(backgroundColor,Document.Background.A==0?Color.White:Document.Background,Document.SetBackground);});Add("no-fill","透明背景",delegate{Document.SetBackground(Color.Transparent);});Add("crop","适应全部图片",View.Fit);Add("rect","视图 100%",delegate{View.ZoomAt(1/View.Zoom,new Point(View.Width/2,View.Height/2));});Add("save","导出完整画板 PNG",Export);Add("pin","选中图片独立贴图",PinSelected,true);
            Controls.AddRange(new Control[]{View,status,tools});View.SelectionChanged+=delegate{UpdateActions();};Document.Changed+=delegate{UpdateStatus();};FormClosing+=delegate(object sender,FormClosingEventArgs e){if(Visible&&Document.Dirty&&MessageBox.Show(this,"画板尚未保存。放弃更改并关闭？","关闭画板",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)e.Cancel=true;};Shown+=delegate{View.Fit();View.Focus();};
            AllowDrop=true;DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;};DragDrop+=delegate(object sender,DragEventArgs e){Act(delegate{foreach(var file in (string[])e.Data.GetData(DataFormats.FileDrop))AddFile(file);View.Fit();});};UpdateStatus();
        }
        CaptureIconButton Add(string glyph,string name,Action action,bool requiresSelection=false){var button=new CaptureIconButton{Glyph=glyph,AccessibleName=name};tips.SetToolTip(button,name);button.Click+=delegate{Act(action);};tools.Controls.Add(button);if(requiresSelection)selectionActions.Add(button);return button;}
        void Act(Action action){try{action();View.Invalidate();UpdateStatus();}catch(Exception ex){status.Text="操作失败："+ex.Message;}}
        void UpdateActions(){
            undo.Enabled=Document.CanUndo;redo.Enabled=Document.CanRedo;
            bool selected=Document.Find(View.SelectedId)!=null;
            foreach(var button in selectionActions)button.Enabled=selected;
        }
        void UpdateStatus(){UpdateActions();status.Text=Document.Items.Length+" 张图片 · 滚轮缩放视图 · 右键拖动画布 · 拖右下角等比缩放 · Alt 关闭吸附";Text="ScreenCraft · 拼图画板"+(Document.Dirty?" *":"");}
        void AddFile(string file){View.SelectedId=Document.AddImage(ShotStack.LoadFull(file),new PointF(Document.Items.Length*28,Document.Items.Length*28));}
        void Import(){using(var dialog=new OpenFileDialog{Filter="图片|*.png;*.jpg;*.jpeg;*.bmp",Multiselect=true})if(dialog.ShowDialog(this)==DialogResult.OK){foreach(var file in dialog.FileNames)AddFile(file);View.Fit();}}
        internal void Paste(){var image=ReadClipboard();if(image==null)throw new InvalidOperationException("剪贴板中没有图片。");View.SelectedId=Document.AddImage(image,View.World(new Point(View.Width/2,View.Height/2)));View.Invalidate();}
        void Save(){if(path==null){using(var dialog=new SaveFileDialog{Filter="ScreenCraft 画板|*.scboard",FileName="参考图画板.scboard"}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;path=dialog.FileName;}}Document.Save(path);UpdateStatus();}
        void Open(){using(var dialog=new OpenFileDialog{Filter="ScreenCraft 画板|*.scboard"})if(dialog.ShowDialog(this)==DialogResult.OK)new BoardWindow(BoardDocument.Open(dialog.FileName),dialog.FileName).Show();}
        void Export(){using(var dialog=new SaveFileDialog{Filter="PNG 图片|*.png",FileName="画板.png"})if(dialog.ShowDialog(this)==DialogResult.OK)using(var image=Document.Render())ImageFiles.SavePng(image,dialog.FileName);}
        internal void PinSelected(){var image=Document.RenderItem(View.SelectedId);try{PinImage(image);}catch{image.Dispose();throw;}}
        protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==(Keys.Control|Keys.S)){Act(Save);return true;}if(key==(Keys.Control|Keys.V)){Act(Paste);return true;}if(View.ApplyKey(key))return true;return base.ProcessCmdKey(ref message,key);}
        protected override void Dispose(bool disposing){if(disposing){tips.Dispose();Document.Dispose();}base.Dispose(disposing);}
    }
}
