using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace Stackshot {
 public static class ScreenCraftProgram {
  [STAThread] public static void Main(string[] args) {
   try {
    if(InstallerCleanup.IsCommand(args)){try{InstallerCleanup.Handle(args);}catch{Environment.ExitCode=1603;}return;}
    Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
    Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
    I18n.Apply("en"); Ds.Apply(0); Theme.Init();
    string sid=System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;bool created;
    using(var mutex=new Mutex(true,"Local\\ScreenCraft-"+sid,out created)) {
     if(!created){try{using(var open=EventWaitHandle.OpenExisting(StartupPolicy.WakeEvent(sid,StartupPolicy.OpensSettings(args))))open.Set();}catch(WaitHandleCannotBeOpenedException){}return;}
     using(var wake=new EventWaitHandle(false,EventResetMode.AutoReset,StartupPolicy.WakeEvent(sid,false)))
     using(var settingsWake=new EventWaitHandle(false,EventResetMode.AutoReset,StartupPolicy.WakeEvent(sid,true)))
     using(var exitWake=new EventWaitHandle(false,EventResetMode.AutoReset,InstallerCleanup.ExitEvent(sid,Application.ExecutablePath)))
     using(var exitDone=new EventWaitHandle(false,EventResetMode.ManualReset,InstallerCleanup.ExitEvent(sid,Application.ExecutablePath,true))){
      var home=new CraftHome(args);var handle=home.Handle;
      var registration=ThreadPool.RegisterWaitForSingleObject(wake,delegate(object state,bool timeout){home.RequestWake(false);},null,-1,false);
      var settingsRegistration=ThreadPool.RegisterWaitForSingleObject(settingsWake,delegate(object state,bool timeout){home.RequestWake(true);},null,-1,false);
      var exitRegistration=ThreadPool.RegisterWaitForSingleObject(exitWake,delegate(object state,bool timeout){home.RequestUninstallExit();},null,-1,false);
      using(var context=new TrayApplicationContext(home))try{home.StartQuietly(args);Application.Run(context);}finally{registration.Unregister(null);settingsRegistration.Unregister(null);exitRegistration.Unregister(null);mutex.ReleaseMutex();exitDone.Set();}
     }
    }
   }catch(Exception ex) { MessageBox.Show("ScreenCraft 无法启动："+ex.Message,"ScreenCraft"); }
  }
 }
 public class CraftHome : Form {
  readonly ShotStack host=new ShotStack();
  readonly NotifyIcon tray=new NotifyIcon();
  readonly TransactionalHotkeys hotkeys=new TransactionalHotkeys(new WindowsHotkeyRegistrar());
  readonly PreferenceStore preferencesStore;
  Preferences preferences;
  readonly Label status=new Label {AutoSize=true};
  bool picking,exiting,released;
  Icon brand;
  string startupWarning;SettingsDialog activeSettings;
  StartupToast startupToast;
  System.Collections.Generic.List<Form> scrollHidden;
  public CraftHome(string[] args) {
   ShotStack.Running=host;Icon=ShotStack.AppIcon;Text="ScreenCraft · 后台";ShowInTaskbar=false;FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.Dpi;
   string basePath=AppDomain.CurrentDomain.BaseDirectory;bool portable=File.Exists(Path.Combine(basePath,"portable.flag"));
   string userData=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenCraft");preferencesStore=PreferenceStore.Select(portable?Path.Combine(basePath,"Data"):userData,userData);
   preferences=preferencesStore.Load();host.ToolbarChanged=delegate(ToolbarSettings t){preferences.Toolbar=t;preferencesStore.Save(preferences);};try{string actualStartup=new UserStartupEntry().Read();preferences.RunOnStartup=StartupCommand.Matches(actualStartup,Application.ExecutablePath);if(actualStartup!=null&&!preferences.RunOnStartup)preferencesStore.Warning="此用户的 ScreenCraft 启动项指向其他位置，可在常规设置中主动切换到当前程序。";}catch(Exception){preferences.RunOnStartup=false;preferencesStore.Warning="无法读取当前用户启动项，请在常规设置中检查。";}Settings.DataDir=preferencesStore.DirectoryPath;host.OcrLanguage=preferences.OcrLanguage;host.ColorPreferences=preferences;
   Font=new Font("Microsoft YaHei UI",9);ClientSize=new Size(1,1);brand=TrayBrand.Create();tray.Icon=brand;tray.Text="ScreenCraft · 截图与贴图";tray.Visible=true;
   var menu=TrayActions.Create(delegate{return preferences;},delegate(string action){switch(action){case "capture":CaptureArea(false);break;case "fullscreen":case "window":case "repeat":CaptureArea(false,action);break;case "togglepins":PinWindow.ToggleAllPins();break;case "ocr":CaptureArea(true);break;case "color":PickScreenColor();break;case "scroll":BeginScroll();break;case "open":OpenImage();break;case "files":PinFiles();break;case "paste":PastePin();break;case "memo":OpenMemo(null);break;case "pins":ShowPins();break;case "board":new BoardWindow().Show();break;case "history":new HistoryWindow(preferencesStore.DirectoryPath,preferences.HistoryEnabled).Show();break;case "folder":try{string folder=CaptureCompletion.SaveDirectory(preferences,preferencesStore.DirectoryPath);Directory.CreateDirectory(folder);System.Diagnostics.Process.Start(folder);}catch(Exception ex){host.Notify("无法打开保存目录",ex.Message);}break;case "settings":ShowSettings();break;case "exit":exiting=true;Close();break;}});
   tray.ContextMenuStrip=menu; tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)CaptureArea(false);};
   host.Notification=delegate(string title,string message){tray.ShowBalloonTip(5000,title,message,ToolTipIcon.Info);};host.BeginScroll=BeginScroll;ScrollCapture.Completed+=ScrollEnded;
   host.CaptureReady=delegate(Bitmap b){new CraftEditor(b).Show();};
   hotkeys.Pressed+=delegate(string action){if(action=="capture")CaptureArea(false); if(action=="ocr")CaptureArea(true); if(action=="paste")PastePin();if(action=="color")PickScreenColor();if(action=="scroll")BeginScroll();if(action=="fullscreen"||action=="window"||action=="repeat")CaptureArea(false,action);if(action=="togglepins")PinWindow.ToggleAllPins();if(action=="board")new BoardWindow().Show();};
   string hotkeyError;bool registered=hotkeys.TryApply(preferences.Hotkeys(),out hotkeyError);
   startupWarning=preferencesStore.Warning??(registered?null:hotkeyError);
   status.MaximumSize=new Size(640,0);status.Text=preferencesStore.Warning??(registered?"快捷键已注册。截图和 OCR 均在本机处理。":hotkeyError);
   FormClosing+=delegate(object s,FormClosingEventArgs e){if(!exiting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
   FormClosed+=delegate {ReleaseRuntime();};
   AllowDrop=true;DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;};DragDrop+=delegate(object sender,DragEventArgs e){foreach(var path in (string[])e.Data.GetData(DataFormats.FileDrop))TryPinFile(path);};
  }
  protected override void SetVisibleCore(bool value){base.SetVisibleCore(false);}
  internal void StartQuietly(string[] args){if(StartupPolicy.OpensSettings(args))RequestWake(true);else BeginInvoke((Action)delegate{if(StartupPolicy.ShowsNotice(args,preferences))ShowStartupFeedback(false);if(startupWarning!=null)host.Notify("ScreenCraft 配置提示",startupWarning);});}
  void ShowStartupFeedback(bool alreadyRunning){if(exiting||IsDisposed||picking||ScrollCapture.Active||!preferences.ShowStartupNotice)return;if(startupToast!=null&&!startupToast.IsDisposed)startupToast.Close();startupToast=new StartupToast(Screen.PrimaryScreen.WorkingArea,preferences.Capture,alreadyRunning);startupToast.Show();}
  internal void RequestWake(bool settings){if(IsDisposed||!IsHandleCreated||exiting)return;try{BeginInvoke((Action)delegate{if(!exiting&&!IsDisposed)StartupPolicy.Dispatch(settings,ShowSettings,delegate{ShowStartupFeedback(true);});});}catch(InvalidOperationException){}}
  internal void RequestUninstallExit(){if(IsDisposed||exiting)return;BeginInvoke((Action)delegate{foreach(Form window in new System.Collections.Generic.List<Form>(System.Linq.Enumerable.Cast<Form>(Application.OpenForms)))if(window!=this&&!(window is PinWindow)&&!window.IsDisposed){window.Close();if(!window.IsDisposed&&window.Visible)return;}exiting=true;Close();});}
  void ReleaseRuntime(){if(released)return;released=true;exiting=true;if(startupToast!=null)startupToast.Dispose();ScrollCapture.Completed-=ScrollEnded;ScrollCapture.Stop();hotkeys.Dispose();tray.Visible=false;tray.Dispose();foreach(var p in PinWindow.All.ToArray())p.Close();if(brand!=null){brand.Dispose();brand=null;}}
  protected override void Dispose(bool disposing){if(disposing)ReleaseRuntime();base.Dispose(disposing);}
  internal static Button Add(Control parent,string name,Action action){var b=new Button{Text=name,AutoSize=true,MinimumSize=new Size(180,34),AccessibleName=name,Margin=new Padding(0,0,8,8)};b.Click+=delegate{action();};parent.Controls.Add(b);return b;}
  void CaptureArea(bool ocr,string mode="region") {
   if(picking||ScrollCapture.Active)return; picking=true;
   var hidden=new System.Collections.Generic.List<Form>();
   bool homeWasVisible=Visible;IntPtr source=Native.GetForegroundWindow();Action afterCapture=null;
   try{
    foreach(Form f in Application.OpenForms) if(f.Visible && !(f is CraftHome)){hidden.Add(f);f.Hide();} Hide();
    CaptureInterop.DwmFlush();
    if(ocr){Bitmap frozen;Rectangle at;IntPtr win;var rect=RegionPicker.Pick(RegionPicker.Mode.Text,false,false,out frozen,out at,out win,out selectedMode);if(rect.IsEmpty){if(frozen!=null)frozen.Dispose();return;}afterCapture=delegate{using(frozen)host.CopyText(frozen,null);};}
    else {
     Action<Bitmap,Rectangle,Rectangle,IWin32Window> edit=delegate(Bitmap frozen,Rectangle bounds,Rectangle rect,IWin32Window owner){
      using(var editor=new CaptureOverlay(frozen,bounds,rect,Array.ConvertAll(Screen.AllScreens,delegate(Screen screen){return screen.WorkingArea;}),preferences.Toolbar,preferences.SelectionDimPercent,preferences)){
       editor.ShowDialog(owner);
       if(editor.Completed){var r=editor.ScreenSelection;preferences.LastCapture=new[]{r.X,r.Y,r.Width,r.Height};try{preferencesStore.Save(preferences);}catch(Exception ex){host.Notify("上次截图范围未保存",ex.Message);}}
       var boardImage=editor.TakeBoardImage();var textImage=editor.TakeOcrImage();if(boardImage!=null)afterCapture=delegate{var board=new BoardWindow();board.Document.AddImage(boardImage,PointF.Empty);board.Show();};else if(textImage!=null)afterCapture=delegate{using(textImage)host.CopyText(textImage,null);};else if(editor.ScrollRequested)afterCapture=BeginScroll;
      }
     };
     var desktop=SystemInformation.VirtualScreen;var foreground=source==IntPtr.Zero?Rectangle.Empty:Grabber.Bounds(source);
     var direct=CaptureWorkflows.Resolve(mode,desktop,Screen.FromPoint(Control.MousePosition).Bounds,foreground,preferences.LastCapture);
     if(mode!="region"&&direct.IsEmpty)host.Notify("请重新框选","目标窗口或上次区域不在当前桌面，已切回区域截图。");
     if(direct.IsEmpty)RegionPicker.PickAndAnnotate(edit);
     else using(var grab=Dib.FromScreen(desktop)){if(preferences.IncludeCursor)Grabber.DrawCursor(grab.Dc,desktop.X,desktop.Y);edit(grab.ToBitmap(),desktop,direct,null);}
    }
   }catch(Exception ex){MessageBox.Show("截图失败："+ex.Message);}finally{picking=false;if(!exiting&&!IsDisposed){foreach(var f in hidden)if(!f.IsDisposed&&!f.Visible)f.Show();if(homeWasVisible&&!Visible)Show();if(afterCapture!=null)BeginInvoke(afterCapture);else if(source!=IntPtr.Zero)Native.SetForegroundWindow(source);}}
  }
  RegionPicker.Mode selectedMode;
  System.Collections.Generic.List<Form> HideOwnWindows(){var list=new System.Collections.Generic.List<Form>();foreach(Form f in Application.OpenForms)if(f.Visible){list.Add(f);f.Hide();}CaptureInterop.DwmFlush();return list;}
  void RestoreWindows(System.Collections.Generic.List<Form> list){if(exiting||IsDisposed||list==null)return;foreach(var form in list)if(!form.IsDisposed)form.Show();}
  void PickScreenColor(){if(picking||ScrollCapture.Active)return;picking=true;System.Collections.Generic.List<Form> hidden=null;try{hidden=HideOwnWindows();string value=RegionPicker.PickColor(preferences.ColorFormat,preferences.ColorUpper);RestoreWindows(hidden);if(value!=null){Clipboard.SetText(value);host.Notify("颜色已复制",value);}}catch(Exception ex){host.Notify("取色失败",ex.Message);}finally{RestoreWindows(hidden);picking=false;}}
  void BeginScroll(){if(ScrollCapture.Active){ScrollCapture.Toggle(host,host.Settings);return;}if(picking)return;picking=true;try{scrollHidden=HideOwnWindows();ScrollCapture.Toggle(host,host.Settings);if(!ScrollCapture.Active)ScrollEnded();}catch(Exception ex){ScrollEnded();host.Notify("滚动截图失败",ex.Message);}finally{picking=false;}}
  void ScrollEnded(){var hidden=scrollHidden;scrollHidden=null;RestoreWindows(hidden);}
  void OpenImage(){using(var d=new OpenFileDialog{Filter="图片或可编辑工程|*.png;*.jpg;*.jpeg;*.bmp;*.sccraft"})if(d.ShowDialog()==DialogResult.OK)try{CraftEditor.OpenFile(d.FileName).Show();}catch(Exception ex){MessageBox.Show(ex.Message);}}
  void PastePin(){try{using(var image=Clipboard.GetImage()){if(image==null){if(Clipboard.ContainsText())OpenMemo(Clipboard.GetText());else MessageBox.Show("剪贴板中没有图片或文字。");return;}PinWindow.Open(new Bitmap(image));}}catch(Exception ex){MessageBox.Show("无法读取剪贴板："+ex.Message);}}
  void OpenMemo(string initial){try{if(initial==null&&Clipboard.ContainsText())initial=Clipboard.GetText();using(var memo=new TextMemoDialog(initial))memo.ShowDialog(this);}catch(Exception ex){host.Notify("文字备忘无法打开",ex.Message);}}
  void PinFiles(){using(var d=new OpenFileDialog{Filter="图片或可编辑工程|*.png;*.jpg;*.jpeg;*.bmp;*.sccraft",Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK)foreach(var file in d.FileNames)TryPinFile(file);}
  void TryPinFile(string path){try{if(string.Equals(Path.GetExtension(path),".sccraft",StringComparison.OrdinalIgnoreCase))PinWindow.Open(AnnotationDocument.Open(path));else PinWindow.Open(ShotStack.LoadFull(path));}catch(Exception ex){MessageBox.Show(this,"无法贴图："+ex.Message);}}
  void ShowPins(){new PinManagerWindow().Show();}
  void ShowSettings(){
   if(activeSettings!=null&&!activeSettings.IsDisposed){activeSettings.WindowState=FormWindowState.Normal;activeSettings.Show();activeSettings.Activate();return;}
   var settings=new SettingsDialog(preferences,preferencesStore.DirectoryPath,SavePreferences,status.Text);
   activeSettings=settings;hotkeys.CapturePressed=settings.RecordRegisteredShortcut;
   settings.FormClosed+=delegate{if(activeSettings==settings){activeSettings=null;hotkeys.CapturePressed=null;}};
   // Capturing temporarily hides our windows. A modal settings loop would end on Hide and discard its draft.
   try{settings.Show();}catch{activeSettings=null;hotkeys.CapturePressed=null;settings.Dispose();throw;}
  }
  string SavePreferences(Preferences next){
   string error=next.ValidateDaily();if(error!=null)return error;if(!hotkeys.TryApply(next.Hotkeys(),out error))return error;
   var startup=new UserStartupEntry();string oldStartup=null;bool startupTouched=false;
   try{if(next.RunOnStartup!=preferences.RunOnStartup){oldStartup=startup.Read();StartupCommand.Apply(startup,next.RunOnStartup,Application.ExecutablePath);startupTouched=true;}preferencesStore.Save(next);preferences=next;foreach(var pin in PinWindow.All)pin.ApplyPinAppearance(next);host.OcrLanguage=next.OcrLanguage;host.ColorPreferences=next;status.Text="设置已保存；当前快捷键已成功注册。";return null;}
   catch(Exception ex){string ignored;hotkeys.TryApply(preferences.Hotkeys(),out ignored);if(startupTouched){try{if(oldStartup==null)startup.Remove();else startup.Write(oldStartup);}catch{}}return "设置未能保存，已尝试恢复原快捷键："+ex.Message;}
  }
 }
}


