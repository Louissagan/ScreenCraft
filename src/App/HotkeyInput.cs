using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    // Records messages addressed to this control; never installs a keyboard hook or injects input.
    public class HotkeyInput:TextBox
    {
        string beforeEditing="";
        public bool ToolShortcutMode;
        public event Action<string> Feedback;
        public HotkeyInput(){ReadOnly=true;BackColor=SystemColors.Window;ShortcutsEnabled=false;ImeMode=ImeMode.Disable;AccessibleDescription="点击后按 Ctrl、Alt 或 Shift 加一个按键。Delete 或 Backspace 清除，Esc 恢复本次修改，Tab 切换下一栏。";}
        protected override void OnEnter(EventArgs e){beforeEditing=Text;SelectAll();Report(ToolShortcutMode?"请单独按一个字母；P 贴图、C 取色为保留键。":"请直接按组合键；Delete / Backspace 清除，Esc 恢复，Tab 切换。");base.OnEnter(e);}
        protected override void OnLeave(EventArgs e){Report("");base.OnLeave(e);}
        public void ClearShortcut(){Text="";Report("已清除；保存并应用后禁用这个动作的快捷键。");}
        void Report(string message){var handler=Feedback;if(handler!=null)handler(message);}
        internal bool RecordKey(Keys keys)
        {
            if(keys==Keys.Tab||keys==(Keys.Shift|Keys.Tab))return false;
            if(keys==Keys.Escape){Text=beforeEditing;SelectAll();Report("已恢复进入此栏时的快捷键。");return true;}
            if(keys==Keys.Delete||keys==Keys.Back){ClearShortcut();return true;}
            Keys key=keys&Keys.KeyCode;
            if(Hotkeys.IsModifierKey(key)||key==Keys.None)return true;
            if(ToolShortcutMode){if(keys==key&&key>=Keys.A&&key<=Keys.Z&&key!=Keys.P&&key!=Keys.C){Text=key.ToString();Report("已录入工具键 "+Text);}else Report("请单独按一个英文字母；P 和 C 为保留键。");return true;}
            uint modifiers;Keys parsed;string value=Hotkeys.ToSetting(keys);
            if(!Hotkeys.TryParse(value,out modifiers,out parsed)||modifiers==0||(modifiers&8)!=0){Report("请同时按 Ctrl、Alt 或 Shift 加一个按键，例如 Ctrl+Shift+F8。");return true;}
            Text=value;SelectAll();Report("已录入 "+value+"；点击“保存并应用”后生效。");return true;
        }
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData){return RecordKey(keyData)||base.ProcessCmdKey(ref msg,keyData);}
        protected override void OnKeyPress(KeyPressEventArgs e){e.Handled=true;base.OnKeyPress(e);}
    }
}
