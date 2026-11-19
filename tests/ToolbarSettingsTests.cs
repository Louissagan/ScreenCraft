using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class ToolbarSettingsTests
    {
        static System.Collections.Generic.IEnumerable<Control> Children(Control root){foreach(Control c in root.Controls){yield return c;foreach(var d in Children(c))yield return d;}}
        static void Click(Button button){typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});}
        static bool Key(CaptureOverlay overlay,Keys keys){var message=Message.Create(IntPtr.Zero,0x100,IntPtr.Zero,IntPtr.Zero);return (bool)typeof(CaptureOverlay).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(overlay,new object[]{message,keys});}
        public static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("toolbar/legacy line entries merge without dropping either shortcut",delegate{
                var legacy=new ToolbarSettings();
                legacy.Order=new[]{"Line"}.Concat(legacy.Order.Where(id=>id!="PenPath")).ToArray();legacy.Keys.Remove("PenPath");
                legacy.Hidden=new[]{"Arrow","Counter"};
                legacy.Keys["Line"]="J";legacy.Keys["Arrow"]="G";
                var bothHidden=legacy.Clone();bothHidden.Hidden=new[]{"Line","Arrow","Counter"};
                var merged=ToolbarSettings.Safe(legacy);
                check(merged.Order[0]=="Arrow"&&!merged.Order.Contains("Line")&&merged.Hidden.SequenceEqual(new[]{"Counter"}),"visible line position or hidden tools lost");
                check(merged.Order[Array.IndexOf(merged.Order,"Pen")+1]=="PenPath"&&merged.Keys["PenPath"]==""&&merged.Find(Keys.J)==Tool.Line&&merged.Find(Keys.G)==Tool.Arrow,"legacy shortcuts lost");
                check(ToolbarSettings.Safe(bothHidden).Hidden.Contains("Arrow"),"merged hidden entry became visible");
            });
            run("toolbar/editor line style converts existing geometry and supports undo",delegate{
                using(var editor=new CraftEditor(new Bitmap(640,480))){
                    var canvas=editor.AnnotationCanvas;
                    var button=Children(editor).OfType<CaptureIconButton>().Single(b=>b.AccessibleName.StartsWith("线条"));
                    Click(button);
                    check(button.Selected&&canvas.Tool==Tool.Arrow,"merged tool did not activate");
                    var line=ScreenCraftTests.Mark(Tool.Line,42,65,160,180);
                    line.Curved=true;line.C=new PointF(95,58);
                    canvas.AddAnnotation(line);
                    var plain=Children(editor).OfType<CaptureIconButton>().Single(b=>b.AccessibleName=="无箭头（直线）");
                    var thin=Children(editor).OfType<CaptureIconButton>().Single(b=>b.AccessibleName=="细线箭头");
                    check(button.Selected&&plain.Selected&&!thin.Selected,"opening an existing line did not select its style");
                    Click(thin);
                    var arrow=canvas.SelectedShape;
                    check(arrow.Id==line.Id&&arrow.Kind==Tool.Arrow&&arrow.SimpleArrow&&arrow.A==line.A&&arrow.B==line.B&&arrow.C==line.C&&arrow.Curved,"line geometry or arrow style lost");
                    Click(plain);
                    check(canvas.SelectedShape.Kind==Tool.Line,"removing arrowhead failed");
                    canvas.Undo();check(canvas.SelectedShape.Kind==Tool.Arrow&&thin.Selected&&!plain.Selected,"undo did not restore the arrow");
                    canvas.Undo();check(canvas.SelectedShape.Kind==Tool.Line&&plain.Selected&&!thin.Selected,"undo did not restore the original line");
                }
            });
            run("toolbar/settings clone isolates order hidden flags and key dictionaries",delegate{var settings=new ToolbarSettings();var clone=settings.Clone();clone.Move(2,0);clone.Hidden=new[]{"Rect"};clone.Keys["Arrow"]="S";check(settings.Order[0]=="Rect"&&settings.Keys["Arrow"]=="A"&&settings.Hidden.Contains("Counter"),"settings aliased");check(clone.Validate()==null&&clone.Order[0]=="Arrow","move invalid");});
            run("toolbar/duplicates reserved keys malformed orders and corrupt configs rejected",delegate{var s=new ToolbarSettings();s.Keys["Arrow"]="R";check(s.Validate()!=null,"duplicate");s.Keys["Arrow"]="P";check(s.Validate()!=null,"reserved pin");s.Keys["Arrow"]="C";check(s.Validate()!=null,"reserved color");s.Keys["Arrow"]="S";s.Order[0]="Unknown";check(s.Validate()!=null&&ToolbarSettings.Safe(s).Order[0]=="Rect","corrupt fallback");});
            run("toolbar/tool shortcut recorder accepts one letter and protects global chords",delegate{using(var input=new HotkeyInput{ToolShortcutMode=true}){input.RecordKey(Keys.G);check(input.Text=="G","letter not recorded");input.RecordKey(Keys.Control|Keys.R);check(input.Text=="G","modifier chord became tool key");input.RecordKey(Keys.P);input.RecordKey(Keys.C);check(input.Text=="G","reserved key accepted");input.RecordKey(Keys.Back);check(input.Text=="","clear failed");}});
            run("toolbar/preference roundtrip retains order hidden tools and remapped keys",delegate{var preferences=new Preferences();preferences.Toolbar.DefaultFillPattern=new FillPatternSettings{Kind=FillPatternKind.Vertical,Spacing=28,StrokePercent=75};preferences.Toolbar.Move(2,0);preferences.Toolbar.Keys["Arrow"]="S";preferences.Toolbar.Hidden=new[]{"Rect","Redact"};var store=new PreferenceStore(Path.Combine(output,"toolbar-settings"));store.Save(preferences);var loaded=store.Load();check(loaded.Toolbar.DefaultFillPattern.Kind==FillPatternKind.Vertical&&loaded.Toolbar.DefaultFillPattern.StrokePercent==75&&loaded.Toolbar.Validate()==null&&loaded.Toolbar.Order[0]=="Arrow"&&loaded.Toolbar.Hidden.Contains("Rect")&&loaded.Toolbar.Keys["Arrow"]=="S","toolbar preferences lost");});
            run("toolbar/hidden dialog reorder checkbox key and confirm invoke real UI events",delegate{using(var dialog=new ToolbarSettingsDialog(new ToolbarSettings())){var list=Children(dialog).OfType<CheckedListBox>().Single();list.SetItemChecked(0,false);list.SelectedIndex=2;dialog.MoveSelection(-1);var fields=Children(dialog).OfType<HotkeyInput>().ToArray();fields.Single(f=>f.AccessibleName=="箭头快捷键").RecordKey(Keys.S);fields.Single(f=>f.AccessibleName=="无箭头快捷键").RecordKey(Keys.J);Click(Children(dialog).OfType<Button>().Single(b=>b.Text=="确认工具栏"));check(dialog.Result!=null&&dialog.Result.Hidden.Contains("Rect")&&dialog.Result.Order[1]=="Arrow"&&dialog.Result.Keys["Arrow"]=="S"&&dialog.Result.Keys["Line"]=="J"&&!dialog.Visible,"toolbar UI did not save controls");}});
            run("toolbar/general settings save preserves toolbar pending config",delegate{var p=new Preferences();p.Toolbar.Keys["Arrow"]="S";p.Toolbar.Hidden=new[]{"Rect"};Preferences saved=null;using(var dialog=new SettingsDialog(p,"fixture",delegate(Preferences value){saved=value;return "keep hidden";})){Click(Children(dialog).OfType<Button>().Single(b=>b.Text=="保存并应用"));check(saved!=null&&saved.Toolbar.Keys["Arrow"]=="S"&&saved.Toolbar.Hidden.Contains("Rect"),"global settings reset toolbar");}});
            run("toolbar/overlay consumes ordered visible tools and remapped keys",delegate{var settings=new ToolbarSettings();settings.Move(2,0);settings.Hidden=new[]{"Rect","Redact"};settings.Keys["Arrow"]="S";settings.Keys["Line"]="J";using(var overlay=new CaptureOverlay(new Bitmap(1000,700),new Rectangle(0,0,1000,700),new Rectangle(100,100,700,400),new[]{new Rectangle(0,0,1000,660)},settings)){var all=Children(overlay).OfType<CaptureIconButton>().ToArray();check(all.Any(b=>b.AccessibleName=="线条 S / J")&&!all.Any(b=>b.AccessibleName=="矩形 R"),"order visibility not applied");var main=Children(overlay).OfType<FlowLayoutPanel>().First(p=>p.Controls.OfType<CaptureIconButton>().Any(b=>b.AccessibleName=="线条 S / J"));check(main.Controls.OfType<CaptureIconButton>().First().AccessibleName=="线条 S / J","visual order stale");check(Key(overlay,Keys.S)&&overlay.AnnotationCanvas.Tool==Tool.Arrow,"remap ignored");check(Key(overlay,Keys.J)&&overlay.AnnotationCanvas.Tool==Tool.Line,"plain line shortcut ignored");check(Key(overlay,Keys.R)&&overlay.AnnotationCanvas.Tool==Tool.Rect,"hidden key inaccessible");var lineButton=main.Controls.OfType<CaptureIconButton>().First();Click(lineButton);check(lineButton.Selected&&overlay.AnnotationCanvas.Tool==Tool.Line,"line button lost last style");check(!overlay.Visible,"overlay shown");}});
            run("toolbar/text editing keeps remapped letters from selecting tools",delegate{var settings=new ToolbarSettings();settings.Keys["Arrow"]="S";using(var overlay=new CaptureOverlay(new Bitmap(1000,700),new Rectangle(0,0,1000,700),new Rectangle(100,100,700,400),new[]{new Rectangle(0,0,1000,660)},settings)){var c=overlay.AnnotationCanvas;c.SetTool(Tool.Text);var mark=ScreenCraftTests.Mark(Tool.Text,130,130,250,170);mark.Text="输入 G";c.AddAnnotation(mark);c.EditNote();check(c.Typing,"fixture not editing");Key(overlay,Keys.S);check(c.Tool==Tool.Text&&c.Typing,"typing selected arrow");}});
        }
    }
}
