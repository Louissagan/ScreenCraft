using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
namespace ScreenCraft
{
    internal static class SmartRegions
    {
        [DllImport("user32.dll")]static extern bool EnumChildWindows(IntPtr parent,Native.EnumWindowsProc callback,IntPtr data);
        [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr window,ref Native.POINT point);
        internal static Rectangle[] At(Point point,Rectangle window,Rectangle monitor,Rectangle desktop,IEnumerable<Rectangle> detected)
        {
            var regions=new List<Rectangle>();foreach(var box in detected.Concat(new[]{window,monitor})){
                var r=Rectangle.Intersect(box,desktop);if(box!=monitor)r=Rectangle.Intersect(r,window);
                if(r.Width>=12&&r.Height>=12&&r.Contains(point)&&!regions.Contains(r))regions.Add(r);
            }
            return regions.OrderBy(r=>(long)r.Width*r.Height).ToArray();
        }
        internal static Rectangle[] NativeRegions(IntPtr window)
        {
            var regions=new List<Rectangle>();Native.RECT client;var origin=new Native.POINT();
            if(Native.GetClientRect(window,out client)&&ClientToScreen(window,ref origin))regions.Add(new Rectangle(origin.X,origin.Y,client.Right-client.Left,client.Bottom-client.Top));
            int count=0;EnumChildWindows(window,delegate(IntPtr child,IntPtr data){if(++count>512)return false;if(Native.IsWindowVisible(child)){Native.RECT r;if(Native.GetWindowRect(child,out r))regions.Add(Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom));}return true;},IntPtr.Zero);return regions.ToArray();
        }
        internal static Rectangle[] AutomationRegions(IntPtr window,Point point)
        {
            var boxes=new List<Rectangle>();var element=AutomationElement.FromHandle(window);var walker=TreeWalker.ControlViewWalker;var watch=System.Diagnostics.Stopwatch.StartNew();int visited=0;
            // Read rectangles only. Never request names, text, values or control patterns.
            for(int depth=0;element!=null&&depth<24&&watch.ElapsedMilliseconds<1200;depth++){
                AutomationElement best=null;long area=long.MaxValue;
                for(var child=walker.GetFirstChild(element);child!=null&&visited++<384&&watch.ElapsedMilliseconds<1200;child=walker.GetNextSibling(child)){
                    var info=child.Current;var b=info.BoundingRectangle;if(info.IsOffscreen||b.IsEmpty||!Finite(b.X)||!Finite(b.Y)||!Finite(b.Width)||!Finite(b.Height)||b.Width<12||b.Height<12||b.Width>100000||b.Height>100000)continue;
                    var r=Rectangle.FromLTRB((int)Math.Floor(b.Left),(int)Math.Floor(b.Top),(int)Math.Ceiling(b.Right),(int)Math.Ceiling(b.Bottom));if(!r.Contains(point))continue;
                    if(info.ControlType!=ControlType.Text)boxes.Add(r);long size=(long)r.Width*r.Height;if(size<area){area=size;best=child;}
                }
                element=best;
            }
            return boxes.ToArray();
        }
        static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value)&&Math.Abs(value)<1000000;}
    }
    // One process-wide MTA worker and one latest request: a stalled provider never blocks the picker or creates more threads.
    internal static class SmartRegionWorker
    {
        static readonly object gate=new object();static readonly AutoResetEvent signal=new AutoResetEvent(false);static Action pending;static bool started;
        internal static void Request(Action query){lock(gate){pending=query;if(!started){started=true;var thread=new Thread(Run){IsBackground=true,Name="ScreenCraft region detection"};thread.SetApartmentState(ApartmentState.MTA);thread.Start();}}signal.Set();}
        static void Run(){while(true){signal.WaitOne();Action query;lock(gate){query=pending;pending=null;}if(query!=null)try{query();}catch{/* Native windows and manual selection remain available. */}}}
    }
}
