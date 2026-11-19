using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace ScreenCraft
{
    public sealed class ToolbarSettings
    {
        public string[] Order={"Rect","Ellipse","Arrow","Pen","PenPath","Text","Callout","Pixelate","Highlight","Redact","Spotlight","Watermark","Background","Crop","Counter","Cloud"};
        public string[] Hidden={"Counter","Cloud"};
        public Dictionary<string,string> Keys=new Dictionary<string,string>{{"Rect","R"},{"Ellipse","E"},{"Line","L"},{"Arrow","A"},{"Pen","B"},{"PenPath","G"},{"Text","T"},{"Counter","N"},{"Cloud","W"},{"Pixelate","M"},{"Highlight","H"},{"Redact","X"},{"Callout","Q"},{"Spotlight","F"},{"Watermark",""},{"Background",""},{"Crop",""}};
        public static readonly string[] Known={"Rect","Ellipse","Arrow","Pen","PenPath","Text","Callout","Counter","Cloud","Pixelate","Highlight","Redact","Spotlight","Watermark","Background","Crop"};
        static readonly string[] ShortcutIds=Known.Concat(new[]{"Line"}).ToArray();
        public FillPatternSettings DefaultFillPattern;
        public ToolbarSettings Clone(){return new ToolbarSettings{DefaultFillPattern=DefaultFillPattern,Order=(string[])Order.Clone(),Hidden=(string[])Hidden.Clone(),Keys=new Dictionary<string,string>(Keys)};}
        public string Validate()
        {
            if(Order==null||Hidden==null||Keys==null)return "工具栏配置无效，请恢复默认。";
            if(Order.Length==11&&Order.Distinct().Count()==11&&Order.All(x=>ShortcutIds.Contains(x))&&Keys.Count==11&&Order.All(Keys.ContainsKey)){
                var migrated=Order.ToList();int at=migrated.FindIndex(x=>x=="Counter"||x=="Cloud");migrated.Insert(at<0?6:at,"Callout");foreach(var id in ShortcutIds.Where(key=>key!="PenPath"))if(!migrated.Contains(id))migrated.Add(id);Order=migrated.ToArray();Hidden=Hidden.Concat(new[]{"Counter","Cloud"}).Distinct().ToArray();foreach(var id in ShortcutIds.Where(key=>key!="PenPath"))if(!Keys.ContainsKey(id))Keys[id]="";
            }
            if(!DefaultFillPattern.IsValid)return "默认填充图案配置无效。";
            if(!Order.Contains("PenPath")&&Order.Contains("Pen")){
                var order=Order.ToList();order.Insert(order.IndexOf("Pen")+1,"PenPath");Order=order.ToArray();
                if(!Keys.ContainsKey("PenPath"))Keys["PenPath"]=Keys.Values.Contains("G")?"":"G";
            }
            if(Order.Length==ShortcutIds.Length&&Order.Distinct().Count()==ShortcutIds.Length&&Order.All(ShortcutIds.Contains))MergeLineEntries();
            if(Keys.Count!=ShortcutIds.Length||Keys.Keys.Any(x=>!ShortcutIds.Contains(x))||Order.Length!=Known.Length||Order.Distinct().Count()!=Known.Length||Order.Any(x=>!Known.Contains(x))||Hidden.Length>Known.Length||Hidden.Any(x=>!Known.Contains(x)))return "工具栏配置无效，请恢复默认。";
            var used=new HashSet<string>();foreach(var id in ShortcutIds){string key;if(!Keys.TryGetValue(id,out key)||key==null)return "工具快捷键配置不完整。";if(key=="")continue;if(key.Length!=1||key[0]<'A'||key[0]>'Z'||key=="P"||key=="C")return "工具键只支持单个英文字母，P 用于贴图、C 用于取色；留空可禁用。";if(!used.Add(key))return "工具快捷键重复："+key;}return null;
        }
        void MergeLineEntries()
        {
            int line=Array.IndexOf(Order,"Line"),arrow=Array.IndexOf(Order,"Arrow");
            bool lineHidden=Hidden.Contains("Line"),arrowHidden=Hidden.Contains("Arrow");
            int keep=lineHidden==arrowHidden?Math.Min(line,arrow):lineHidden?arrow:line;
            Order=Order.Where((id,index)=>index==keep||id!="Line"&&id!="Arrow").Select(id=>id=="Line"?"Arrow":id).ToArray();
            Hidden=Hidden.Where(id=>id!="Line"&&(id!="Arrow"||lineHidden)).ToArray();
        }
        internal string ToolLabel(string id)
        {
            var keys=id=="Arrow"?new[]{Keys[id],Keys["Line"]}:new[]{Keys[id]};
            string shortcuts=string.Join(" / ",keys.Where(key=>key.Length!=0));
            return Name(id)+(shortcuts.Length==0?"":" "+shortcuts);
        }
        public static ToolbarSettings Safe(ToolbarSettings value){return value!=null&&value.Validate()==null?value.Clone():new ToolbarSettings();}
        public void Move(int from,int to){if(from<0||from>=Order.Length||to<0||to>=Order.Length)return;var list=Order.ToList();string id=list[from];list.RemoveAt(from);list.Insert(to,id);Order=list.ToArray();}
        public Tool? Find(Keys keyData){if((keyData&System.Windows.Forms.Keys.Modifiers)!=0)return null;foreach(var pair in Keys)if(pair.Value.Length==1&&(int)keyData==pair.Value[0]){Tool tool;if(Enum.TryParse<Tool>(pair.Key,out tool))return tool;}return null;}
        public string FindAction(Keys keyData){if((keyData&System.Windows.Forms.Keys.Modifiers)!=0)return null;foreach(var pair in Keys)if(pair.Value.Length==1&&(int)keyData==pair.Value[0])return pair.Key;return null;}
        public static string Name(string id){switch(id){case "Background":return "图片美化";case "Crop":return "裁剪与导出圆角";case "Watermark":return "文字水印";case "Spotlight":return "聚光灯";case "Callout":return "点标注";case "Rect":return "矩形";case "Ellipse":return "椭圆";case "Line":return "直线";case "Arrow":return "线条";case "Pen":return "画笔";case "PenPath":return "钢笔";case "Text":return "文字";case "Counter":return "序号";case "Cloud":return "云线";case "Pixelate":return "马赛克";case "Highlight":return "荧光笔";case "Redact":return "实色遮盖";default:return id;}}
        public static string Glyph(string id){switch(id){case "Callout":return "callout";case "Crop":return "crop";case "Background":return "image";case "Watermark":return "watermark";case "Spotlight":return "spotlight";case "Rect":return "rect";case "Ellipse":return "ellipse";case "Line":return "line";case "Arrow":return "arrow";case "Pen":return "pen";case "PenPath":return "pen-path";case "Text":return "text";case "Counter":return "counter";case "Cloud":return "cloud";case "Pixelate":return "pixelate";case "Highlight":return "highlight";default:return "redact";}}
    }
    internal sealed class ToolbarSettingsEditor:UserControl
    {
        readonly CheckedListBox list=new CheckedListBox{CheckOnClick=true,Width=280,Height=390,IntegralHeight=false,BorderStyle=BorderStyle.FixedSingle,AllowDrop=true,AccessibleName="工具顺序与显示"};
        readonly HotkeyInput key=new HotkeyInput{ToolShortcutMode=true,Width=200,AccessibleName="工具单键快捷键"};
        readonly HotkeyInput noArrowKey=new HotkeyInput{ToolShortcutMode=true,Width=200,BorderStyle=BorderStyle.None,AccessibleName="无箭头快捷键"};
        readonly Label keyCaption=new Label{AutoSize=true},noArrowCaption=new Label{AutoSize=true,Text="无箭头快捷键"};
        readonly PreferencesField noArrowField;
        readonly Label status=new Label{AutoSize=true,MaximumSize=new System.Drawing.Size(230,0)};ToolbarSettings working;bool syncing;int dragIndex=-1;System.Drawing.Point dragAt;
        public ToolbarSettings Export(){SaveChecks();return working.Clone();}
        public void Reset(){working=new ToolbarSettings();Reload(0);}
        internal void LoadConfiguration(ToolbarSettings settings){working=ToolbarSettings.Safe(settings);Reload(0);}
        public ToolbarSettingsEditor(ToolbarSettings initial)
        {
            working=ToolbarSettings.Safe(initial);Font=new System.Drawing.Font("Microsoft YaHei UI",9);BackColor=PreferencesLook.Background;Size=new System.Drawing.Size(650,375);list.Height=310;list.BorderStyle=BorderStyle.None;list.BackColor=Color.White;key.BorderStyle=BorderStyle.None;status.AutoSize=false;status.Size=new System.Drawing.Size(222,40);status.ForeColor=PreferencesLook.Muted;
            var layout=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown,WrapContents=false};layout.Controls.Add(new Label{Text="拖动调整顺序；取消勾选后仍可从“更多”或工具键打开。",AutoSize=true});var row=new FlowLayoutPanel{AutoSize=true,WrapContents=false};row.Controls.Add(list);var side=new FlowLayoutPanel{Width=245,Height=320,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(10,0,0,0)};row.Controls.Add(side);layout.Controls.Add(row);
            side.Controls.Add(keyCaption);side.Controls.Add(new PreferencesField(key,222));
            noArrowField=new PreferencesField(noArrowKey,222);side.Controls.Add(noArrowCaption);side.Controls.Add(noArrowField);side.Controls.Add(status);
            key.Feedback+=delegate(string message){status.Text=message;};noArrowKey.Feedback+=delegate(string message){status.Text=message;};
            key.TextChanged+=delegate{if(syncing||list.SelectedIndex<0)return;working.Keys[working.Order[list.SelectedIndex]]=key.Text;};
            noArrowKey.TextChanged+=delegate{if(!syncing)working.Keys["Line"]=noArrowKey.Text;};
            var up=new PreferencesButton{Text="上移",Width=106};var down=new PreferencesButton{Text="下移",Width=106};up.Click+=delegate{MoveSelection(-1);};down.Click+=delegate{MoveSelection(1);};var moves=new FlowLayoutPanel{Width=228,Height=38,WrapContents=false};moves.Controls.Add(up);moves.Controls.Add(down);side.Controls.Add(moves);var defaults=new PreferencesButton{Text="恢复默认工具栏",Width=222};defaults.Click+=delegate{Reset();};side.Controls.Add(defaults);
            side.Controls.Add(new Label{Text="P 贴图 · C 取色\n按 Delete 清除工具键\n点击“保存并应用”生效",AutoSize=true,ForeColor=PreferencesLook.Muted});Controls.Add(layout);
            list.SelectedIndexChanged+=delegate{if(!syncing)LoadShortcut();};
            list.MouseDown+=delegate(object sender,MouseEventArgs e){dragIndex=list.IndexFromPoint(e.Location);dragAt=e.Location;};list.MouseMove+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left&&dragIndex>=0&&(Math.Abs(e.X-dragAt.X)>SystemInformation.DragSize.Width||Math.Abs(e.Y-dragAt.Y)>SystemInformation.DragSize.Height)){string id=working.Order[dragIndex];dragIndex=-1;list.DoDragDrop(id,DragDropEffects.Move);}};list.DragEnter+=delegate(object sender,DragEventArgs e){if(e.Data.GetDataPresent(typeof(string)))e.Effect=DragDropEffects.Move;};list.DragDrop+=delegate(object sender,DragEventArgs e){string id=e.Data.GetData(typeof(string)) as string;int from=Array.IndexOf(working.Order,id),to=list.IndexFromPoint(list.PointToClient(new System.Drawing.Point(e.X,e.Y)));if(from>=0&&to>=0){SaveChecks();working.Move(from,to);Reload(to);}};Reload(0);
        }
        void SaveChecks(){working.Hidden=Enumerable.Range(0,list.Items.Count).Where(i=>!list.GetItemChecked(i)).Select(i=>working.Order[i]).ToArray();}
        void Reload(int selected){syncing=true;try{list.Items.Clear();foreach(string id in working.Order)list.Items.Add(ToolbarSettings.Name(id),!working.Hidden.Contains(id));list.SelectedIndex=Math.Max(0,Math.Min(list.Items.Count-1,selected));}finally{syncing=false;}LoadShortcut();}
        void LoadShortcut()
        {
            if(list.SelectedIndex<0)return;
            syncing=true;
            try{
                string id=working.Order[list.SelectedIndex];bool linear=id=="Arrow";
                keyCaption.Text=key.AccessibleName=linear?"箭头快捷键":"选中工具的单键快捷键";
                key.Text=working.Keys[id];noArrowKey.Text=working.Keys["Line"];
                noArrowCaption.Visible=noArrowField.Visible=linear;
            }finally{syncing=false;}
        }
        internal void MoveSelection(int delta){int from=list.SelectedIndex,to=from+delta;if(from<0||to<0||to>=working.Order.Length)return;SaveChecks();working.Move(from,to);Reload(to);}
    }
    internal sealed class ToolbarSettingsDialog:Form
    {
        readonly ToolbarSettingsEditor editor;public ToolbarSettings Result{get;private set;}
        public ToolbarSettingsDialog(ToolbarSettings initial){Text="ScreenCraft · 工具栏";ClientSize=new System.Drawing.Size(660,440);editor=new ToolbarSettingsEditor(initial){Dock=DockStyle.Fill};Controls.Add(editor);var apply=new PreferencesButton{Text="确认工具栏",Dock=DockStyle.Bottom};apply.Click+=delegate{var result=editor.Export();if(result.Validate()!=null)return;Result=result;DialogResult=DialogResult.OK;Close();};Controls.Add(apply);}
        internal void MoveSelection(int delta){editor.MoveSelection(delta);}
    }
}
