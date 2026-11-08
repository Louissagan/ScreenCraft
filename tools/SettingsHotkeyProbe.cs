// Opt-in native focus/WM_HOTKEY regression; only self-owned settings and a blank test window are shown.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Stackshot;
class SettingsHotkeyProbe
{
    sealed class Registrar:IHotkeyRegistrar
    {
        internal int Id;public bool Register(int id,uint modifiers,Keys key){Id=id;return true;}
        public void Unregister(int id){}public void Dispose(){}
    }
    static IEnumerable<Control> Children(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in Children(child))yield return nested;}}
    static object Invoke(object target,string method,params object[] args){return target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);}
    static void Check(bool okay,string detail){if(!okay)throw new Exception(detail);}
    [STAThread]static int Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);var results=new List<string>();
        try{
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            var registrar=new Registrar();using(var manager=new TransactionalHotkeys(registrar))using(var settings=new SettingsDialog(new Preferences(),"isolated test; nothing saved",delegate{return "test does not save";})){
                string error;Check(manager.TryApply(new Dictionary<string,string>{{"capture","Ctrl+Alt+A"}},out error),"binding fixture");int captures=0;
                manager.CapturePressed=keys=>(bool)Invoke(settings,"RecordRegisteredShortcut",keys);
                manager.Pressed+=delegate{captures++;settings.Hide();using(var capture=new Form{Text="ScreenCraft · isolated capture lifecycle test",Width=340,Height=180}){capture.Show();Application.DoEvents();capture.Close();}settings.Show();settings.Activate();Application.DoEvents();};
                settings.Show();settings.Activate();Application.DoEvents();var all=Children(settings).ToArray();var captureField=all.OfType<HotkeyInput>().Single(c=>c.AccessibleName=="区域截图");
                foreach(string page in new[]{"常规","截图","工具栏","OCR 与取色","关于","贴图"}){
                    var tab=all.OfType<Button>().Single(c=>c.AccessibleName==page);typeof(Control).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(tab,new object[]{EventArgs.Empty});tab.Focus();Application.DoEvents();Check(settings.ContainsFocus,"settings did not receive focus");int before=captures;Invoke(manager,"Dispatch",registrar.Id);Check(captures==before+1&&settings.Visible&&!settings.IsDisposed&&!settings.Modal,"global shortcut blocked or settings lost on "+page);results.Add("PASS focused settings page: "+page);
                }
                Invoke(settings,"SelectPage",2);captureField.Focus();Application.DoEvents();Check(captureField.Focused,"recorder did not receive focus");int count=captures;Invoke(manager,"Dispatch",registrar.Id);Check(captures==count&&captureField.Text=="Ctrl+Alt+A","focused recorder triggered capture");results.Add("PASS global binding recorded only in focused shortcut field");
                Invoke(captureField,"RecordKey",Keys.Control|Keys.Shift|Keys.F8);var tabGeneral=all.OfType<Button>().Single(c=>c.AccessibleName=="常规");typeof(Control).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(tabGeneral,new object[]{EventArgs.Empty});tabGeneral.Focus();Application.DoEvents();Invoke(manager,"Dispatch",registrar.Id);Check(captures==count+1&&captureField.Text=="Ctrl+Shift+F8"&&settings.Visible,"capture dropped unsaved shortcut draft");results.Add("PASS draft survives hide/capture/restore");
                settings.Hide();Invoke(manager,"Dispatch",registrar.Id);Check(captures==count+2,"hidden settings swallowed binding");results.Add("PASS hidden settings passes binding through");settings.Close();manager.CapturePressed=null;
            }
            File.WriteAllLines(Path.Combine(args[0],"native-settings-hotkeys.txt"),results);return 0;
        }catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(args[0],"native-settings-hotkeys.txt"),results);return 1;}
    }
}
