// Opt-in live settings probe: uses shared offscreen painting; never reads the desktop or clipboard.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenCraft;
class OcrSettingsProbe
{
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
    [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr hwnd);
    static System.Collections.Generic.IEnumerable<Control> Children(Control p){foreach(Control c in p.Controls){yield return c;foreach(var nested in Children(c))yield return nested;}}
    static object Get(object c,string name){return c.GetType().GetProperty(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c,null);}
    static void Set(object c,string name,object value){c.GetType().GetProperty(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,value,null);Application.DoEvents();}
    static void Snap(Form f,string path){f.Refresh();Application.DoEvents();using(var b=(Bitmap)f.GetType().GetMethod("RenderSyntheticPreview",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f,new object[]{4}))b.Save(path);}
    [STAThread]static int Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);try{
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Preferences saved=null;
            using(var f=new SettingsDialog(new Preferences(),"isolated probe; no settings written",p=>{saved=p;return "test keeps window open";})){
                f.Show();f.GetType().GetMethod("SelectPage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f,new object[]{4});var all=Children(f).ToArray();var status=all.OfType<Label>().Single(c=>c.AccessibleName=="OCR 语言检测状态");var wait=System.Diagnostics.Stopwatch.StartNew();while((status.Text.StartsWith("正在")||status.Text.StartsWith("打开设置"))&&wait.ElapsedMilliseconds<10000){Application.DoEvents();System.Threading.Thread.Sleep(10);}if(status.Text.StartsWith("正在")||status.Text.StartsWith("打开设置"))throw new Exception("Discovery timed out");
                var language=all.Single(c=>c.AccessibleName=="OCR 语言");var format=all.Single(c=>c.AccessibleName=="取色格式");var options=(string[])Get(language,"Choices");Set(format,"SelectedIndex",0);Snap(f,Path.Combine(args[0],"ocr-rgb-native.png"));Set(format,"SelectedIndex",1);Snap(f,Path.Combine(args[0],"ocr-hex-native.png"));if(options.Length>1)Set(language,"SelectedIndex",1);
                var save=all.OfType<Button>().Single(c=>c.Text=="保存并应用");typeof(Control).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(save,new object[]{EventArgs.Empty});if(saved==null||saved.ColorFormat!="HEX")throw new Exception("Settings save failed");
                File.WriteAllText(Path.Combine(args[0],"native-result.txt"),"PASS native discovery, closed language picker, RGB/HEX examples and save. DPI="+GetDpiForWindow(f.Handle)+"\nAvailable choices: "+string.Join("; ",options)+"\nSaved OCR language="+saved.OcrLanguage+"; format="+saved.ColorFormat+"\n"+status.Text);
            }return 0;
        }catch(Exception ex){File.WriteAllText(Path.Combine(args[0],"native-error.txt"),ex.ToString());return 1;}
    }
}
