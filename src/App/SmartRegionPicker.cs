using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ScreenCraft
{
    public partial class RegionPicker
    {
        readonly Dictionary<IntPtr,Rectangle[]> smartNative=new Dictionary<IntPtr,Rectangle[]>(),smartAutomation=new Dictionary<IntPtr,Rectangle[]>();
        Timer smartTimer;Rectangle[] smartChoices=new Rectangle[0];Rectangle smartWindowBounds;Point smartPoint;IntPtr smartWindow;int smartVersion,smartIndex,smartPass;bool smartWheel;
        Rectangle SmartHover(Rectangle window,IntPtr handle,Point point)
        {
            bool moved=smartPoint!=point||smartWindow!=handle;smartPoint=point;smartWindow=handle;smartWindowBounds=window;if(moved){smartIndex=0;smartWheel=false;smartPass=0;smartVersion++;}
            Rectangle[] native,automation;if(!smartNative.TryGetValue(handle,out native)){native=SmartRegions.NativeRegions(handle);smartNative[handle]=native;}if(!smartAutomation.TryGetValue(handle,out automation))automation=new Rectangle[0];
            var monitor=curMon;monitor.Offset(vs.X,vs.Y);smartChoices=SmartRegions.At(point,window,monitor,vs,native.Concat(automation));smartIndex=Math.Min(smartIndex,Math.Max(0,smartChoices.Length-1));
            if(smartTimer==null){smartTimer=new Timer{Interval=90};smartTimer.Tick+=delegate{smartTimer.Stop();RequestSmartRegions();};}smartTimer.Stop();if(IsHandleCreated&&!down&&!dragging&&!adjusting)smartTimer.Start();
            return smartChoices.Length==0?window:smartChoices[smartIndex];
        }
        void RequestSmartRegions()
        {
            if(IsDisposed||down||dragging||adjusting||smartWindow==IntPtr.Zero)return;smartPass++;int version=smartVersion;var point=smartPoint;var handle=smartWindow;
            SmartRegionWorker.Request(delegate{
                var boxes=SmartRegions.AutomationRegions(handle,point);
                if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke((Action)delegate{
                    if(IsDisposed||version!=smartVersion||point!=smartPoint||handle!=smartWindow||down||dragging||adjusting||measuring)return;
                    var prior=smartChoices.Length>0?smartChoices[smartIndex]:Rectangle.Empty;smartAutomation[handle]=boxes;var monitor=curMon;monitor.Offset(vs.X,vs.Y);
                    smartChoices=SmartRegions.At(point,smartWindowBounds,monitor,vs,smartNative[handle].Concat(boxes));smartIndex=0;if(smartWheel){int at=Array.IndexOf(smartChoices,prior);if(at>=0)smartIndex=at;}
                    ApplySmartChoice();
                    if(smartPass<2&&smartTimer!=null)smartTimer.Start();
                });}catch(InvalidOperationException){}
            });
        }
        void ApplySmartChoice()
        {
            if(smartChoices.Length==0)return;var screen=smartChoices[smartIndex];var local=screen;local.Offset(-vs.X,-vs.Y);hover=local;hoverHandle=screen==smartWindowBounds?smartWindow:IntPtr.Zero;Present();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);if(down||dragging||adjusting||measuring||loupe||smartChoices.Length<2||e.Delta==0)return;
            smartIndex=Math.Max(0,Math.Min(smartChoices.Length-1,smartIndex+(e.Delta>0?1:-1)));smartWheel=true;ApplySmartChoice();
        }
        void ClearSmartHover(){smartVersion++;smartWindow=IntPtr.Zero;smartChoices=new Rectangle[0];if(smartTimer!=null)smartTimer.Stop();}
        void DisposeSmartRegions(){ClearSmartHover();if(smartTimer!=null){smartTimer.Dispose();smartTimer=null;}}
        protected override void Dispose(bool disposing){if(disposing){DisposeSmartRegions();ReleasePixels();}base.Dispose(disposing);}
    }
}
