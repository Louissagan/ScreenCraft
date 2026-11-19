using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class OcrColorSettingsTests
    {
        static System.Collections.Generic.IEnumerable<Control> Children(Control p){foreach(Control c in p.Controls){yield return c;foreach(var nested in Children(c))yield return nested;}}
        static void Click(Button b){typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,new object[]{EventArgs.Empty});}
        internal static void Run(Action<string,Action> run,Action<bool,string> check)
        {
            run("OCR settings/only discovered languages and automatic mode are selectable",delegate{
                var setting=new OcrLanguageSetting("zh-Hans");using(setting.Choice)using(setting.Status){setting.SetCapabilities(new OcrCapabilities{Languages=new[]{"en-US","zh-Hans-CN","en-US"}});check(setting.Choice.Enabled&&setting.Choice.Choices.Length==3&&setting.SelectedLanguage=="zh-Hans-CN","installed aliases or duplicate filtering");check(setting.Choice.Choices[1].EndsWith("en-US")&&setting.Choice.Choices[2].EndsWith("zh-Hans-CN"),"labels lost actual engine tags");setting.Choice.SelectedIndex=1;check(setting.SelectedLanguage=="en-US","selection did not map to engine tag");}
            });
            run("OCR settings/unavailable imported language never becomes an offered option",delegate{
                var setting=new OcrLanguageSetting("fr-FR");using(setting.Choice)using(setting.Status){setting.SetCapabilities(new OcrCapabilities{Languages=new[]{"en-US"}});check(setting.SelectedLanguage=="auto"&&!setting.Choice.Choices.Any(c=>c.Contains("fr-FR"))&&setting.Status.Text.Contains("不可用"),"unsupported language offered or silently kept");setting.SelectedLanguage="en";check(setting.SelectedLanguage=="en-US","imported alias not resolved");setting.SelectedLanguage="auto";check(setting.Choice.SelectedIndex==0,"reset auto");}
            });
            run("OCR settings/missing engine disables language list and keeps other preferences usable",delegate{
                var setting=new OcrLanguageSetting("auto");using(setting.Choice)using(setting.Status){check(setting.SelectedLanguage=="auto"&&!setting.Choice.Enabled,"loading state");setting.SetCapabilities(new OcrCapabilities{Error="OCR unavailable",Languages=new[]{"en-US"}});check(!setting.Choice.Enabled&&setting.Choice.Choices.Length==1&&setting.SelectedLanguage=="auto"&&setting.Status.Text.Contains("安装"),"failed discovery left selectable phantom languages");}
            });
            run("color settings/two RGB encodings preview and save distinct compatible values",delegate{
                Preferences saved=null;using(var dialog=new SettingsDialog(new Preferences(),"fixture",p=>{saved=p;return "stay open";})){
                    var all=Children(dialog).ToArray();var format=all.OfType<PreferencesChoice>().Single(c=>c.AccessibleName=="取色格式");var example=all.OfType<Label>().Single(c=>c.AccessibleName=="取色输出示例");var upper=all.OfType<PreferencesSwitch>().Single(c=>c.AccessibleName=="HEX 使用大写字母");var save=all.OfType<Button>().Single(c=>c.Text=="保存并应用");
                    check(format.Choices[0].StartsWith("RGB")&&format.Choices[1].StartsWith("RGB"),"encodings presented as different color spaces");format.SelectedIndex=1;check(example.Text.Contains("#FF8040")&&upper.Enabled,"HEX sample or uppercase not active");Click(save);check(saved.ColorFormat=="HEX","HEX compatibility key changed");upper.Checked=false;check(example.Text.Contains("#ff8040"),"lowercase sample stale");format.SelectedIndex=0;check(example.Text.Contains("rgb(255, 128, 64)")&&!upper.Enabled,"RGB sample or case state");Click(save);check(saved.ColorFormat=="RGB","RGB save changed format");
                }
            });
        }
    }
}
