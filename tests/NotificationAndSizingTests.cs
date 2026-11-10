using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenCraft
{
    static class NotificationAndSizingTests
    {
        sealed class Entry:IStartupEntry{internal string Command;internal int Writes;public string Read(){return Command;}public void Write(string command){Command=command;Writes++;}public void Remove(){Command=null;}}
        static IEnumerable<Control> Children(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Children(child))yield return nested;}}
        static void Draft(AnnotationSizeInput input,string value){Children(input).OfType<TextBox>().Single().Text=value;}
        static Bitmap Source(){var image=new Bitmap(1000,700);using(var g=Graphics.FromImage(image))g.Clear(Color.FromArgb(237,240,245));return image;}
        static CaptureOverlay Overlay(){return new CaptureOverlay(Source(),new Rectangle(0,0,1000,700),new Rectangle(80,70,800,440),null);}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("startup repair/old quoted Unicode location migrates while retaining enabled startup",delegate{
                string old=Path.Combine(output,"中文 用户","旧版本","ScreenCraft.exe"),current=Path.Combine(output,"中文 用户","新版本","ScreenCraft.exe");var entry=new Entry{Command=StartupCommand.For(old)};
                check(StartupRepair.RepairMovedEntry(entry,current,p=>p==current?new Version(0,10,3):new Version(0,9,3))&&entry.Writes==1&&entry.Command==StartupCommand.For(current),"old startup path not repaired");
                check(!StartupRepair.RepairMovedEntry(entry,current,p=>new Version(0,10,3))&&entry.Writes==1,"same location rewritten");
            });
            run("startup repair/disabled custom and newer entries are preserved",delegate{
                string current=Path.Combine(output,"new","ScreenCraft.exe"),old=Path.Combine(output,"old","ScreenCraft.exe");
                foreach(var command in new[]{null,"",StartupCommand.For(old)+" --custom",StartupCommand.For(Path.Combine(output,"other.exe"))}){var entry=new Entry{Command=command};check(!StartupRepair.RepairMovedEntry(entry,current,p=>new Version(0,10,3))&&entry.Command==command&&entry.Writes==0,"changed disabled or custom startup");}
                var newer=new Entry{Command=StartupCommand.For(old)};check(!StartupRepair.RepairMovedEntry(newer,current,p=>p==current?new Version(0,10,3):new Version(0,11))&&newer.Writes==0,"older build took newer startup entry");
            });
            run("system notice/start and duplicate launch send through same notification callback",delegate{
                var messages=new List<string>();StartupFeedback.Send((title,body)=>messages.Add(title+"|"+body),"Alt+A",false);StartupFeedback.Send((title,body)=>messages.Add(title+"|"+body),null,true);
                check(messages[0].StartsWith("ScreenCraft 已启动|")&&messages[0].Contains("Alt+A")&&messages[1].StartsWith("ScreenCraft 已在运行|")&&!messages[1].Contains("按  截图"),"notification content or disabled shortcut wrong");check(typeof(Canvas).Assembly.GetType("ScreenCraft.StartupToast")==null,"custom startup form compiled");
            });
            run("stroke sizing/default four and quick maximum twenty-four are shared by capture and editor",delegate{
                using(var overlay=Overlay()){var c=overlay.AnnotationCanvas;check(c.DefaultStyle.Width==4,"default stroke remains thin");var input=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="线宽或字号数值");var slider=Children(overlay).OfType<CaptureSlider>().Single(n=>n.AccessibleName=="线宽或字号");check(input.Value==4&&input.Minimum==1&&input.Maximum==24&&slider.Maximum==24,"capture range");Draft(input,"7.5");input.CommitEdit();check(c.DefaultStyle.Width==7.5f,"decimal default input ignored");}
                using(var editor=new CraftEditor(Source())){var input=Children(editor).OfType<AnnotationSizeInput>().Single();check(input.Value==4&&input.Maximum==24,"editor default range");Draft(input,"6.25");input.CommitEdit();check(editor.AnnotationCanvas.DefaultStyle.Width==6.25f,"editor numeric input ignored");}
            });
            run("stroke sizing/typed selected width is undoable and high input clamps",delegate{
                using(var overlay=Overlay()){var c=overlay.AnnotationCanvas;c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,200,160,500,350));var input=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="线宽或字号数值");Draft(input,"9.5");input.CommitEdit();check(c.SelectedShape.Width==9.5,"selection numeric edit");c.Undo();check(c.SelectedShape.Width==3,"numeric undo");Draft(input,"999");input.CommitEdit();check(c.SelectedShape.Width==24,"input exceeded common range");Draft(input,"bad");input.CommitEdit();check(c.SelectedShape.Width==24&&input.Text=="24.00","invalid input changed selection");}
            });
            run("stroke sizing/old wide marks display actual width without silent truncation",delegate{
                using(var overlay=Overlay()){var mark=ScreenCraftTests.Mark(Tool.Rect,200,160,500,350);mark.Width=80;overlay.AnnotationCanvas.AddAnnotation(mark);var input=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="线宽或字号数值");var slider=Children(overlay).OfType<CaptureSlider>().Single(n=>n.AccessibleName=="线宽或字号");check(overlay.AnnotationCanvas.SelectedShape.Width==80&&input.Value==80&&slider.Maximum==24,"loading rewrote old width or expanded slider");Draft(input,"12");input.CommitEdit();check(overlay.AnnotationCanvas.SelectedShape.Width==12&&input.Maximum==24,"old width did not return to common range");}
            });
            run("stroke sizing/text mosaic spotlight and cloud retain their own parameter units",delegate{
                using(var overlay=Overlay()){var c=overlay.AnnotationCanvas;var input=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="线宽或字号数值");c.SetTool(Tool.Text);check(input.Minimum==8&&input.Maximum==300,"font range truncated");Draft(input,"42");input.CommitEdit();check(c.DefaultStyle.FontPx==42,"font field edits line width");c.SetTool(Tool.Pixelate);Draft(input,"36");input.CommitEdit();check(c.DefaultStyle.MosaicBlock==36,"mosaic field");c.SetTool(Tool.Spotlight);Draft(input,"81");input.CommitEdit();check(c.DefaultStyle.EffectOpacity==81,"spotlight field");c.SetTool(Tool.Cloud);var anchor=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="标记或云线数值");check(anchor.Maximum==24,"cloud width range");Draft(anchor,"7");anchor.CommitEdit();check(c.DefaultStyle.Width==7,"cloud numeric field");}
            });
            run("stroke sizing/Enter commits and Escape discards draft without capture completion",delegate{
                using(var overlay=Overlay()){var c=overlay.AnnotationCanvas;var input=Children(overlay).OfType<AnnotationSizeInput>().Single(n=>n.AccessibleName=="线宽或字号数值");int focused=0;Draft(input,"8.5");check(c.DefaultStyle.Width==4,"draft committed before Enter");check(input.HandleKey(Keys.Enter,delegate{focused++;})&&focused==1&&c.DefaultStyle.Width==8.5&&!overlay.Completed,"Enter finalized capture");Draft(input,"19");input.HandleKey(Keys.Escape,delegate{focused++;});check(c.DefaultStyle.Width==8.5&&input.Text=="8.50"&&!overlay.IsDisposed,"Escape applied draft or closed capture");}
            });
            run("palette/expanded grid stays distinct and clickable within bounds at four DPI sizes",delegate{
                foreach(float scale in new[]{1f,1.25f,1.5f,2f}){int applied=0;Color actual=Color.Empty;using(var palette=new AnnotationPalette(Color.Red,color=>{actual=color;applied++;},null,scale)){var swatches=Children(palette).OfType<PaletteSwatch>().ToArray();check(swatches.Length==28&&swatches.Select(b=>b.Ink.ToArgb()).Distinct().Count()==28,"palette duplicates");foreach(var swatch in swatches)check(palette.ClientRectangle.Contains(swatch.Bounds),"palette cell clipped");for(int a=0;a<swatches.Length;a++)for(int b=a+1;b<swatches.Length;b++)check(!swatches[a].Bounds.IntersectsWith(swatches[b].Bounds),"palette cells overlap");typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(swatches.Last(),new object[]{EventArgs.Empty});check(applied==1&&actual.ToArgb()==swatches.Last().Ink.ToArgb(),"last row unreachable");using(var image=palette.RenderOffscreen())image.Save(Path.Combine(output,"rich-palette-"+(int)(scale*100)+".png"));}}
            });
            run("stroke sizing/offscreen quick editors use numeric fields with keyboard access",delegate{
                using(var overlay=Overlay()){using(var image=overlay.RenderSyntheticPreview())image.Save(Path.Combine(output,"numeric-stroke-capture.png"));}
                using(var editor=new CraftEditor(Source())){editor.CreateControl();editor.PerformLayout();using(var image=new Bitmap(editor.ClientSize.Width,editor.ClientSize.Height)){using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,editor,editor);image.Save(Path.Combine(output,"numeric-stroke-editor.png"));}}
            });
        }
    }
}
