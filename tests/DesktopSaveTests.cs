using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class DesktopSaveTests
    {
        static System.Collections.Generic.IEnumerable<Control> Children(Control p){foreach(Control c in p.Controls){yield return c;foreach(var nested in Children(c))yield return nested;}}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("desktop save/default follows known folder provider with Unicode redirected desktop",delegate{
                var data=Path.Combine(output,"portable-data");var desktop=Path.GetFullPath(Path.Combine(output,"用户","王 小明","OneDrive - 工作室","桌面"));
                foreach(var value in new[]{null,"","   "})check(CaptureCompletion.SaveDirectory(new Preferences{SaveDirectory=value},data,()=>desktop)==desktop,"default did not follow redirected desktop");
                check(CaptureCompletion.SaveDirectory(new Preferences(),data)==Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),"real Windows desktop resolution");
                check(CaptureCompletion.SaveDirectory(new Preferences{SaveDirectory="Captures"},data,()=>{throw new Exception("explicit path queried desktop");})==Path.GetFullPath(Path.Combine(data,"Captures")),"custom relative path changed");
            });
            run("desktop save/Chinese username spaces and long paths roundtrip PNG and avoid collisions",delegate{
                string folder=Path.GetFullPath(Path.Combine(output,"long-png",Guid.NewGuid().ToString("N"),"中文用户 王小明","OneDrive - 团队文件","桌面"));while(folder.Length<280)folder=Path.Combine(folder,"中文目录与空格 0123456789");
                var p=new Preferences{SaveDirectory=folder,CompleteAction="save",FileNameTemplate="中文截图"};var root=Path.Combine(output,"unicode-save-data");using(var image=new Bitmap(32,24)){
                    using(var g=Graphics.FromImage(image))g.Clear(Color.SeaGreen);var first=CaptureCompletion.Deliver(image,p,root,b=>{throw new Exception("save-only copied");});var second=CaptureCompletion.Deliver(image,p,root,b=>{});
                    check(first==Path.Combine(folder,"中文截图.png")&&second==Path.Combine(folder,"中文截图-1.png")&&FilePaths.Exists(first)&&FilePaths.Exists(second),"Unicode filename, long path or collision handling failed");
                    using(var stream=FilePaths.Read(first))using(var restored=new Bitmap(stream))check(restored.Size==image.Size&&restored.GetPixel(10,10).ToArgb()==Color.SeaGreen.ToArgb(),"PNG pixels lost");
                }
                var store=new PreferenceStore(root);store.Save(p);check(store.Load().SaveDirectory==folder,"Unicode configured path changed during persistence");
            });
            run("desktop save/settings displays actual desktop but saves dynamic portable default",delegate{
                using(var dialog=new SettingsDialog(new Preferences(),"fixture",p=>"test")){
                    var input=Children(dialog).OfType<TextBox>().Single(c=>c.AccessibleName=="自动保存目录");string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);check(input.Text==desktop,"default desktop not displayed");check(CaptureCompletion.StoreSaveDirectory(input.Text)=="","default leaked current username into stored configuration");
                    string custom=Path.Combine(output,"自定义 截图目录");check(CaptureCompletion.StoreSaveDirectory(custom)==custom,"custom directory discarded");check(CaptureCompletion.StoreSaveDirectory(" ")=="","clearing field did not reset default");
                }
            });
            run("desktop save/unavailable desktop reports error without falling back to app directory",delegate{
                bool rejected=false;try{CaptureCompletion.SaveDirectory(new Preferences(),output,()=>"");}catch(IOException){rejected=true;}check(rejected,"missing desktop silently saved in wrong folder");
            });
        }
    }
}
