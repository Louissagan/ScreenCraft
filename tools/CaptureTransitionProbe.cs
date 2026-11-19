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
    [DllImport("user32.dll")] static extern bool GetWindowDisplayAffinity(IntPtr window,out uint affinity);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr window,int attribute,out int value,int size);
    static readonly Color source=Color.FromArgb(48,66,84),live=Color.FromArgb(72,62,52),dim=Color.FromArgb(14,20,25);
    static Assembly application;static string prefix;static volatile bool done;static volatile string phase="fixture";
    static int frames,invalid,excluded;static string obstruction;static Exception failure;
    static bool verifyGate,verifyAffinity;
    static int completed,copies;static bool pixelProbeEnabled;
    static readonly List<string> invalidSamples=new List<string>();
    static readonly List<string> events=new List<string>();static readonly Stopwatch clock=Stopwatch.StartNew();
    static BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Event(string value){events.Add(clock.ElapsedMilliseconds+" "+value);}
    static Type Type(string name){return application.GetType(prefix+"."+name,true);}
    static void Mouse(Form picker,int message,int x,int y){SendMessage(picker.Handle,message,message==0x202?IntPtr.Zero:new IntPtr(1),new IntPtr((y<<16)|(x&65535)));}
    static bool OwnsSample(Point point)
    {
        uint process;GetWindowThreadProcessId(WindowFromPoint(point),out process);return process==(uint)Process.GetCurrentProcess().Id;
    }
    static bool Near(Color a,Color b){return Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B)<18;}
    static void Record(Rectangle rectangle)
    {
        try{using(var pixels=new Bitmap(rectangle.Width,1))using(var graphics=Graphics.FromImage(pixels)){
            while(!done){
                var top=GetAncestor(WindowFromPoint(new Point{X=rectangle.Left+30,Y=rectangle.Top+40}),2);uint affinity;
                if(GetWindowDisplayAffinity(top,out affinity)&&affinity!=0){excluded++;Thread.Sleep(4);continue;}
                int bad=0;
                for(int i=0;i<5;i++)if(!OwnsSample(new Point{X=rectangle.Left+rectangle.Width*(1+2*i)/10,Y=rectangle.Top+40})){obstruction="Fixture obscured";return;}
                graphics.CopyFromScreen(rectangle.Left,rectangle.Top+40,0,0,pixels.Size,CopyPixelOperation.SourceCopy);
                for(int i=0;i<5;i++){
                    int x=rectangle.Width*(1+2*i)/10;var point=new Point{X=rectangle.Left+x,Y=rectangle.Top+40};
                    if(!OwnsSample(point)){obstruction="Sample ownership changed";return;}
                    var color=pixels.GetPixel(x,0);if(!Near(color,source)&&!Near(color,live)&&!Near(color,dim))bad++;
                }
                frames++;if(bad>=4){invalid++;invalidSamples.Add(clock.ElapsedMilliseconds+" phase="+phase+" pixel="+pixels.GetPixel(rectangle.Width/2,0));}Thread.Sleep(4);
            }
        }}catch(Exception ex){failure=ex;}
    }
    static void Observe(Form window,string name,bool stall)
    {
        window.VisibleChanged+=delegate{
            if(!window.Visible)return;int cloak;DwmGetWindowAttribute(window.Handle,14,out cloak,4);
            Event(name+" became visible, cloak="+cloak);
            if(verifyGate&&(cloak&1)==0)failure=new Exception(name+" exposed its initial HWND");
            if(stall)Thread.Sleep(90);
        };
        window.Shown+=delegate{
            int cloak;DwmGetWindowAttribute(window.Handle,14,out cloak,4);
            if(cloak!=0)failure=new Exception(name+" still cloaked after first frame");
            uint affinity;if(verifyAffinity&&(!GetWindowDisplayAffinity(window.Handle,out affinity)||affinity!=0x11))failure=new Exception(name+" lost capture exclusion");
        };
        window.FormClosed+=delegate{if(window.IsHandleCreated){int cloak;DwmGetWindowAttribute(window.Handle,14,out cloak,4);Event(name+" closing, cloak="+cloak);if(verifyGate&&(cloak&1)==0)failure=new Exception(name+" exposed its retiring HWND");}};
    }
    static void CheckDisplayPixel(int x,int y,Color expected)
    {
        if(!pixelProbeEnabled)return;
        if(!OwnsSample(new Point{X=x,Y=y}))throw new InvalidOperationException("Fixture obscured");
        using(var b=new Bitmap(1,1))using(var g=Graphics.FromImage(b)){
            g.CopyFromScreen(x,y,0,0,b.Size);
            if(!Near(b.GetPixel(0,0),expected))throw new InvalidOperationException("Steady display pixel differed from "+expected);
        }
    }
    static Button Done(Control parent)
    {
        foreach(Control child in parent.Controls){if(child.AccessibleName=="完成并复制 Enter")return (Button)child;var match=Done(child);if(match!=null)return match;}
        return null;
    }
    [STAThread] static int Main(string[] arguments)
    {
        if(arguments.Length<2)return 2;Directory.CreateDirectory(arguments[1]);SetProcessDpiAwarenessContext(new IntPtr(-4));Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        application=Assembly.LoadFrom(Path.GetFullPath(arguments[0]));prefix=application.GetType("ScreenCraft.RegionPicker")!=null?"ScreenCraft":"Stackshot";Type("Theme").GetMethod("Init").Invoke(null,null);
        bool stall=Array.IndexOf(arguments,"--stall")>=0,protectedRun=Array.IndexOf(arguments,"--protected")>=0;
        verifyGate=application.GetType(prefix+".CaptureSurfaceForm")!=null;verifyAffinity=protectedRun;
        var display=application.GetType(prefix+".CaptureDisplay");if(display!=null&&!protectedRun){var exclusion=display.GetField("ExcludeFromCapture",BindingFlags.NonPublic|BindingFlags.Static);if(exclusion!=null){exclusion.SetValue(null,(Action<IntPtr>)delegate{});pixelProbeEnabled=true;}}
        Rectangle work=Screen.PrimaryScreen.WorkingArea;
        var bounds=Array.IndexOf(arguments,"--fullscreen")>=0?Screen.PrimaryScreen.Bounds:new Rectangle(work.Left+(work.Width-800)/2,work.Top+(work.Height-500)/2,800,500);int returnedVisible=0;
        using(var fixture=new Form{FormBorderStyle=FormBorderStyle.None,StartPosition=FormStartPosition.Manual,AutoScaleMode=AutoScaleMode.None,Bounds=bounds,BackColor=source,TopMost=true,ShowInTaskbar=false,Text="ScreenCraft synthetic transition probe"}){
            var handle=fixture.Handle;int disable=1;DwmSetWindowAttribute(handle,3,ref disable,4);fixture.Show();fixture.Refresh();DwmFlush();
            Thread recorder=null;if(pixelProbeEnabled){recorder=new Thread(delegate(){Record(bounds);}){IsBackground=true};recorder.Start();}
            try{for(int cycle=0;cycle<3;cycle++){
                fixture.BackColor=source;fixture.Refresh();DwmFlush();Thread.Sleep(60);
                var dib=Activator.CreateInstance(Type("Dib"),new object[]{bounds.Width,bounds.Height});using(var g=(Graphics)Type("Dib").GetMethod("Graphics").Invoke(dib,null))g.Clear(source);
                var list=Activator.CreateInstance(typeof(List<>).MakeGenericType(Type("Grabber+Win")));var mode=Enum.Parse(Type("RegionPicker+Mode"),"Image");
                using(var picker=(Form)Activator.CreateInstance(Type("RegionPicker"),Hidden,null,new object[]{dib,bounds,list,mode,false,false},null)){
                    Observe(picker,"picker",stall);var modifiers=Type("RegionPicker").GetField("ModifierSource",Hidden);if(modifiers!=null)modifiers.SetValue(picker,(Func<Keys>)delegate{return Keys.None;});
                    Type("RegionPicker").GetField("annotationHandoff",Hidden).SetValue(picker,true);picker.Activated+=delegate{Event("picker activated / "+phase);};
                    Action<Bitmap,Rectangle,Rectangle,IWin32Window> annotate=delegate(Bitmap image,Rectangle desktop,Rectangle selection,IWin32Window owner){
                        using(var editor=(Form)Activator.CreateInstance(Type("CaptureOverlay"),new object[]{image,desktop,selection,new[]{bounds},null,70,null})){
                            Observe(editor,"editor",stall);
                            Type("CaptureOverlay").GetField("CopyOutput",Hidden).SetValue(editor,(Action<Bitmap>)delegate(Bitmap result){
                                if(result.Size!=selection.Size||!Near(result.GetPixel(result.Width/2,result.Height/2),source))throw new InvalidDataException("Unexpected capture output");
                                copies++;
                            });
                            Type("CaptureOverlay").GetField("CommandError",Hidden).SetValue(editor,(Action<string>)delegate(string message){failure=new Exception(message);editor.Close();});
                            var editorHandle=editor.Handle;var close=new System.Windows.Forms.Timer{Interval=260};close.Tick+=delegate{
                                close.Stop();CheckDisplayPixel(bounds.Left+180,bounds.Top+150,source);phase="confirm";Done(editor).PerformClick();
                            };
                            editor.Shown+=delegate{phase="editor";Event("editor shown");fixture.BackColor=live;fixture.Refresh();close.Start();};
                            var show=Type("CaptureOverlay").GetMethod("ShowForCapture",Hidden);if(show==null)editor.ShowDialog(owner);else show.Invoke(editor,new object[]{owner});close.Dispose();
                            completed++;if(picker.Visible)returnedVisible++;int cloak;DwmGetWindowAttribute(picker.Handle,14,out cloak,4);
                            if(verifyGate&&(cloak&1)==0)failure=new Exception("Retired picker exposed during modal return");
                            Event("editor returned / picker visible="+picker.Visible+", cloak="+cloak);phase="return";
                        }
                    };
                    Type("RegionPicker").GetField("annotationEditor",Hidden).SetValue(picker,annotate);var pickerHandle=picker.Handle;
                    var select=new System.Windows.Forms.Timer{Interval=180};select.Tick+=delegate{select.Stop();phase="selection";Mouse(picker,0x201,80,80);Mouse(picker,0x200,580,300);Mouse(picker,0x202,580,300);};
                    picker.Shown+=delegate{phase="picker";Event("picker shown");select.Start();};picker.ShowDialog(fixture);select.Dispose();Application.DoEvents();DwmFlush();Thread.Sleep(80);CheckDisplayPixel(bounds.Left+180,bounds.Top+150,live);
                }
            }}catch(Exception ex){failure=ex;}finally{done=true;if(recorder!=null)recorder.Join(5000);fixture.Close();}
        }
        var text="Version="+FileVersionInfo.GetVersionInfo(arguments[0]).FileVersion+"\r\nBounds="+bounds+"\r\nStressDelay="+stall+"\r\nPixelProbeEnabled="+pixelProbeEnabled+"\r\nCompletedHandoffs="+completed+"\r\nConfirmedCopies="+copies+"\r\nFrames="+frames+"\r\nExcludedSamples="+excluded+"\r\nUnexpectedColorFrames="+invalid+"\r\nPickerVisibleAfterEditor="+returnedVisible+"\r\nObstruction="+obstruction+"\r\nFailure="+failure+"\r\n"+string.Join("\r\n",events.ToArray())+"\r\n"+string.Join("\r\n",invalidSamples.ToArray());File.WriteAllText(Path.Combine(arguments[1],"transition-probe.txt"),text);Console.WriteLine(text);return failure!=null||obstruction!=null||completed!=3||copies!=completed||returnedVisible!=0||pixelProbeEnabled&&frames==0?2:invalid!=0?1:0;
    }
}
