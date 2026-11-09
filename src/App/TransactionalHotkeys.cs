using System;
using System.Collections.Generic;
using System.Windows.Forms;
namespace ScreenCraft
{
    public interface IHotkeyRegistrar:IDisposable{bool Register(int id,uint modifiers,Keys key);void Unregister(int id);}
    public sealed class WindowsHotkeyRegistrar:NativeWindow,IHotkeyRegistrar
    {
        public event Action<int> Pressed;
        public WindowsHotkeyRegistrar(){CreateHandle(new CreateParams{Parent=new IntPtr(-3)});}
        public bool Register(int id,uint modifiers,Keys key){return Native.RegisterHotKey(Handle,id,modifiers|0x4000,(uint)key);}
        public void Unregister(int id){Native.UnregisterHotKey(Handle,id);}
        protected override void WndProc(ref Message m){if(m.Msg==0x312){var action=Pressed;if(action!=null)action(m.WParam.ToInt32());}base.WndProc(ref m);}
        public void Dispose(){DestroyHandle();}
    }
    public sealed class TransactionalHotkeys:IDisposable
    {
        sealed class Binding{public int Id;public uint Modifiers;public Keys Key;public string Action;public string Canonical{get{return Modifiers+":"+(uint)Key;}}}
        readonly IHotkeyRegistrar registrar;
        Dictionary<string,Binding> current=new Dictionary<string,Binding>();
        int next;
        public event Action<string> Pressed;
        public Func<Keys,bool> CapturePressed;
        public TransactionalHotkeys(IHotkeyRegistrar adapter){registrar=adapter;var windows=adapter as WindowsHotkeyRegistrar;if(windows!=null)windows.Pressed+=Dispatch;}
        internal void Dispatch(int id){foreach(var b in current.Values)if(b.Id==id){Keys keys=b.Key|((b.Modifiers&2)!=0?Keys.Control:Keys.None)|((b.Modifiers&1)!=0?Keys.Alt:Keys.None)|((b.Modifiers&4)!=0?Keys.Shift:Keys.None);if(CapturePressed!=null&&CapturePressed(keys))return;var handler=Pressed;if(handler!=null)handler(b.Action);return;}}
        public bool TryApply(Dictionary<string,string> requested,out string error)
        {
            var wanted=new Dictionary<string,Binding>();var canonical=new HashSet<string>();error=null;
            foreach(var pair in requested)
            {
                if(string.IsNullOrWhiteSpace(pair.Value))continue;
                uint mods;Keys key;
                if(!Hotkeys.TryParse(pair.Value,out mods,out key)||mods==0||(mods&8)!=0){error="快捷键无效："+pair.Value+"。请使用 Ctrl/Alt/Shift 加一个按键；留空可禁用。";return false;}
                var b=new Binding{Modifiers=mods,Key=key,Action=pair.Key};
                if(!canonical.Add(b.Canonical)){error="两个动作不能使用相同快捷键："+pair.Value;return false;}wanted.Add(pair.Key,b);
            }
            var byKey=new Dictionary<string,Binding>();foreach(var b in current.Values)byKey[b.Canonical]=b;
            var acquired=new List<Binding>();
            foreach(var b in wanted.Values)
            {
                Binding old;if(byKey.TryGetValue(b.Canonical,out old)){b.Id=old.Id;continue;}
                b.Id=++next;
                if(!registrar.Register(b.Id,b.Modifiers,b.Key)){foreach(var added in acquired)registrar.Unregister(added.Id);error="快捷键已被其他程序占用或由 Windows 保留。原快捷键仍然有效。";return false;}acquired.Add(b);
            }
            foreach(var b in current.Values)if(!canonical.Contains(b.Canonical))registrar.Unregister(b.Id);
            current=wanted;return true;
        }
        public void Dispose(){foreach(var b in current.Values)registrar.Unregister(b.Id);current.Clear();registrar.Dispose();}
    }
}
