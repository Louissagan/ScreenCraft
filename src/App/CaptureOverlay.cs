using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed partial class CaptureOverlay:Form
    {
        readonly Canvas canvas;AnnotationInspector inspector;readonly Rectangle desktop;readonly Rectangle[] workAreas;
        readonly ToolbarSettings toolbarSettings;
        readonly int dimPercent;readonly Preferences completionPreferences;internal string OutputRoot=Settings.DataDir;
        readonly int borderWidth;readonly Color borderColor;readonly bool preferToolbarTop;
        readonly CaptureBar bar=new CaptureBar(),optionsBar=new CaptureBar();readonly FlowLayoutPanel tools=Flow(),options=Flow();
        readonly Label propertyTitle=new CaptureLabel{Text="矩形属性",Width=58,Height=32,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent,ForeColor=PreferencesLook.Muted,AccessibleName="当前工具属性"};
        readonly Panel inspectorPanel=new Panel{BorderStyle=BorderStyle.FixedSingle,BackColor=Color.Transparent};
        readonly Label dimensions=new Label{AutoSize=false,Height=23,Width=140,BackColor=Color.FromArgb(38,42,48),ForeColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.SizeAll};
        readonly ToolTip tips=new ToolTip();readonly Dictionary<Tool,CaptureIconButton> toolButtons=new Dictionary<Tool,CaptureIconButton>();
        readonly Rectangle[] handles=new Rectangle[8];readonly CaptureSlider stroke=new CaptureSlider{Width=78};readonly Label strokeValue=new CaptureLabel{Width=66,Height=32,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent};
        CaptureIconButton swatch,square,round,emptyFill,filled,solid,dash,undo,redo,arrowThin,arrowBold;bool syncing,inspectorOpen,refreshing,optionsShown=true;
        readonly CaptureSlider radiusSlider=new CaptureSlider{Minimum=0,Maximum=200,Value=0,Width=78};readonly Label radiusLabel=new CaptureLabel{Text="圆角 0",Width=64,Height=34,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent};CaptureIconButton transparentText,solidText;
        readonly HashSet<Control> hiddenOptions=new HashSet<Control>();
        CaptureIconButton pointMode,cloudMode,counterMode,circleNumber,leaderNumber,autoWidth;Tool annotationTool=Tool.Callout;
        CaptureIconButton outlineArrow,rawPen,smoothPen,freeArrow;
        CaptureIconButton mosaicBlocks,mosaicBlur,mosaicDots;
        readonly CaptureSlider labelWidth=new CaptureSlider{Minimum=60,Maximum=800,Value=320,Width=105};readonly Label widthValue=new CaptureLabel{Width=78,Height=34,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleCenter};readonly Panel noteDivider=new Panel{Width=1,Height=23,BackColor=CaptureLook.Line,Margin=new Padding(4,6,4,3)};
        int rangeHandle=-1;Point dragStart;Rectangle originalRange;
        Bitmap pendingOcr;public bool ScrollRequested{get;private set;}
        internal Action<Bitmap> CopyOutput=ShotStack.CopyImage;
        internal Action<string> CopyColorText=Clipboard.SetText;
        internal Action<string> CommandError;
        internal Action<AnnotationDocument> PinOutput=delegate(AnnotationDocument doc){PinWindow.Open(doc);};
        public CaptureOverlay(Bitmap ownedFrozen,Rectangle desktopBounds,Rectangle selectedScreen,Rectangle[] screenWorkAreas,ToolbarSettings configuration=null,int selectionDimPercent=SelectionAppearance.DefaultDimPercent,Preferences capturePreferences=null)
        {
            if(ownedFrozen==null)throw new ArgumentNullException("ownedFrozen");if(ownedFrozen.Size!=desktopBounds.Size)throw new ArgumentException("冻结图像尺寸不匹配。");
            Rectangle selection=CaptureOverlayGeometry.LocalSelection(selectedScreen,desktopBounds);desktop=desktopBounds;
            toolbarSettings=ToolbarSettings.Safe(configuration);
            dimPercent=SelectionAppearance.Percent(selectionDimPercent);
            var appearance=capturePreferences??new Preferences();completionPreferences=appearance;borderWidth=SelectionAppearance.BorderWidth(appearance.SelectionBorderWidth);borderColor=SelectionAppearance.BorderColor(appearance.SelectionBorderColor);preferToolbarTop=appearance.ToolbarPlacement=="top";
            guidesEnabled=appearance.ShowCaptureGuides;
            workAreas=screenWorkAreas==null?new[]{new Rectangle(Point.Empty,desktop.Size)}:Array.ConvertAll(screenWorkAreas,delegate(Rectangle r){r.Offset(-desktop.Left,-desktop.Top);return Rectangle.Intersect(r,new Rectangle(Point.Empty,desktop.Size));});
            Text="ScreenCraft · 原位截图标注";AutoScaleMode=AutoScaleMode.None;Font=new Font("Microsoft YaHei UI",9);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=desktop;TopMost=true;ShowInTaskbar=false;KeyPreview=true;DoubleBuffered=true;
            canvas=new Canvas(ownedFrozen){InPlaceViewport=true,FullCaptureSurface=true,CaptureDim=dimPercent,Bg=new Settings(),Bounds=ClientRectangle,Ui=1};canvas.SetCaptureSelection(selection,false);canvas.SetColor(AnnotationColors.Default);canvas.DefaultStyle.FillColor=Color.FromArgb(60,canvas.Color);
            inspectorPanel.Visible=false;options.Controls.Add(propertyTitle);
            Controls.Add(canvas);canvas.Controls.Add(bar);canvas.Controls.Add(optionsBar);canvas.Controls.Add(inspectorPanel);bar.Controls.Add(tools);optionsBar.Controls.Add(options);canvas.PaintCaptureChrome=PaintRangeChrome;canvas.CaptureDown=RangeDown;canvas.CaptureMove=RangeMove;canvas.CaptureUp=RangeUp;
            BuildConfigurableTools();
            Separator(tools);undo=Add(tools,"undo","撤销 Ctrl+Z",canvas.Undo);redo=Add(tools,"redo","重做 Ctrl+Y",canvas.Redo);
            Add(tools,"pin","钉在屏幕 P（滚轮等比缩放）",Pin);Add(tools,"save","保存 PNG Ctrl+S",Save);Add(tools,"ocr","本地 OCR",ReadText);Add(tools,"settings","详细标注属性",ToggleInspector);
            var more=Add(tools,"more","更多工具与导出",delegate{});more.Click+=delegate{ShowMore(more);};Separator(tools);var cancel=Add(tools,"cancel","取消 Esc",Close);cancel.IconColor=CaptureLook.Danger;Add(tools,"done","完成并复制 Enter",Copy);
            pointMode=Add(options,"dot","实心点标注",delegate{canvas.SetAnnotationMode(Tool.Callout);canvas.Focus();});counterMode=Add(options,"counter","数字点标注",delegate{canvas.SetAnnotationMode(Tool.Counter);canvas.Focus();});cloudMode=Add(options,"cloud","云线标注",delegate{canvas.SetAnnotationMode(Tool.Cloud);canvas.Focus();});options.Controls.Add(noteDivider);
            swatch=Add(options,"color","颜色",ShowColors);Separator(options);
            square=Add(options,"rect","直角矩形",delegate{Style(delegate(Shape s){s.CornerRadius=0;},delegate(AnnotationStyle d){d.Radius=0;});});
            round=Add(options,"round","圆角矩形",delegate{Style(delegate(Shape s){s.CornerRadius=12;},delegate(AnnotationStyle d){d.Radius=12;});});
            arrowThin=Add(options,"arrow","细线箭头",delegate{Style(delegate(Shape s){s.SimpleArrow=true;s.ArrowWhiteOutline=false;},delegate(AnnotationStyle d){d.SimpleArrow=true;d.ArrowWhiteOutline=false;});});arrowBold=Add(options,"arrow-solid","渐宽箭头",delegate{Style(delegate(Shape s){s.SimpleArrow=false;s.ArrowWhiteOutline=false;},delegate(AnnotationStyle d){d.SimpleArrow=false;d.ArrowWhiteOutline=false;});});
            outlineArrow=Add(options,"arrow-outline","白边醒目箭头",delegate{Style(delegate(Shape s){s.SimpleArrow=false;s.ArrowWhiteOutline=true;s.Width=Math.Max(6,s.Width);},delegate(AnnotationStyle d){d.SimpleArrow=false;d.ArrowWhiteOutline=true;d.Width=Math.Max(6,d.Width);});});
            rawPen=Add(options,"pen","原始画笔线条",delegate{Style(delegate(Shape s){s.SmoothPen=false;},delegate(AnnotationStyle d){d.SmoothPen=false;});});smoothPen=Add(options,"pen-smooth","平滑画笔线条",delegate{Style(delegate(Shape s){s.SmoothPen=true;},delegate(AnnotationStyle d){d.SmoothPen=true;});});freeArrow=Add(options,"pen-arrow","自由曲线箭头",delegate{bool value=!(canvas.SelectedShape==null?canvas.DefaultStyle.FreehandArrow:canvas.SelectedShape.FreehandArrow);Style(delegate(Shape s){s.FreehandArrow=value;},delegate(AnnotationStyle d){d.FreehandArrow=value;});});
            mosaicBlocks=Add(options,"pixelate","方块马赛克",delegate{Style(delegate(Shape s){s.RedactionMode=0;},delegate(AnnotationStyle d){d.RedactionMode=0;});});mosaicBlur=Add(options,"blur","柔化模糊",delegate{Style(delegate(Shape s){s.RedactionMode=1;},delegate(AnnotationStyle d){d.RedactionMode=1;});});mosaicDots=Add(options,"dots","彩点遮盖",delegate{Style(delegate(Shape s){s.RedactionMode=2;},delegate(AnnotationStyle d){d.RedactionMode=2;});});
            emptyFill=Add(options,"no-fill","无填充",delegate{canvas.SetShapeFill(false);});
            filled=Add(options,"fill","半透明填充",delegate{canvas.SetShapeFill(true);});
            solid=Add(options,"line","实线",delegate{Style(delegate(Shape s){s.Dash=DashStyle.Solid;},delegate(AnnotationStyle d){d.Dash=DashStyle.Solid;});});
            dash=Add(options,"dash","虚线（其他线型见详细属性）",delegate{Style(delegate(Shape s){s.Dash=DashStyle.Dash;},delegate(AnnotationStyle d){d.Dash=DashStyle.Dash;});});
            transparentText=Add(options,"no-fill","透明底文字",delegate{canvas.ApplyTextPreset(false);});solidText=Add(options,"fill","色底白字",delegate{canvas.ApplyTextPreset(true);});
            circleNumber=Add(options,"counter","圆形序号",delegate{canvas.SetCounterLeader(false);});leaderNumber=Add(options,"counter-leader","引线序号",delegate{canvas.SetCounterLeader(true);});
            labelWidth.AccessibleName="文本宽度";tips.SetToolTip(labelWidth,"拖动控制文本宽度，自动换行；选中后的左右角点也可调整宽度。");labelWidth.ValueChanged+=delegate{if(!syncing)canvas.SetNoteWidth(labelWidth.Value);};options.Controls.Add(widthValue);options.Controls.Add(labelWidth);autoWidth=Add(options,"grow","随输入自动扩展（到宽度上限换行）",delegate{canvas.SetNoteAutoWidth(!(canvas.SelectedShape==null?canvas.DefaultStyle.NoteAutoWidth:canvas.SelectedShape.NoteAutoWidth));});
            radiusSlider.AccessibleName="圆角半径快捷调整";tips.SetToolTip(radiusSlider,"拖动或滚轮调整圆角半径；详细属性可输入精确值。");options.Controls.Add(radiusLabel);options.Controls.Add(radiusSlider);radiusSlider.ValueChanged+=delegate{if(!syncing){if(canvas.ActiveKind==Tool.Crop)canvas.SetOutputCornerRadius(radiusSlider.Value);else Style(delegate(Shape s){s.CornerRadius=radiusSlider.Value;},delegate(AnnotationStyle d){d.Radius=radiusSlider.Value;});}};
            Separator(options);stroke.AccessibleName="线宽或字号";tips.SetToolTip(stroke,"拖动调整线宽；文字和序号工具调整字号。方向键可细调。详细属性支持精确值。");options.Controls.Add(strokeValue);options.Controls.Add(stroke);
            stroke.ValueChanged+=delegate{if(syncing)return;if(canvas.ActiveKind==Tool.Pixelate)Style(delegate(Shape s){s.MosaicBlock=stroke.Value;},delegate(AnnotationStyle d){d.MosaicBlock=stroke.Value;});else if(canvas.ActiveKind==Tool.Spotlight)Style(s=>s.EffectOpacity=stroke.Value,d=>d.EffectOpacity=stroke.Value);else if(canvas.ActiveKind==Tool.Text||canvas.ActiveKind==Tool.Watermark||canvas.ActiveHasNote)canvas.SetTextSize(stroke.Value);else Style(delegate(Shape s){s.Width=stroke.Value;},delegate(AnnotationStyle d){d.Width=stroke.Value;});};
            BuildAnnotationControls();
            canvas.MouseCaptureChanged+=delegate{if(!canvas.Capture&&rangeHandle!=-1)CancelRangeDrag();};
            foreach(Control control in options.Controls){
                var option=control as CaptureIconButton;
                if(option!=null){option.Size=new Size(29,32);option.Secondary=true;}
                if(control is CaptureSlider)control.Height=32;
            }
            RememberStripSizes(tools);
            RememberStripSizes(options);
            Tool initial;if(Enum.TryParse<Tool>(appearance.InitialTool,out initial)&&Enum.IsDefined(typeof(Tool),initial))canvas.SetTool(initial);
            canvas.StateChanged+=delegate{RefreshState();};Shown+=delegate{canvas.Focus();};RefreshState();
            canvas.MouseWheel+=delegate(object sender,MouseEventArgs e){if(!canvas.Typing&&!canvas.Busy&&e.Delta!=0)stroke.Value+=Math.Sign(e.Delta);};
        }
        static FlowLayoutPanel Flow(){return new CaptureFlow{FlowDirection=FlowDirection.LeftToRight,WrapContents=true,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};}
        static void Separator(Control parent){parent.Controls.Add(new Panel{Width=1,Height=23,BackColor=CaptureLook.Line,Margin=new Padding(4,6,4,3)});}
        internal void ExecuteCommand(Action action){try{action();}catch(Exception ex){string message="操作失败："+ex.Message;if(CommandError!=null)CommandError(message);else MessageBox.Show(this,message,"ScreenCraft");}}
        CaptureIconButton Add(Control parent,string glyph,string name,Action action){var button=new CaptureIconButton{Glyph=glyph,AccessibleName=name};tips.SetToolTip(button,name);button.Click+=delegate{ExecuteCommand(action);};parent.Controls.Add(button);return button;}
        void AddMenu(ContextMenuStrip menu,string name,Action action){menu.Items.Add(new CraftMenuItem(name,MenuGlyph(name),delegate{ExecuteCommand(action);}));}
        void Style(Action<Shape> selected,Action<AnnotationStyle> defaults){if(canvas.SelectedShape!=null)canvas.EditSelected(selected);else canvas.ChangeDefaultStyle(defaults);}
        void OptionVisible(Control control,bool visible){control.Visible=visible;if(visible)hiddenOptions.Remove(control);else hiddenOptions.Add(control);}
        void RefreshState()
        {
            if(refreshing||IsDisposed)return;refreshing=true;syncing=true;
            try{RefreshContextualControls();undo.Enabled=canvas.CanUndo;redo.Enabled=canvas.CanRedo;foreach(Control control in options.Controls)control.Invalidate();LayoutBars();optionsBar.Visible=optionsShown&&rangeHandle==-1;}
            finally{syncing=false;refreshing=false;}
        }
        void LayoutBars()
        {
            var range=Rectangle.Round(canvas.Crop);
            var area=CaptureOverlayGeometry.WorkArea(range,workAreas,ClientRectangle);
            ApplyMonitorScale(area);
            canvas.Bounds=ClientRectangle;
            int padding=Px(6),gap=Px(6);
            int available=Math.Max(1,area.Width-Px(16));
            Size mainSize=LayoutStrip(tools,available,padding);
            Size optionSize=LayoutStrip(options,available,padding);
            int height=mainSize.Height+(optionsShown?gap+optionSize.Height:0);
            var stack=CaptureOverlayGeometry.PlaceToolbar(range,new Size(Math.Max(mainSize.Width,optionSize.Width),height),area,preferToolbarTop);
            bar.Bounds=new Rectangle(stack.Location,mainSize);
            optionsBar.Bounds=new Rectangle(stack.X,bar.Bottom+gap,optionSize.Width,optionSize.Height);
            UpdateRangeChrome(range,area,bar.Bounds,optionsShown?optionsBar.Bounds:Rectangle.Empty);
            int iw=Math.Min(Px(280),Math.Max(1,area.Width-Px(16))),ih=Math.Min(Px(520),Math.Max(1,area.Height-Px(16)));
            int ix=range.Right+Px(12);
            if(ix+iw>area.Right)ix=range.Left-iw-Px(12);
            inspectorPanel.Bounds=new Rectangle(Math.Max(area.Left,Math.Min(area.Right-iw,ix)),Math.Max(area.Top,Math.Min(area.Bottom-ih,range.Top)),iw,ih);
            bar.UpdateMaterial(canvas);
            if(optionsShown)optionsBar.UpdateMaterial(canvas);
            if(inspectorOpen)inspectorPanel.BringToFront();
            canvas.Invalidate();
        }
        void UpdateRangeChrome(Rectangle range,Rectangle area,Rectangle main,Rectangle properties)
        {
            dimensions.Text=range.Width+" × "+range.Height;
            dimensions.Bounds=CaptureOverlayGeometry.PlaceDimensions(range,new Size(Px(104),Px(23)),main,properties,area);
            var points=CaptureOverlayGeometry.Handles(range);
            int radius=Px(4);
            for(int i=0;i<handles.Length;i++)handles[i]=new Rectangle(points[i].X-radius,points[i].Y-radius,2*radius+1,2*radius+1);
        }
        void ToggleInspector(){if(inspector==null){inspector=new AnnotationInspector(canvas){Dock=DockStyle.Fill};inspectorPanel.Controls.Add(inspector);}inspectorOpen=!inspectorOpen;inspectorPanel.Visible=inspectorOpen;LayoutBars();}
        void PaintRangeChrome(Graphics g){if(guideVisible)SelectionAppearance.PaintCrosshair(g,guideAt,ClientRectangle,borderColor);SelectionAppearance.PaintFrame(g,Rectangle.Round(canvas.Crop),borderWidth,borderColor);foreach(var r in handles)SelectionAppearance.PaintHandle(g,r,borderColor);PreferencesLook.Round(g,dimensions.Bounds,dimensions.BackColor,Px(5),null);PreferencesLook.Text(g,dimensions.Text,dimensions.Font,dimensions.Bounds,Color.White);}
        bool RangeDown(MouseEventArgs e){if(e.Button==MouseButtons.Right&&!Rectangle.Round(canvas.Crop).Contains(e.Location)){Close();return true;}if(e.Button!=MouseButtons.Left)return false;for(int i=0;i<handles.Length;i++)if(Rectangle.Inflate(handles[i],3,3).Contains(e.Location)){BeginRangeDrag(i,e.Location);canvas.Capture=true;return true;}if(dimensions.Bounds.Contains(e.Location)){BeginRangeDrag(8,e.Location);canvas.Capture=true;return true;}if(!Rectangle.Round(canvas.Crop).Contains(e.Location)){BeginRangeDrag(-2,e.Location);canvas.Capture=true;return true;}return false;}
        bool RangeMove(MouseEventArgs e){if(rangeHandle!=-1){UpdateRangeDrag(e.Location);return true;}if(canvas.Busy){SetGuide(e.Location,false);return false;}for(int i=0;i<handles.Length;i++)if(Rectangle.Inflate(handles[i],3,3).Contains(e.Location)){SetGuide(e.Location,true);canvas.Cursor=CursorFor(i);return true;}if(dimensions.Bounds.Contains(e.Location)){SetGuide(e.Location,true);canvas.Cursor=Cursors.SizeAll;return true;}SetGuide(e.Location,canvas.Tool==Tool.Crop);return false;}
        bool RangeUp(MouseEventArgs e){if(rangeHandle==-1)return false;if(e.Button==MouseButtons.Left){UpdateRangeDrag(e.Location);EndRangeDrag();canvas.Capture=false;}return true;}
        void ApplyAnnotationColor(Color color){canvas.SetColor(color);}
        internal void ApplySampledColor(Color color){ApplyAnnotationColor(color);var host=ShotStack.Running;var preferences=host==null?null:host.ColorPreferences;string value=ColorValues.Format(color,preferences==null?"RGB":preferences.ColorFormat,preferences==null||preferences.ColorUpper);try{CopyColorText(value);tips.SetToolTip(swatch,"已复制颜色 "+value);}catch(System.Runtime.InteropServices.ExternalException){tips.SetToolTip(swatch,"颜色 "+value+"；剪贴板忙，请重新取色复制。");}}
        void ShowMore(Control anchor){PrepareMoreMenu().Show(anchor,new Point(0,anchor.Height));}
        void Copy(){canvas.CommitText();using(var image=canvas.Render())CaptureCompletion.Deliver(image,completionPreferences,OutputRoot,CopyOutput);Completed=true;Close();}
        void Pin(){canvas.CommitText();var doc=canvas.ExportDocument();try{if(completionPreferences.HistoryEnabled)using(var image=doc.Render())new CaptureHistory(OutputRoot).Record(image,completionPreferences.HistoryLimit);PinOutput(doc);}catch{doc.Dispose();throw;}Completed=true;Close();}
        void Save(){canvas.CommitText();using(var dialog=new SaveFileDialog{Filter="PNG 图片|*.png",FileName="截图.png"})if(dialog.ShowDialog(this)==DialogResult.OK)using(var image=canvas.Render())ImageFiles.SavePng(image,dialog.FileName);}
        void ReadText(){canvas.CommitText();pendingOcr=canvas.Render();Completed=true;Close();}
        internal Bitmap TakeOcrImage(){var image=pendingOcr;pendingOcr=null;return image;}
        internal Canvas AnnotationCanvas{get{return canvas;}}internal Rectangle ToolbarBounds{get{return bar.Bounds;}}internal Rectangle OptionsBounds{get{return optionsBar.Bounds;}}
        internal bool IsOptionShown(Control control){return !hiddenOptions.Contains(control);}
        // Offline evidence: synthetic source + actual production canvas/bar renderers, never a desktop screenshot.
        internal Bitmap RenderSyntheticPreview(){var image=new Bitmap(Width,Height);using(var g=Graphics.FromImage(image)){CaptureOverlayGeometry.PaintSurround(g,canvas.Img,Rectangle.Round(canvas.Crop),dimPercent);using(var rendered=canvas.Render())g.DrawImageUnscaled(rendered,(int)canvas.Crop.X,(int)canvas.Crop.Y);SelectionAppearance.PaintFrame(g,Rectangle.Round(canvas.Crop),borderWidth,borderColor);foreach(var h in handles)SelectionAppearance.PaintHandle(g,h,borderColor);using(var fill=new SolidBrush(dimensions.BackColor))g.FillRectangle(fill,dimensions.Bounds);TextRenderer.DrawText(g,dimensions.Text,dimensions.Font,dimensions.Bounds,dimensions.ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);PaintBarPreview(g,bar,tools,null);if(optionsShown){PaintBarPreview(g,optionsBar,options,hiddenOptions);}using(var font=new Font("Segoe UI",9))g.DrawString("OFFSCREEN SYNTHETIC FIXTURE · NOT A DESKTOP SCREENSHOT",font,Brushes.White,12,Height-24);}return image;}
        static void PaintBarPreview(Graphics g,CaptureBar surface,FlowLayoutPanel children,HashSet<Control> hidden){surface.PaintSurface(g,surface.Bounds);var origin=new Point(surface.Left+children.Left,surface.Top+children.Top);foreach(Control child in children.Controls){if(hidden!=null&&hidden.Contains(child))continue;var bounds=new Rectangle(origin.X+child.Left,origin.Y+child.Top,child.Width,child.Height);var button=child as CaptureIconButton;var slider=child as CaptureSlider;if(button!=null)button.PaintButton(g,bounds);else if(slider!=null)slider.PaintPreview(g,bounds.Location);else if(child is Label)PreferencesLook.Text(g,child.Text,child.Font,bounds,child.ForeColor);else using(var brush=new SolidBrush(child.BackColor))g.FillRectangle(brush,bounds);}}
        internal void BeginRangeDrag(int handle,Point at)
        {
            canvas.CommitText();
            originalRange=Rectangle.Round(canvas.Crop);
            rangeHandle=handle;
            dragStart=at;
            bar.Hide();
            optionsBar.Hide();
            inspectorPanel.Hide();
            SetGuide(at,true);
            canvas.Invalidate();
        }
        internal void UpdateRangeDrag(Point at)
        {
            if(rangeHandle==-1)return;
            SetGuide(at,true);
            if(rangeHandle==-2&&at==dragStart&&canvas.Crop==originalRange)return;
            var delta=new Point(at.X-dragStart.X,at.Y-dragStart.Y);
            var range=rangeHandle==-2
                ? Rectangle.FromLTRB(Math.Min(dragStart.X,at.X),Math.Min(dragStart.Y,at.Y),Math.Max(dragStart.X,at.X)+1,Math.Max(dragStart.Y,at.Y)+1)
                : CaptureOverlayGeometry.DragSelection(originalRange,rangeHandle,delta,ClientRectangle);
            range=Rectangle.Intersect(range,ClientRectangle);
            if(range.Width<1||range.Height<1||range==canvas.Crop)return;
            // Preview on the fixed canvas. Moving native child windows copies their pixels on Windows.
            canvas.Crop=range;
            var area=CaptureOverlayGeometry.WorkArea(range,workAreas,ClientRectangle);
            UpdateRangeChrome(range,area,Rectangle.Empty,Rectangle.Empty);
            canvas.Invalidate();
        }
        internal void EndRangeDrag()
        {
            if(rangeHandle==-1)return;
            var final=Rectangle.Round(canvas.Crop);
            rangeHandle=-1;
            SetGuide(guideAt,false);
            canvas.Crop=originalRange;
            if(final==originalRange)RefreshState();
            else canvas.SetCaptureSelection(final,true);
            bar.Show();
            optionsBar.Visible=optionsShown;
            inspectorPanel.Visible=inspectorOpen;
            canvas.Invalidate(true);
        }
        internal void CancelRangeDrag()
        {
            if(rangeHandle==-1)return;
            canvas.Crop=originalRange;
            EndRangeDrag();
        }
        static Cursor CursorFor(int handle){return handle==0||handle==4?Cursors.SizeNWSE:handle==2||handle==6?Cursors.SizeNESW:handle==1||handle==5?Cursors.SizeNS:Cursors.SizeWE;}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Right){Close();return;}if(e.Button==MouseButtons.Left){BeginRangeDrag(-2,e.Location);Capture=true;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(rangeHandle!=-1)UpdateRangeDrag(e.Location);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left&&rangeHandle!=-1){UpdateRangeDrag(e.Location);EndRangeDrag();Capture=false;canvas.Focus();}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&rangeHandle!=-1)CancelRangeDrag();}
        protected override void OnPaint(PaintEventArgs e){canvas.PaintCaptureBackdrop(e.Graphics);base.OnPaint(e);}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.SetWindowDisplayAffinity(Handle,0x11);}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys)
        {
            if(rangeHandle!=-1){
                if(keys==Keys.Escape){CancelRangeDrag();canvas.Capture=false;Capture=false;}
                return true;
            }
            if(canvas.Typing||(inspector!=null&&inspector.ContainsFocus))return base.ProcessCmdKey(ref msg,keys);
            if(radiusSlider.Focused&&((keys&Keys.KeyCode)==Keys.Left||(keys&Keys.KeyCode)==Keys.Right)){radiusSlider.Value+=((keys&Keys.KeyCode)==Keys.Right?1:-1)*((keys&Keys.Shift)!=0?10:1);return true;}
            if(labelWidth.Focused&&((keys&Keys.KeyCode)==Keys.Left||(keys&Keys.KeyCode)==Keys.Right)){labelWidth.Value+=((keys&Keys.KeyCode)==Keys.Right?1:-1)*((keys&Keys.Shift)!=0?20:5);return true;}
            if(stroke.Focused&&((keys&Keys.KeyCode)==Keys.Left||(keys&Keys.KeyCode)==Keys.Right)){stroke.Value+=((keys&Keys.KeyCode)==Keys.Right?1:-1)*((keys&Keys.Shift)!=0?10:1);return true;}
            if(keys==Keys.Escape){if(canvas.Busy){canvas.CancelGesture();return true;}Close();return true;}
            if(keys==(Keys.Control|Keys.Z)){canvas.Undo();return true;}if(keys==(Keys.Control|Keys.Y)){canvas.Redo();return true;}if(keys==(Keys.Control|Keys.Shift|Keys.Enter)){ExecuteCommand(CopyAndContinue);return true;}if(keys==Keys.Enter||keys==(Keys.Control|Keys.C)){ExecuteCommand(Copy);return true;}if(keys==Keys.P){ExecuteCommand(Pin);return true;}if(keys==(Keys.Control|Keys.S)){ExecuteCommand(Save);return true;}if(keys==(Keys.Control|Keys.A)){canvas.SelectAll();return true;}if(keys==Keys.Delete){canvas.DeleteSelected();return true;}if(keys==(Keys.Control|Keys.D)){canvas.DuplicateSelected();return true;}
            if(keys==Keys.C){canvas.BeginColorPick(ApplySampledColor);canvas.Focus();return true;}
            string command=toolbarSettings.FindAction(keys);if(command!=null){ExecuteCommand(delegate{SelectToolbarTool(command);});return true;}
            Keys key=keys&Keys.KeyCode;if(key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down){int step=(keys&Keys.Shift)!=0?10:1;int dx=key==Keys.Left?-step:key==Keys.Right?step:0,dy=key==Keys.Up?-step:key==Keys.Down?step:0;if(canvas.HasSelection)canvas.Nudge(dx,dy);else{var adjusted=CaptureKeyboard.AdjustRange(Rectangle.Round(canvas.Crop),keys,ClientRectangle);canvas.SetCaptureSelection(adjusted,true);SetGuide(new Point(adjusted.Right,adjusted.Bottom),true);}return true;}return base.ProcessCmdKey(ref msg,keys);
        }
        protected override void Dispose(bool disposing){if(disposing){DisposePopups();if(pendingBoard!=null){pendingBoard.Dispose();pendingBoard=null;}tips.Dispose();if(pendingOcr!=null){pendingOcr.Dispose();pendingOcr=null;}}base.Dispose(disposing);}
    }
}
