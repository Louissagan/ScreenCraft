using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
namespace Stackshot
{
    public sealed partial class Preferences
    {
        // New shortcuts are opt-in, so an upgrade never claims more system keys.
        public string FullScreen="",ActiveWindow="",RepeatRegion="",TogglePins="",Board="";
        public string InitialTool="Rect";
        public bool PinReverseWheel,PinWheelOpacity,PinSnap=true,PinShadow=true;
        public int PinOpacity=100,PinZoomPercent,PinBorderWidth=1,PinRadius=10;
        public string PinBorderColor="#636872";
        public string CompleteAction="copy",SaveDirectory="",FileNameTemplate="ScreenCraft-{date}-{time}";
        public bool HistoryEnabled;
        public int HistoryLimit=100;
        public string ValidateDaily()
        {
            if(Array.IndexOf(new[]{"Rect","Ellipse","Line","Arrow","Pen","Text","Callout","Counter","Cloud","Pixelate","Redact","Crop"},InitialTool)<0)return "默认标注工具无效。";
            if(PinOpacity<20||PinOpacity>100||PinZoomPercent<0||PinZoomPercent>400||PinBorderWidth<0||PinBorderWidth>8||PinRadius<0||PinRadius>60)return "贴图透明度、缩放或边框参数超出范围。";
            System.Drawing.Color color;if(!AnnotationPalette.TryHex(PinBorderColor,out color))return "贴图边框颜色应为 #RRGGBB。";
            if(Array.IndexOf(new[]{"copy","copy-save","save"},CompleteAction)<0)return "完成后动作无效。";
            if(HistoryLimit<10||HistoryLimit>500)return "历史上限应为 10–500。";
            if(string.IsNullOrWhiteSpace(FileNameTemplate)||FileNameTemplate.Length>120||FileNameTemplate.IndexOfAny(Path.GetInvalidFileNameChars())>=0)return "文件名模板含无效字符。";
            if(SelectionDimPercent<45||SelectionDimPercent>85||Array.IndexOf(new[]{2,4,6},SelectionBorderWidth)<0||Array.IndexOf(new[]{"blue","green","orange"},SelectionBorderColor)<0||Array.IndexOf(new[]{"top","bottom"},ToolbarPlacement)<0)return "选区外观参数无效。";
            if(Array.IndexOf(new[]{"RGB","HEX","HSL"},ColorFormat)<0)return "取色格式无效。";
            if(FileNameTemplate.Contains("{")&&FileNameTemplate.Replace("{date}","").Replace("{time}","").Replace("{id}","").IndexOfAny(new[]{'{','}'})>=0)return "文件名模板仅支持 {date}、{time}、{id}。";
            return Toolbar==null?null:Toolbar.Validate();
        }
    }
    internal static class PreferenceTransfer
    {
        internal static void Export(Preferences preferences,string path)
        {
            var clone=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(preferences));
            clone.RunOnStartup=false; // Machine-specific consent is never carried to another machine.
            File.WriteAllText(FilePaths.Native(path),new JavaScriptSerializer().Serialize(clone));
        }
        internal static Preferences Import(string path,bool currentStartup)
        {
            if(new FileInfo(FilePaths.Native(path)).Length>65536)throw new InvalidDataException("配置文件过大。");
            Preferences value;
            try{value=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(FilePaths.Native(path)));}catch(Exception ex){throw new InvalidDataException("配置格式无效。",ex);}
            if(value==null||value.Version!=1)throw new InvalidDataException("不支持的配置版本。");
            string error=value.ValidateDaily();if(error!=null)throw new InvalidDataException(error);
            var seen=new HashSet<string>();foreach(var key in value.Hotkeys().Values){if(string.IsNullOrWhiteSpace(key))continue;uint modifiers;System.Windows.Forms.Keys code;if(!Hotkeys.TryParse(key,out modifiers,out code)||modifiers==0||(modifiers&8)!=0||!seen.Add(modifiers+":"+(uint)code))throw new InvalidDataException("配置中有无效或重复快捷键。");}
            value.RunOnStartup=currentStartup;return value;
        }
    }
}
