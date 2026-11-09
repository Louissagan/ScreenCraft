using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// Opt-in compositor probe. All forms and directed messages belong to this process.
// Pixels are read only while the complete sample rectangle is covered by its forms.
class CaptureTransitionProbe
{
    [StructLayout(LayoutKind.Sequential)] struct Point {public int X,Y;}
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
    [DllImport("user32.dll")] static extern bool GetWindowDisplayAffinity(IntPtr window,out uint affinity);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    static readonly Color source=Color.FromArgb(48,125,190),live=Color.FromArgb(225,110,55);
    static Assembly application;static string prefix;static volatile bool done;static volatile string phase="fixture";
    static int frames,invalid,excluded;static string obstruction;static Exception failure;
    static int completed;static bool pixelProbeEnabled;
    static readonly List<string> invalidSamples=new List<string>();
    static readonly List<string> events=new List<string>();static readonly Stopwatch clock=Stopwatch.StartNew();
    static BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Event(string value){events.Add(clock.ElapsedMilliseconds+" "+value);}
    static Type Type(string name){return application.GetType(prefix+"."+name,true);}
    static void Mouse(Form picker,int message,int x,int y){SendMessage(picker.Handle,message,message==0x202?IntPtr.Zero:new IntPtr(1),new IntPtr((y<<16)|(x&65535)));}
    static bool Owned(Rectangle rectangle)
    {
        uint self=(uint)Process.GetCurrentProcess().Id;for(int row=0;row<3;row++)for(int col=0;col<3;col++){uint process;GetWindowThreadProcessId(WindowFromPoint(new Point{X=rectangle.Left+8+col*(rectangle.Width-16)/2,Y=rectangle.Top+8+row*(rectangle.Height-16)/2}),out process);if(process!=self)return false;}return true;
    }
    static bool Near(Color a,Color b){return Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B)<18;}
    static void Record(Rectangle rectangle,string output)
    {
        try{using(var pixels=new Bitmap(rectangle.Width,rectangle.Height))using(var graphics=Graphics.FromImage(pixels)){
            while(!done){if(!Owned(rectangle)){obstruction="Self-owned fixture was obscured; no further pixels recorded.";return;}
                var top=GetAncestor(WindowFromPoint(new Point{X=rectangle.Left+30,Y=rectangle.Top+40}),2);uint affinity;
                if(GetWindowDisplayAffinity(top,out affinity)&&affinity!=0){excluded++;Thread.Sleep(4);continue;}
                graphics.CopyFromScreen(rectangle.Location,System.Drawing.Point.Empty,rectangle.Size,CopyPixelOperation.SourceCopy);if(!Owned(rectangle)){obstruction="Ownership changed during sample; sample discarded.";return;}
                int bad=0;foreach(var point in new[]{new System.Drawing.Point(30,40),new System.Drawing.Point(200,40),new System.Drawing.Point(430,40),new System.Drawing.Point(650,40),new System.Drawing.Point(750,40)}){var color=pixels.GetPixel(point.X,point.Y);if(!Near(color,source)&&!Near(color,live)&&!Near(color,Color.FromArgb(14,37,57)))bad++;}
                frames++;if(bad>=4){invalid++;invalidSamples.Add(clock.ElapsedMilliseconds+" phase="+phase+" pixel="+pixels.GetPixel(30,40)+" affinity="+affinity);if(invalid<=2)pixels.Save(Path.Combine(output,"unexpected-frame-"+invalid+".png"));}Thread.Sleep(4);
            }
        }}catch(Exception ex){failure=ex;}
    }
    [STAThread] static int Main(string[] arguments)
    {
        if(arguments.Length!=2)return 2;Directory.CreateDirectory(arguments[1]);SetProcessDpiAwarenessContext(new IntPtr(-4));Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        application=Assembly.LoadFrom(Path.GetFullPath(arguments[0]));prefix=application.GetType("ScreenCraft.RegionPicker")!=null?"ScreenCraft":"Stackshot";Type("Theme").GetMethod("Init").Invoke(null,null);
        var display=application.GetType(prefix+".CaptureDisplay");if(display!=null){var exclusion=display.GetField("ExcludeFromCapture",BindingFlags.NonPublic|BindingFlags.Static);if(exclusion!=null){exclusion.SetValue(null,(Action<IntPtr>)delegate{});pixelProbeEnabled=true;}}
        Rectangle work=Screen.PrimaryScreen.WorkingArea;var bounds=new Rectangle(work.Left+(work.Width-800)/2,work.Top+(work.Height-500)/2,800,500);int returnedVisible=0;
        using(var fixture=new Form{FormBorderStyle=FormBorderStyle.None,StartPosition=FormStartPosition.Manual,AutoScaleMode=AutoScaleMode.None,Bounds=bounds,BackColor=source,TopMost=true,ShowInTaskbar=false,Text="ScreenCraft synthetic transition probe"}){
            var handle=fixture.Handle;int disable=1;DwmSetWindowAttribute(handle,3,ref disable,4);fixture.Show();fixture.Refresh();DwmFlush();
            Thread recorder=null;if(pixelProbeEnabled){recorder=new Thread(delegate(){Record(bounds,arguments[1]);}){IsBackground=true};recorder.Start();}
            try{for(int cycle=0;cycle<3;cycle++){
                fixture.BackColor=source;fixture.Refresh();DwmFlush();Thread.Sleep(60);
                var dib=Activator.CreateInstance(Type("Dib"),new object[]{800,500});using(var g=(Graphics)Type("Dib").GetMethod("Graphics").Invoke(dib,null))g.Clear(source);
                var list=Activator.CreateInstance(typeof(List<>).MakeGenericType(Type("Grabber+Win")));var mode=Enum.Parse(Type("RegionPicker+Mode"),"Image");
                using(var picker=(Form)Activator.CreateInstance(Type("RegionPicker"),Hidden,null,new object[]{dib,bounds,list,mode,false,false},null)){
                    var modifiers=Type("RegionPicker").GetField("ModifierSource",Hidden);if(modifiers!=null)modifiers.SetValue(picker,(Func<Keys>)delegate{return Keys.None;});
                    Type("RegionPicker").GetField("annotationHandoff",Hidden).SetValue(picker,true);picker.Activated+=delegate{Event("picker activated / "+phase);};
                    Action<Bitmap,Rectangle,Rectangle,IWin32Window> annotate=delegate(Bitmap image,Rectangle desktop,Rectangle selection,IWin32Window owner){
                        using(var editor=(Form)Activator.CreateInstance(Type("CaptureOverlay"),new object[]{image,desktop,selection,new[]{bounds},null,70,null})){
                            var editorHandle=editor.Handle;var close=new System.Windows.Forms.Timer{Interval=260};close.Tick+=delegate{close.Stop();phase="editor-closing";editor.Close();};
                            editor.Shown+=delegate{phase="editor";Event("editor shown");fixture.BackColor=live;fixture.Refresh();close.Start();};
                            var show=Type("CaptureOverlay").GetMethod("ShowForCapture",Hidden);if(show==null)editor.ShowDialog(owner);else show.Invoke(editor,new object[]{owner});close.Dispose();
                            completed++;if(picker.Visible)returnedVisible++;Event("editor returned / picker visible="+picker.Visible);phase="return";
                        }
                    };
                    Type("RegionPicker").GetField("annotationEditor",Hidden).SetValue(picker,annotate);var pickerHandle=picker.Handle;
                    var select=new System.Windows.Forms.Timer{Interval=180};select.Tick+=delegate{select.Stop();phase="selection";Mouse(picker,0x201,80,80);Mouse(picker,0x200,580,300);Mouse(picker,0x202,580,300);};
                    picker.Shown+=delegate{phase="picker";Event("picker shown");select.Start();};picker.ShowDialog(fixture);select.Dispose();Application.DoEvents();DwmFlush();Thread.Sleep(80);
                }
            }}catch(Exception ex){failure=ex;}finally{done=true;if(recorder!=null)recorder.Join(5000);fixture.Close();}
        }
        var text="Version="+FileVersionInfo.GetVersionInfo(arguments[0]).FileVersion+"\r\nPixelProbeEnabled="+pixelProbeEnabled+"\r\nCompletedHandoffs="+completed+"\r\nFrames="+frames+"\r\nExcludedSamples="+excluded+"\r\nUnexpectedColorFrames="+invalid+"\r\nPickerVisibleAfterEditor="+returnedVisible+"\r\nObstruction="+obstruction+"\r\nFailure="+failure+"\r\n"+string.Join("\r\n",events.ToArray())+"\r\n"+string.Join("\r\n",invalidSamples.ToArray());File.WriteAllText(Path.Combine(arguments[1],"transition-probe.txt"),text);Console.WriteLine(text);return failure!=null||obstruction!=null||completed==0?2:invalid!=0?1:0;
    }
}
