using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class TrayAndSelectionTests
    {
        static IEnumerable<Control> Children(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var c in Children(child))yield return c;}}
        static void Click(Button button){typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(button,new object[]{EventArgs.Empty});}
        static int BluePixels(Bitmap image,Rectangle bounds){int count=0;for(int y=bounds.Top;y<bounds.Bottom;y++)for(int x=bounds.Left;x<bounds.Right;x++){var c=image.GetPixel(x,y);if(c.B>150&&c.R<100)count++;}return count;}
        static Rectangle InRoot(Control control,Control root){var p=Point.Empty;Control node=control;while(node!=root){p.Offset(node.Location);node=node.Parent;}return new Rectangle(p,control.Size);}
        public static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("tray/default background and repeated launch never request settings",delegate{int settings=0,notices=0;foreach(var args in new[]{new string[0],new[]{"--background"},new[]{"unrecognized"}}){bool opens=StartupPolicy.OpensSettings(args);check(!opens,"default opens settings");StartupPolicy.Dispatch(opens,delegate{settings++;},delegate{notices++;});}StartupPolicy.Dispatch(StartupPolicy.OpensSettings(new[]{"--SETTINGS"}),delegate{settings++;},delegate{notices++;});check(settings==1&&notices==3,"wake dispatch");check(StartupPolicy.WakeEvent("fixture-user",true)!=StartupPolicy.WakeEvent("fixture-user",false),"settings wake aliases default wake");});
            run("tray/application context has no main form and disposes hidden controller",delegate{var controller=new Form{ShowInTaskbar=false};using(var context=new TrayApplicationContext(controller)){var handle=controller.Handle;check(context.MainForm==null&&!controller.Visible,"launcher would be shown");int exits=0;context.ThreadExit+=delegate{exits++;};controller.Close();check(exits==1&&!controller.Visible,"hidden controller failed to exit context");}check(controller.IsDisposed,"controller leak");});
            run("tray/owned geometric icon survives native handle disposal",delegate{using(var icon=TrayBrand.Create())using(var image=icon.ToBitmap()){check(image.Width==32&&image.GetPixel(16,16).B>180,"tray icon blank");check(image.GetPixel(8,8).R>220,"capture corners absent");image.Save(Path.Combine(output,"tray-icon-offscreen.png"));}});
            run("selection/default outside is seventy percent darker and selection untouched",delegate{using(var source=new Bitmap(300,200))using(var painted=new Bitmap(300,200)){using(var g=Graphics.FromImage(source))g.Clear(Color.FromArgb(200,160,120));foreach(int percent in new[]{45,70,85}){using(var g=Graphics.FromImage(painted))CaptureOverlayGeometry.PaintSurround(g,source,new Rectangle(50,40,200,120),percent);int expected=(int)Math.Round(200*(1-SelectionAppearance.DimAlpha(percent)/255.0));check(Math.Abs(painted.GetPixel(10,10).R-expected)<=1,"mask alpha wrong");check(painted.GetPixel(100,80).ToArgb()==source.GetPixel(100,80).ToArgb(),"source inside changed");check(SelectionAppearance.SourceAlpha(percent)+SelectionAppearance.DimAlpha(percent)==255,"preselection alpha differs");}check(SelectionAppearance.DefaultDimPercent==70&&SelectionAppearance.Percent(0)==45&&SelectionAppearance.Percent(100)==85,"limits");}});
            run("selection/frame has blue outer edge white inner edge and white centered handles",delegate{using(var image=new Bitmap(150,120))using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(60,60,60));SelectionAppearance.PaintFrame(g,new Rectangle(30,30,90,60));SelectionAppearance.PaintHandle(g,new Rectangle(26,26,9,9));check(image.GetPixel(70,29).B>180&&image.GetPixel(70,29).R<50,"blue edge absent");check(image.GetPixel(70,30).R>180,"inner edge absent");check(image.GetPixel(30,30).R>240,"handle center absent");}});
            run("selection/preferences migrate old json and persist new dim setting",delegate{var store=new PreferenceStore(Path.Combine(output,"selection-settings"));Directory.CreateDirectory(store.DirectoryPath);File.WriteAllText(store.FilePath,"{\"Version\":1,\"Capture\":\"Ctrl+Alt+A\"}");check(store.Load().SelectionDimPercent==70,"legacy config not migrated");var p=store.Load();p.SelectionDimPercent=82;store.Save(p);check(store.Load().SelectionDimPercent==82,"mask setting lost");});
            run("selection/tools and properties move together when combined block does not fit below",delegate{using(var overlay=new CaptureOverlay(new Bitmap(1000,700),new Rectangle(0,0,1000,700),new Rectangle(100,200,780,380),new[]{new Rectangle(0,0,1000,660)})){check(overlay.OptionsBounds.Top==overlay.ToolbarBounds.Bottom+(int)Math.Round(6*overlay.AnnotationCanvas.Ui)&&overlay.OptionsBounds.Bottom<=200,"combined toolbar did not move above selection");check(!overlay.OptionsBounds.IntersectsWith(overlay.ToolbarBounds),"bars overlap");}});
            run("settings/seven actual pages retain global chords mask toolbar and startup transaction",delegate{Preferences saved=null;var original=new Preferences{SelectionDimPercent=82,RunOnStartup=true};using(var dialog=new SettingsDialog(original,"fixture-data",delegate(Preferences p){saved=p;return "fixture conflict keeps dialog open";})){var controls=Children(dialog).ToArray();check(controls.OfType<PreferencesTab>().Count()==7,"missing tabs");Click(controls.OfType<PreferencesTab>().Single(t=>t.Text=="截图"));check(dialog.SelectedPage==1,"tab did not change page");controls.OfType<CaptureSlider>().Single().Value=76;Click(controls.OfType<Button>().Single(t=>t.Text=="保存并应用"));check(saved!=null&&saved.SelectionDimPercent==76&&saved.RunOnStartup&&saved.Capture==original.Capture&&!dialog.Visible,"pending values not saved or desktop shown");check(original.SelectionDimPercent==82,"original mutated before acceptance");Click(controls.OfType<Button>().Single(t=>t.Text=="恢复默认"));check(controls.OfType<CaptureSlider>().Single().Value==70,"default mask not restored");}});
            run("settings/hidden production paint renders all pages at normal and compact sizes",delegate{
                using(var dialog=new SettingsDialog(new Preferences(),"C:\\Fixture\\ScreenCraft",delegate{return "unused";})){
                    for(int i=0;i<7;i++)using(var preview=dialog.RenderSyntheticPreview(i)){
                        preview.Save(Path.Combine(output,"settings-"+i+"-offscreen.png"));int ink=0;
                        for(int y=0;y<preview.Height;y++)for(int x=0;x<preview.Width;x++){var c=preview.GetPixel(x,y);if(c.R<140&&c.G<140&&c.B<140)ink++;}
                        check(BluePixels(preview,new Rectangle(0,0,preview.Width,preview.Height))>1000&&ink>1200,"child controls absent from preview");
                        if(i==2){var field=Children(dialog).OfType<HotkeyInput>().Single(c=>c.AccessibleName=="区域截图");check(BluePixels(preview,InRoot(field,dialog))>20,"recorded chord text absent or translated to wrong coordinates");}
                        var tab=Children(dialog).OfType<PreferencesTab>().Single(t=>t.Selected);check(BluePixels(preview,new Rectangle(tab.Left,tab.Top+39,tab.Width,22))>10,"tab caption absent or misplaced");
                    }
                    dialog.ClientSize=new Size(720,520);using(var compact=dialog.RenderSyntheticPreview(2))compact.Save(Path.Combine(output,"settings-compact-offscreen.png"));
                    var buttons=Children(dialog).OfType<Button>().Where(b=>b.Text=="保存并应用"||b.Text=="恢复默认").ToArray();check(buttons.All(b=>dialog.ClientRectangle.Contains(b.Bounds))&&!dialog.Visible,"footer clipped or form shown");
                }
            });
        }
    }
}
