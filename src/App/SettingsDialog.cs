using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed class SettingsDialog:Form
    {
        readonly HotkeyInput[] hotkeyFields;
        readonly Panel body=new Panel();readonly Label status=new Label();bool layingOutPage;
        readonly PreferencesTab[] tabs=new PreferencesTab[7];readonly Panel[] pages=new Panel[7];
        readonly PreferencesButton defaults=new PreferencesButton{Text="恢复默认"},apply=new PreferencesButton{Text="保存并应用",Primary=true};
        readonly Label hint=new Label();readonly ToolTip feedbackTips=new ToolTip();readonly ToolbarSettingsEditor toolbar;
        internal int SelectedPage{get;private set;}
        const string KeyHint="点击快捷键栏后按组合键；Delete 清除，Esc 恢复，Tab 切换。";
        public SettingsDialog(Preferences previous,string dataPath,Func<Preferences,string> save,string runtimeStatus=null)
        {
            Text="ScreenCraft 设置 · "+Application.ProductVersion;Icon=ShotStack.AppIcon;StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.Sizable;ControlBox=true;MinimizeBox=true;MaximizeBox=true;
            Font=new Font("Microsoft YaHei UI",9F);AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
            BackColor=PreferencesLook.Background;ForeColor=PreferencesLook.Ink;ClientSize=new Size(860,680);MinimumSize=new Size(720,520);DoubleBuffered=true;
            string[] labels={"常规","截图","快捷键","工具栏","OCR 与取色","关于","贴图"},icons={"gear","crop","keyboard","rect","ocr","info","stack"};
            body.BackColor=BackColor;Controls.Add(body);
            for(int i=0;i<tabs.Length;i++){int index=i;tabs[i]=new PreferencesTab{Text=labels[i],Glyph=icons[i],AccessibleName=labels[i],TabIndex=i+1};tabs[i].Click+=delegate{SelectPage(index);};Controls.Add(tabs[i]);pages[i]=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=BackColor};pages[i].SizeChanged+=delegate(object sender,EventArgs e){LayoutPage((Panel)sender);};body.Controls.Add(pages[i]);}
            var startup=new PreferencesSwitch{Checked=previous.RunOnStartup,AccessibleName="登录 Windows 后在后台启动（仅当前用户）"};
            var notice=new PreferencesSwitch{Checked=previous.ShowStartupNotice,AccessibleName="启动时显示悬浮提示"};
            AddHeading(0,"常规","轻点托盘图标截图；右键打开功能与设置。");
            AddGroup(0,new PreferencesRow("登录 Windows 后启动",startup,42),new PreferencesRow("启动时显示提示",notice,42));
            AddNote(0,"关闭此窗口后继续在托盘运行。双击程序不会再打开主面板。\n如需退出，请右键托盘图标，选择“退出”。");
            AddNote(0,string.IsNullOrEmpty(runtimeStatus)?"截图与文字识别在本机处理。":runtimeStatus);
            var dim=new CaptureSlider{Minimum=45,Maximum=85,Value=SelectionAppearance.Percent(previous.SelectionDimPercent),Height=30,BackColor=Color.White,AccessibleName="未选中区域压暗程度"};
            var percent=new NumericUpDown{Minimum=45,Maximum=85,Value=dim.Value,BorderStyle=BorderStyle.None,AccessibleName="压暗百分比",Width=58};
            var dimField=new Panel{Size=new Size(270,34),BackColor=Color.White};dim.Bounds=new Rectangle(0,0,176,34);var percentField=new PreferencesField(percent,76){Location=new Point(190,0)};dimField.Controls.Add(dim);dimField.Controls.Add(percentField);
            dim.ValueChanged+=delegate{percent.Value=dim.Value;};percent.ValueChanged+=delegate{dim.Value=(int)percent.Value;};
            var border=new PreferencesChoice("标准 · 2 px","加粗 · 4 px","醒目 · 6 px"){AccessibleName="选区边框粗细",SelectedIndex=previous.SelectionBorderWidth>4?2:previous.SelectionBorderWidth>2?1:0};
            var edgeColor=new PreferencesChoice("蓝色","青绿","橙色"){AccessibleName="选区边框颜色",SelectedIndex=previous.SelectionBorderColor=="green"?1:previous.SelectionBorderColor=="orange"?2:0};
            var placement=new PreferencesChoice("优先下方","优先上方"){AccessibleName="编辑工具条位置",SelectedIndex=previous.ToolbarPlacement=="top"?1:0};
            var guides=new PreferencesSwitch{Checked=previous.ShowCaptureGuides,AccessibleName="框选与调整时显示十字线"};
            var daily=new DailySettings(previous);
            AddHeading(6,"贴图","每张图片独立显示；按住 Alt 拖动可临时关闭吸附。");AddBlock(6,daily.PinGroup());AddNote(6,"普通滚轮默认缩放，Ctrl+滚轮默认调透明度。方向键微移，Shift 加速；Ctrl+R 旋转，Ctrl+H/J 翻转。\n改变外观会同步到已有贴图；初始缩放和透明度只作用于新图。");
            AddHeading(1,"截图","清楚区分选区与桌面，直接在截图位置标注。");
            AddGroup(1,new PreferencesRow("压暗程度（%）",dimField),new PreferencesRow("选区边框粗细",border),new PreferencesRow("选区边框颜色",edgeColor),new PreferencesRow("十字参考线",guides,42),new PreferencesRow("编辑工具条位置",placement));
            AddNote(1,"选区内保留原始画面亮度，压暗数值越高，选区外越暗。\n工具与属性作为整体定位，属性始终紧跟工具栏下方；空间不足时一起移至另一侧。\n边框、控制点、遮罩与工具条均不会进入保存的图片。");
            AddBlock(1,daily.CaptureGroup());AddBlock(1,daily.ExportGroup());AddNote(1,"自动保存目录默认使用系统桌面，留空也使用桌面；文件名支持 {date}、{time}、{id}。历史默认关闭，只清理自有历史副本，不删除手动保存的文件。");
            AddHeading(2,"快捷键","点击右侧快捷键栏，直接按下想设置的组合键。");
            hotkeyFields=new[]{Recorder("区域截图",previous.Capture),Recorder("本地 OCR",previous.Ocr),Recorder("贴剪贴板图片",previous.Paste),Recorder("屏幕取色",previous.Color),Recorder("框选滚动长截图",previous.Scroll),Recorder("当前屏全屏截图",previous.FullScreen),Recorder("当前窗口截图",previous.ActiveWindow),Recorder("重复上次区域",previous.RepeatRegion),Recorder("隐藏／恢复全部贴图",previous.TogglePins),Recorder("打开拼图画板",previous.Board)};
            var keyGroup=new PreferencesGroup();foreach(var input in hotkeyFields){var row=new Panel{Size=new Size(284,34),BackColor=Color.White};var field=new PreferencesField(input,222){Dock=DockStyle.Left};var clear=new PreferencesButton{Text="清除",AccessibleName="清除"+input.AccessibleName,Width=54,Height=32,Dock=DockStyle.Right,TabStop=false};clear.Click+=delegate{input.ClearShortcut();input.Focus();};row.Controls.Add(field);row.Controls.Add(clear);row.Resize+=delegate{field.Width=Math.Max(100,row.Width-62);};keyGroup.AddRow(new PreferencesRow(input.AccessibleName,row,284));input.Feedback+=delegate(string text){hint.Text=string.IsNullOrEmpty(text)?KeyHint:text;};}AddBlock(2,keyGroup);
            hint.Text=KeyHint;hint.ForeColor=PreferencesLook.Muted;hint.Height=48;AddBlock(2,hint);AddNote(2,"留空可禁用。冲突时保留原快捷键；不修改 Windows 的 PrintScreen 设置。");
            AddHeading(3,"工具栏","调整工具顺序、显示状态与截图时使用的单键快捷键。");
            toolbar=new ToolbarSettingsEditor(previous.Toolbar);AddBlock(3,toolbar);
            var language=new OcrLanguageSetting(previous.OcrLanguage);
            Shown+=async delegate{await language.LoadAsync();};
            var format=new PreferencesChoice(ColorFormatSettings.Labels){SelectedIndex=ColorFormatSettings.Index(previous.ColorFormat)};
            var uppercase=new PreferencesSwitch{Checked=previous.ColorUpper,AccessibleName="HEX 使用大写字母"};
            AddHeading(4,"OCR 与取色","按你的使用习惯设置文字识别和颜色输出。");
            var upperRow=new PreferencesRow("HEX 使用大写字母",uppercase,42);format.SelectionChanged+=delegate{upperRow.Enabled=format.SelectedIndex==1;};upperRow.Enabled=format.SelectedIndex==1;
            AddGroup(4,new PreferencesRow("OCR 语言",language.Choice),new PreferencesRow("取色格式",format),upperRow);
            AddBlock(4,language.Status);language.Status.TextChanged+=delegate{LayoutPage(pages[4]);};
            var colorExample=new Label{Tag="note",Height=40,ForeColor=PreferencesLook.Muted,Margin=new Padding(0,12,0,0),AccessibleName="取色输出示例"};
            Action previewColor=delegate{colorExample.Text="输出示例："+ColorValues.Format(Color.FromArgb(255,128,64),ColorFormatSettings.Codes[format.SelectedIndex],uppercase.Checked)+"\n六位十六进制与三通道数值表示同一个 RGB 颜色。";LayoutPage(pages[4]);};
            AddBlock(4,colorExample);format.SelectionChanged+=delegate{previewColor();};uppercase.CheckedChanged+=delegate{previewColor();};previewColor();
            AddHeading(5,"ScreenCraft","截图、标注，然后钉在屏幕上。");
            AddGroup(5,new PreferencesRow("版本",Value(Application.ProductVersion)),new PreferencesRow("运行权限",Value("普通用户 · 无需管理员权限")));
            AddNote(5,"配置位置\n"+dataPath+"\n\n便携版通过同目录 portable.flag 使用 Data 文件夹。");
            status.ForeColor=Color.FromArgb(181,47,56);status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;status.TextChanged+=delegate{feedbackTips.SetToolTip(status,status.Text);};Controls.Add(status);Controls.Add(defaults);Controls.Add(apply);
            defaults.Click+=delegate{var p=new Preferences();string[] keys={p.Capture,p.Ocr,p.Paste,p.Color,p.Scroll,p.FullScreen,p.ActiveWindow,p.RepeatRegion,p.TogglePins,p.Board};for(int i=0;i<keys.Length;i++)hotkeyFields[i].Text=keys[i];language.SelectedLanguage=p.OcrLanguage;format.SelectedIndex=0;uppercase.Checked=p.ColorUpper;dim.Value=p.SelectionDimPercent;border.SelectedIndex=1;edgeColor.SelectedIndex=0;placement.SelectedIndex=0;notice.Checked=p.ShowStartupNotice;guides.Checked=p.ShowCaptureGuides;toolbar.Reset();daily.Load(p);status.Text="已恢复默认值，点击保存后生效。";};
            Func<Preferences> collect=delegate{var model=toolbar.Export();return daily.Apply(new Preferences{Capture=hotkeyFields[0].Text.Trim(),Ocr=hotkeyFields[1].Text.Trim(),Paste=hotkeyFields[2].Text.Trim(),Color=hotkeyFields[3].Text.Trim(),Scroll=hotkeyFields[4].Text.Trim(),FullScreen=hotkeyFields[5].Text.Trim(),ActiveWindow=hotkeyFields[6].Text.Trim(),RepeatRegion=hotkeyFields[7].Text.Trim(),TogglePins=hotkeyFields[8].Text.Trim(),Board=hotkeyFields[9].Text.Trim(),LastCapture=previous.LastCapture,RunOnStartup=startup.Checked,ShowStartupNotice=notice.Checked,ShowCaptureGuides=guides.Checked,SelectionDimPercent=dim.Value,SelectionBorderWidth=(border.SelectedIndex+1)*2,SelectionBorderColor=new[]{"blue","green","orange"}[edgeColor.SelectedIndex],ToolbarPlacement=placement.SelectedIndex==1?"top":"bottom",Toolbar=model,OcrLanguage=language.SelectedLanguage,ColorFormat=ColorFormatSettings.Codes[format.SelectedIndex],ColorUpper=uppercase.Checked});};
            apply.Click+=delegate{try{var model=collect();var error=model.ValidateDaily();if(error==null)error=save(model);if(error==null){DialogResult=DialogResult.OK;Close();}else status.Text=error;}catch(Exception ex){status.Text="无法保存设置："+ex.Message;}};
            var transfer=new FlowLayoutPanel{Height=42,BackColor=BackColor};PreferencesButton export=new PreferencesButton{Text="导出配置",Width=118},import=new PreferencesButton{Text="导入配置",Width=118};transfer.Controls.Add(export);transfer.Controls.Add(import);AddBlock(5,transfer);
            export.Click+=delegate{using(var file=new SaveFileDialog{Filter="ScreenCraft 配置|*.json",FileName="ScreenCraft-preferences.json"})if(file.ShowDialog(this)==DialogResult.OK)try{var value=collect();var error=value.ValidateDaily();if(error!=null)throw new InvalidOperationException(error);PreferenceTransfer.Export(value,file.FileName);status.Text="配置已导出，不含此机器的自启授权。";}catch(Exception ex){status.Text=ex.Message;}};
            import.Click+=delegate{using(var file=new OpenFileDialog{Filter="ScreenCraft 配置|*.json"})if(file.ShowDialog(this)==DialogResult.OK)try{var p=PreferenceTransfer.Import(file.FileName,startup.Checked);string[] keys={p.Capture,p.Ocr,p.Paste,p.Color,p.Scroll,p.FullScreen,p.ActiveWindow,p.RepeatRegion,p.TogglePins,p.Board};for(int i=0;i<keys.Length;i++)hotkeyFields[i].Text=keys[i];language.SelectedLanguage=p.OcrLanguage;format.SelectedIndex=ColorFormatSettings.Index(p.ColorFormat);uppercase.Checked=p.ColorUpper;dim.Value=p.SelectionDimPercent;border.SelectedIndex=p.SelectionBorderWidth/2-1;edgeColor.SelectedIndex=p.SelectionBorderColor=="green"?1:p.SelectionBorderColor=="orange"?2:0;placement.SelectedIndex=p.ToolbarPlacement=="top"?1:0;notice.Checked=p.ShowStartupNotice;guides.Checked=p.ShowCaptureGuides;toolbar.LoadConfiguration(p.Toolbar);daily.Load(p);status.Text="已读取配置；保存并应用后生效，自启开关保留。";}catch(Exception ex){status.Text="导入失败："+ex.Message;}};
            SelectPage(0);PerformLayout();
        }
        static HotkeyInput Recorder(string name,string text){return new HotkeyInput{Text=text,AccessibleName=name};}
        static Label Value(string text){return new Label{Text=text,TextAlign=ContentAlignment.MiddleRight,AutoEllipsis=true,Height=30,ForeColor=PreferencesLook.Muted};}
        void AddHeading(int page,string text,string subtitle){AddBlock(page,new Label{Text=text,Height=34,Font=new Font(Font.FontFamily,15,FontStyle.Bold)});AddBlock(page,new Label{Text=subtitle,Height=40,ForeColor=PreferencesLook.Muted});}
        void AddNote(int page,string text){AddBlock(page,new Label{Text=text,Tag="note",Height=40,ForeColor=PreferencesLook.Muted,Margin=new Padding(0,16,0,0)});}
        void AddGroup(int page,params PreferencesRow[] rows){var group=new PreferencesGroup();foreach(var row in rows)group.AddRow(row);AddBlock(page,group);}
        void AddBlock(int index,Control block){var page=pages[index];page.Controls.Add(block);LayoutPage(page);}
        int U(int value){return (int)Math.Round(value*DeviceDpi/96f);}
        void LayoutPage(Panel page){if(layingOutPage)return;layingOutPage=true;try{int width=Math.Min(U(660),Math.Max(U(280),page.ClientSize.Width-U(56))),y=U(18);Point scroll=page.AutoScrollPosition;foreach(Control child in page.Controls){y+=child.Margin.Top;if((string)child.Tag=="note")child.Height=TextRenderer.MeasureText(child.Text,child.Font,new Size(width,0),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding).Height+U(6);child.Bounds=new Rectangle(Math.Max(U(16),(page.ClientSize.Width-width)/2)+scroll.X,y+scroll.Y,width,child.Height);y+=child.Height+U(8);}page.AutoScrollMinSize=new Size(0,y+U(12));}finally{layingOutPage=false;}}
        internal void SelectPage(int index){SelectedPage=index;for(int i=0;i<pages.Length;i++){pages[i].Visible=i==index;tabs[i].Selected=i==index;tabs[i].Invalidate();}pages[index].BringToFront();pages[index].AutoScrollPosition=Point.Empty;LayoutPage(pages[index]);}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(tabs[tabs.Length-1]==null)return;int width=ClientSize.Width,height=ClientSize.Height,step=Math.Min(U(100),Math.Max(1,(width-U(16))/tabs.Length)),left=(width-tabs.Length*step)/2;for(int i=0;i<tabs.Length;i++)tabs[i].Bounds=new Rectangle(left+i*step,U(16),Math.Max(1,step-U(4)),U(67));body.Bounds=new Rectangle(0,U(100),width,Math.Max(1,height-U(164)));defaults.Location=new Point(U(22),height-U(48));apply.Location=new Point(width-apply.Width-U(22),height-U(48));status.Bounds=new Rectangle(defaults.Right+U(16),height-U(54),Math.Max(1,apply.Left-defaults.Right-U(30)),U(46));}
        protected override void OnPaint(PaintEventArgs e){PreferencesLook.PaintSurface(e.Graphics,ClientSize,U(100),U(64));base.OnPaint(e);}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);Invalidate();}
        protected override void Dispose(bool disposing){if(disposing)feedbackTips.Dispose();base.Dispose(disposing);}
        internal bool RecordRegisteredShortcut(Keys keys){if(!ContainsFocus)return false;foreach(var field in hotkeyFields)if(field.Focused)return field.RecordKey(keys);return false;}
        // Hidden WM_PRINT only; does not show, activate or capture any desktop window.
        internal Bitmap RenderSyntheticPreview(int page){SelectPage(page);CreateControl();PerformLayout();var image=new Bitmap(ClientSize.Width,ClientSize.Height);using(var g=Graphics.FromImage(image))using(var font=new Font("Segoe UI",7)){PreferencesPreview.Draw(g,this,pages[page]);g.DrawString("OFFSCREEN CLIENT PREVIEW · NATIVE TITLE BAR EXCLUDED · GUI NOT RUN",font,Brushes.Gray,22,ClientSize.Height-66);}return image;}
    }
}
