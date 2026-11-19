using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScreenCraft
{
    // Only serialization and validation live here. Registration is handled by
    // TransactionalHotkeys, so editing settings never releases the active bindings.
    public static class Hotkeys
    {
        static readonly Dictionary<string,Keys> aliases=new Dictionary<string,Keys>(StringComparer.OrdinalIgnoreCase){
            {"PrtSc",Keys.PrintScreen},{"PrtScn",Keys.PrintScreen},{"ImprPant",Keys.PrintScreen},{"Impr Pant",Keys.PrintScreen},
            {"Esc",Keys.Escape},{"Del",Keys.Delete},{"Supr",Keys.Delete},{"Backspace",Keys.Back},{"Retroceso",Keys.Back},
            {"Intro",Keys.Enter},{"Espacio",Keys.Space},{"Ins",Keys.Insert},{"Inicio",Keys.Home},{"Fin",Keys.End},
            {"Pausa",Keys.Pause},{"Re Pág",Keys.PageUp},{"Av Pág",Keys.PageDown}
        };
        public static bool IsModifierKey(Keys key)
        {
            int code=(int)(key&Keys.KeyCode);return code==16||code==17||code==18||code==91||code==92||(code>=160&&code<=165);
        }
        public static bool TryParse(string chord,out uint modifiers,out Keys key)
        {
            modifiers=0;key=Keys.None;if(string.IsNullOrWhiteSpace(chord))return false;
            foreach(string token in chord.Split('+')){
                string part=token.Trim();if(part.Length==0)return false;
                switch(part.ToLowerInvariant()){
                    case "ctrl":case "control":modifiers|=2;continue;
                    case "shift":case "mayús":case "mayus":modifiers|=4;continue;
                    case "alt":modifiers|=1;continue;
                    case "altgr":modifiers|=3;continue;
                    case "win":case "windows":modifiers|=8;continue;
                }
                if(key!=Keys.None)return false;Keys parsed;
                if(part.Length==1&&part[0]>='0'&&part[0]<='9')parsed=Keys.D0+(part[0]-'0');
                else if(!aliases.TryGetValue(part,out parsed)){
                    if(char.IsDigit(part[0])||part[0]=='-'||!Enum.TryParse(part,true,out parsed)||!Enum.IsDefined(typeof(Keys),parsed))return false;
                }
                int value=(int)parsed;
                if(value<8||value>254||IsModifierKey(parsed)||parsed==Keys.ProcessKey||parsed==Keys.Packet)return false;
                key=parsed;
            }
            return key!=Keys.None;
        }
        public static string ToSetting(Keys chord){return ToSetting(chord,false);}
        public static string ToSetting(Keys chord,bool windows)
        {
            var parts=new List<string>();if(windows)parts.Add("Win");if((chord&Keys.Control)!=0)parts.Add("Ctrl");if((chord&Keys.Shift)!=0)parts.Add("Shift");if((chord&Keys.Alt)!=0)parts.Add("Alt");
            Keys key=chord&Keys.KeyCode;string name;
            switch(key){case Keys.Enter:name="Enter";break;case Keys.PrintScreen:name="PrintScreen";break;case Keys.PageUp:name="PageUp";break;case Keys.PageDown:name="PageDown";break;case Keys.CapsLock:name="CapsLock";break;
                default:name=key>=Keys.D0&&key<=Keys.D9?((int)(key-Keys.D0)).ToString(System.Globalization.CultureInfo.InvariantCulture):key.ToString();break;}
            parts.Add(name);return string.Join("+",parts.ToArray());
        }
    }
}
