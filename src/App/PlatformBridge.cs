// ScreenCraft application host and Windows integration boundaries.
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace ScreenCraft {
 public partial class CaptureHost {
  readonly Control dispatcher = new Control();
  public CaptureHost() { var handle=dispatcher.Handle; }
  public void Ui(Action action) { if(dispatcher.IsDisposed)return; if(dispatcher.InvokeRequired)dispatcher.BeginInvoke(action);else action(); }
  public static CaptureHost Running;
  public Action<ToolbarSettings> ToolbarChanged;
  public static bool Test;
  public static Icon AppIcon = TrayBrand.Create();
  public Settings Settings = new Settings();
  public static float ScaleFor(Screen screen) {
   var bounds=screen.Bounds;
   var center=new Native.POINT{X=bounds.Left+bounds.Width/2,Y=bounds.Top+bounds.Height/2};
   uint x,y;
   if(Native.GetDpiForMonitor(Native.MonitorFromPoint(center,2),0,out x,out y)==0&&x>0)return x/96f;
   using(var g=Graphics.FromHwnd(IntPtr.Zero))return g.DpiX/96f;
  }
  public static float MaxZoom(Size s) { return (float)Math.Min(8, Math.Sqrt(24000000.0/Math.Max(1,(double)s.Width*s.Height))); }
  public static void Log(string text) { /* upstream diagnostics suppressed: never persist screenshot titles or OCR */ }
  public static string Explain(Exception ex,string path) { return "无法读取或写入图片，请检查文件权限和空间。"; }
  public static Bitmap LoadFull(string path) { using(var file=FilePaths.Read(path))using(var img=Image.FromStream(file,true,true)) { if((long)img.Width*img.Height>64000000) throw new InvalidDataException("图片超过 6400 万像素"); return new Bitmap(img); } }
  public static void CopyImage(Bitmap b) { using(var png=new MemoryStream()){b.Save(png,System.Drawing.Imaging.ImageFormat.Png);png.Position=0;var data=new DataObject();data.SetData(DataFormats.Bitmap,true,b);data.SetData("PNG",false,png);Clipboard.SetDataObject(data,true,5,50);} }
  public void Notify(string title,string text) { Notify(title,text,null); }
  public Action<string,string> Notification;
  public Action BeginScroll;
  public void Notify(string title,string text,string folder) { if(Notification!=null)Notification(title,text);else MessageBox.Show(text,title,MessageBoxButtons.OK,MessageBoxIcon.Information); }
  public Action<Bitmap> CaptureReady;
  public string OcrLanguage="auto";
  public Preferences ColorPreferences=new Preferences();
  public void CopyText(Bitmap b,object unused) {
   new OcrResultWindow(new Bitmap(b),OcrLanguage).Show();
  }
  public string SaveCapture(Bitmap b,string name) { if(CaptureReady!=null)CaptureReady(b);else b.Dispose();return null; }
 }
 internal static class Sheet { public static bool HasMica; }
 internal static class TrayMenu {
  public static ContextMenuStrip Create() { return new StyledMenu(); }
  public static ToolStripMenuItem Item(string text,string icon,Action action) { string glyph=icon=="edit"?"pen":icon=="textscan"?"ocr":icon=="actualsize"?"crop":icon=="close"?"cancel":icon;return new CraftMenuItem(text,glyph,action); }
  public static ToolStripSeparator Separator() {return new ToolStripSeparator();}
 }
 internal static class Dialogs { public static void Error(string title,string message){ MessageBox.Show(message,title,MessageBoxButtons.OK,MessageBoxIcon.Error); } public static DialogResult Show(IWin32Window owner,string message,string title,MessageBoxButtons buttons,MessageBoxIcon icon) {return MessageBox.Show(owner,message,title,buttons,icon);} }
 internal static class CaptureInterop { [System.Runtime.InteropServices.DllImport("dwmapi.dll")] public static extern int DwmFlush(); }
 public class Settings {
  public Settings Clone() {return (Settings)MemberwiseClone();}
  public static string DataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data");
  public int BgPadding=50,BgRadius=40,BgPreset,BgKind,BgColor=0xFFFFFF,BgBlur,BgShadowSize=50,BgInset,BgAlign,ScrollGuides;
  public bool BgShadow=true,BgBalance;
  public string BgRatio="auto";
  public void Save() {System.IO.Directory.CreateDirectory(DataDir);string path=Path.Combine(DataDir,"backdrop.json");string temp=path+".tmp";try{File.WriteAllText(temp,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(this));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 }
}
namespace ScreenCraft { public enum ButtonKind { Primary, Secondary, Plain, Danger, Glass } }
