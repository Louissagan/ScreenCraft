using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class DailySettings
    {
        readonly string[] tools={"Rect","Ellipse","Line","Arrow","Pen","Text","Callout","Counter","Cloud","Pixelate","Redact","Crop"};
        readonly PreferencesChoice initial=new PreferencesChoice("矩形","椭圆","直线","箭头","画笔","文字","点标注","序号","云线","马赛克","实色遮盖","调整选区"){AccessibleName="默认标注工具"};
        readonly PreferencesSwitch cursor=new PreferencesSwitch{AccessibleName="截图包含光标"},reverse=new PreferencesSwitch{AccessibleName="反向缩放滚轮"},wheel=new PreferencesSwitch{AccessibleName="普通滚轮调整透明度"},snap=new PreferencesSwitch{AccessibleName="贴图边缘吸附"},shadow=new PreferencesSwitch{AccessibleName="贴图阴影"},history=new PreferencesSwitch{AccessibleName="保留截图历史"};
        readonly NumericUpDown opacity=Number(20,100,100,"新贴图透明度"),zoom=Number(0,400,0,"新贴图缩放百分比，0 为适应屏幕"),border=Number(0,8,1,"贴图边框宽度"),radius=Number(0,60,10,"贴图圆角半径"),historyLimit=Number(10,500,100,"历史条数上限");
        readonly TextBox edgeColor=new TextBox{AccessibleName="贴图边框 HEX"},directory=new TextBox{AccessibleName="自动保存目录"},template=new TextBox{AccessibleName="保存文件名模板"};
        readonly PreferencesChoice action=new PreferencesChoice("复制并退出","复制并自动保存","仅自动保存"){AccessibleName="完成后动作"};
        internal DailySettings(Preferences previous){Load(previous);}
        static NumericUpDown Number(int min,int max,int value,string name){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,BorderStyle=BorderStyle.None,AccessibleName=name};}
        static PreferencesRow N(string label,NumericUpDown control){return new PreferencesRow(label,new PreferencesField(control,100),100);}
        internal PreferencesGroup CaptureGroup(){var group=new PreferencesGroup();group.AddRow(new PreferencesRow("默认标注工具",initial));group.AddRow(new PreferencesRow("包含系统光标",cursor,42));return group;}
        internal PreferencesGroup PinGroup(){var group=new PreferencesGroup();group.AddRow(N("新贴图透明度（%）",opacity));group.AddRow(N("缩放（%，0 为自动）",zoom));group.AddRow(new PreferencesRow("普通滚轮调透明度",wheel,42));group.AddRow(new PreferencesRow("反向缩放滚轮",reverse,42));group.AddRow(new PreferencesRow("贴图边缘吸附",snap,42));group.AddRow(new PreferencesRow("显示阴影",shadow,42));group.AddRow(N("边框宽度（px）",border));group.AddRow(new PreferencesRow("边框颜色 HEX",new PreferencesField(edgeColor,150),150));group.AddRow(N("圆角半径（px）",radius));return group;}
        internal PreferencesGroup ExportGroup(){var group=new PreferencesGroup();group.AddRow(new PreferencesRow("完成后动作",action));group.AddRow(new PreferencesRow("自动保存目录",new PreferencesField(directory,270)));group.AddRow(new PreferencesRow("文件名模板",new PreferencesField(template,270)));group.AddRow(new PreferencesRow("保留截图历史",history,42));group.AddRow(N("历史条数上限",historyLimit));return group;}
        static void Value(NumericUpDown control,int value){control.Value=Math.Max(control.Minimum,Math.Min(control.Maximum,value));}
        internal void Load(Preferences p){initial.SelectedIndex=Math.Max(0,Array.IndexOf(tools,p.InitialTool));cursor.Checked=p.IncludeCursor;reverse.Checked=p.PinReverseWheel;wheel.Checked=p.PinWheelOpacity;snap.Checked=p.PinSnap;shadow.Checked=p.PinShadow;history.Checked=p.HistoryEnabled;Value(opacity,p.PinOpacity);Value(zoom,p.PinZoomPercent);Value(border,p.PinBorderWidth);Value(radius,p.PinRadius);Value(historyLimit,p.HistoryLimit);edgeColor.Text=p.PinBorderColor;directory.Text=string.IsNullOrWhiteSpace(p.SaveDirectory)?Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory):p.SaveDirectory;template.Text=p.FileNameTemplate;action.SelectedIndex=p.CompleteAction=="save"?2:p.CompleteAction=="copy-save"?1:0;}
        internal Preferences Apply(Preferences p){p.InitialTool=tools[initial.SelectedIndex];p.IncludeCursor=cursor.Checked;p.PinReverseWheel=reverse.Checked;p.PinWheelOpacity=wheel.Checked;p.PinSnap=snap.Checked;p.PinShadow=shadow.Checked;p.PinOpacity=(int)opacity.Value;p.PinZoomPercent=(int)zoom.Value;p.PinBorderWidth=(int)border.Value;p.PinRadius=(int)radius.Value;p.PinBorderColor=edgeColor.Text.Trim();p.CompleteAction=new[]{"copy","copy-save","save"}[action.SelectedIndex];p.SaveDirectory=CaptureCompletion.StoreSaveDirectory(directory.Text);p.FileNameTemplate=template.Text.Trim();p.HistoryEnabled=history.Checked;p.HistoryLimit=(int)historyLimit.Value;return p;}
    }
}
