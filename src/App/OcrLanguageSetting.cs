using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class OcrLanguageSetting
    {
        const string Automatic="自动选择（已安装语言）";
        internal readonly PreferencesChoice Choice=new PreferencesChoice(Automatic){AccessibleName="OCR 语言",Enabled=false};
        internal readonly Label Status=new Label{Text="打开设置后自动检测本机可用的 OCR 语言。",Tag="note",Height=40,ForeColor=PreferencesLook.Muted,Margin=new Padding(0,16,0,0),AccessibleName="OCR 语言检测状态"};
        string requested;string[] tags=new[]{"auto"};bool loaded,loading;
        internal OcrLanguageSetting(string value){requested=string.IsNullOrWhiteSpace(value)?"auto":value;}
        internal string SelectedLanguage{get{if(!loaded&&!requested.Equals("auto",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("正在检测本机 OCR 语言，请稍后再保存。");return tags[Choice.SelectedIndex];}set{requested=string.IsNullOrWhiteSpace(value)?"auto":value;if(loaded)Resolve();}}
        internal async Task LoadAsync()
        {
            if(loaded||loading)return;loading=true;Status.Text="正在检测本机可用的 OCR 语言……";
            OcrCapabilities caps;try{caps=await new LocalOcrService().CapabilitiesAsync();}catch(Exception ex){caps=new OcrCapabilities{Error="无法读取本机 OCR 语言："+ex.Message};}
            if(Choice.IsDisposed||Status.IsDisposed)return;SetCapabilities(caps);
        }
        internal void SetCapabilities(OcrCapabilities caps)
        {
            tags=new[]{"auto"}.Concat(caps.Available?caps.Languages.Where(t=>!string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase):new string[0]).ToArray();loaded=true;
            Choice.SetChoices(tags.Select(t=>t=="auto"?Automatic:Name(t)).ToArray());Choice.Enabled=tags.Length>1;Resolve();
            if(tags.Length==1)Status.Text="未检测到可用的本地 OCR 语言。请在 Windows 语言选项中安装文字识别功能后重新打开设置。";
        }
        static string Name(string tag){try{return CultureInfo.GetCultureInfo(tag).DisplayName+" · "+tag;}catch(CultureNotFoundException){return tag;}}
        void Resolve()
        {
            string selected=requested.Equals("auto",StringComparison.OrdinalIgnoreCase)?"auto":Ocr.SelectLanguage(requested,tags.Skip(1).ToArray());
            Choice.SelectedIndex=selected==null?0:Array.IndexOf(tags,selected);
            Status.Text=selected==null?"原设置的语言 "+requested+" 在本机不可用，已改选自动；保存后生效。":"仅列出本机 OCR 引擎实际支持的语言；自动选择也只使用已安装语言。";
        }
    }
    internal static class ColorFormatSettings
    {
        internal static readonly string[] Codes={"RGB","HEX","HSL"};
        internal static readonly string[] Labels={"RGB · 三通道数值","RGB · 六位十六进制","HSL · 色相／饱和度／亮度"};
        internal static int Index(string code){int i=Array.IndexOf(Codes,code);return i<0?0:i;}
    }
}
