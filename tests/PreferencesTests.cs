using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class PreferencesTests
    {
        sealed class FakeKeys:IHotkeyRegistrar
        {
            public readonly Dictionary<int,string> Registered=new Dictionary<int,string>();public string Busy;
            public bool Register(int id,uint mods,Keys key){string token=mods+":"+(uint)key;if(Busy==token||Registered.ContainsValue(token))return false;Registered.Add(id,token);return true;}
            public void Unregister(int id){Registered.Remove(id);}public void Dispose(){}
        }
        sealed class FakeStartup:IStartupEntry {public string Value;public string Read(){return Value;}public void Write(string command){Value=command;}public void Remove(){Value=null;}}
        public static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("hotkeys/conflict keeps old keys and releases partial acquisition",delegate{var fake=new FakeKeys();using(var manager=new TransactionalHotkeys(fake)){string error;check(manager.TryApply(new Preferences().Hotkeys(),out error),"initial keys");var before=new Dictionary<int,string>(fake.Registered);fake.Busy="6:"+(uint)Keys.B;check(!manager.TryApply(new Dictionary<string,string>{{"capture","Ctrl+Shift+X"},{"ocr","Ctrl+Shift+B"}},out error),"conflict accepted");check(fake.Registered.Count==before.Count,"partial key leaked");foreach(var kv in before)check(fake.Registered[kv.Key]==kv.Value,"old key lost");}check(fake.Registered.Count==0,"keys not released");});
            run("hotkeys/duplicates invalid unmodified letters are rejected",delegate{var fake=new FakeKeys();using(var manager=new TransactionalHotkeys(fake)){string error;check(!manager.TryApply(new Dictionary<string,string>{{"one","Ctrl+A"},{"two","Control+A"}},out error),"duplicate");check(!manager.TryApply(new Dictionary<string,string>{{"one","A"}},out error),"plain letter");check(fake.Registered.Count==0,"invalid registered");}});
            run("hotkeys/swapping existing keys and disabling is transactional",delegate{var fake=new FakeKeys();using(var manager=new TransactionalHotkeys(fake)){string error;check(manager.TryApply(new Dictionary<string,string>{{"one","Ctrl+Alt+A"},{"two","Ctrl+Alt+B"}},out error),"initial");check(manager.TryApply(new Dictionary<string,string>{{"one","Ctrl+Alt+B"},{"two","Ctrl+Alt+A"}},out error),"swap");check(fake.Registered.Count==2,"swap leaked");check(manager.TryApply(new Dictionary<string,string>{{"one",""},{"two",""}},out error),"disable");check(fake.Registered.Count==0,"disabled still held");}});
            run("preferences/corruption recovers without overwriting source",delegate{string dir=Path.Combine(output,"preferences-test");Directory.CreateDirectory(dir);var store=new PreferenceStore(dir);File.WriteAllText(store.FilePath,"{broken");var p=store.Load();check(p.Capture=="Ctrl+Alt+A"&&store.Warning!=null,"no recovery");check(File.ReadAllText(store.FilePath)=="{broken","corruption overwritten");p.Capture="Ctrl+Shift+F2";store.Save(p);check(store.Load().Capture==p.Capture,"settings roundtrip");});
            run("preferences/unwritable portable path falls back without elevation",delegate{string file=Path.Combine(output,"not-a-directory");File.WriteAllText(file,"fixture");var store=PreferenceStore.Select(Path.Combine(file,"Data"),Path.Combine(output,"fallback-data"));check(store.Warning!=null&&store.DirectoryPath.EndsWith("fallback-data"),"fallback missing");store.Save(new Preferences());check(File.ReadAllText(file)=="fixture","unrelated file changed");});
            run("startup/adapter writes quoted user command and removes",delegate{var fake=new FakeStartup();StartupCommand.Apply(fake,true,Path.Combine(output,"folder with spaces","ScreenCraft.exe"));check(fake.Value.StartsWith("\"")&&fake.Value.EndsWith("\" --background"),"quoting");StartupCommand.Apply(fake,false,"unused.exe");check(fake.Value==null,"entry remains");});
            run("files/PNG unicode path atomic save and reopen alpha",delegate{string path=Path.Combine(output,"透明中文.png");using(var image=new Bitmap(50,40)){image.SetPixel(10,10,Color.FromArgb(70,20,80,180));ImageFiles.SavePng(image,path);using(var reopened=new Bitmap(path)){check(reopened.Size==image.Size,"dimensions");check(reopened.GetPixel(10,10).A==70,"alpha lost");}byte[] before=File.ReadAllBytes(path);using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)){bool rejected=false;try{ImageFiles.SavePng(image,path);}catch(IOException){rejected=true;}check(rejected,"locked destination accepted");}check(Convert.ToBase64String(before)==Convert.ToBase64String(File.ReadAllBytes(path)),"failed save corrupted destination");check(Directory.GetFiles(output,"透明中文.png.*.tmp").Length==0,"temp remains");}});
        }
    }
}
