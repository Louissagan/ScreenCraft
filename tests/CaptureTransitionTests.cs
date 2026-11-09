using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenCraft
{
    static class CaptureTransitionTests
    {
        sealed class Picker:RegionPicker
        {
            static Dib Pixels(){var pixels=new Dib(480,320);using(var g=pixels.Graphics())g.Clear(Color.FromArgb(48,125,190));return pixels;}
            internal Picker():base(Pixels(),new Rectangle(0,0,480,320),new List<Grabber.Win>(),Mode.Image,false,false){ModifierSource=delegate{return Keys.None;};}
            internal bool Opaque{get{return GetStyle(ControlStyles.Opaque);}}
            internal void Select(){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,80,80,0));OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,300,220,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,300,220,0));}
        }
        internal static void Run(Action<string,Action> run,Action<bool,string> check)
        {
            run("capture transition/prepare sets a per-window DWM transition override while hidden",delegate{
                using(var picker=new Picker()){var handle=picker.Handle;check(CaptureDisplay.Prepare(handle)==0,"DWM rejected picker transition override");check(picker.AutoScaleMode==AutoScaleMode.None&&picker.Bounds==new Rectangle(0,0,480,320)&&picker.Opaque,"physical bounds or opaque painting not retained");check(!picker.Visible,"preparing showed picker");}
                using(var overlay=new CaptureOverlay(new Bitmap(480,320),new Rectangle(0,0,480,320),new Rectangle(80,80,220,140),new[]{new Rectangle(0,0,480,320)})){var handle=overlay.Handle;check(CaptureDisplay.Prepare(handle)==0&&!overlay.Visible,"editor transition or visibility changed");}
            });
            run("capture transition/old surface retires only after drawing and compositor synchronization",delegate{
                var order=new List<string>();CaptureDisplay.PresentThenRetire(delegate{order.Add("paint");},delegate{order.Add("present");},delegate{order.Add("retire");});check(string.Join(",",order.ToArray())=="paint,present,retire","owner retired before successor pixels");
                bool retired=false;try{CaptureDisplay.PresentThenRetire(delegate{throw new InvalidOperationException("paint fixture");},delegate{},delegate{retired=true;});}catch(InvalidOperationException){}check(!retired,"paint failure exposed desktop");
                try{CaptureDisplay.PresentThenRetire(delegate{},delegate{throw new InvalidOperationException("composition fixture");},delegate{retired=true;});}catch(InvalidOperationException){}check(!retired,"presentation failure exposed desktop");
            });
            run("capture transition/first full raster exists before HWND creation and paint only copies it",delegate{
                using(var picker=new Picker()){check(!picker.IsHandleCreated&&picker.PreparedRevision==1,"first image is deferred until show");int revision=picker.PreparedRevision;for(int i=0;i<10;i++)using(var image=UiPaintingTests.NativePaint(picker,Color.Magenta))check(image.GetPixel(250,150).B==190,"prepared source missing");check(picker.PreparedRevision==revision&&!picker.Visible,"WM_PAINT recomposed or showed a window");}
                using(var overlay=new CaptureOverlay(new Bitmap(480,320),new Rectangle(0,0,480,320),new Rectangle(80,80,220,140),new[]{new Rectangle(0,0,480,320)}))check(overlay.FirstFramePrepared&&!overlay.Visible,"annotation frame is deferred until show");
            });
            run("capture transition/accepted annotation backing retains final selection instead of reverting to hover",delegate{
                using(var picker=new Picker()){
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(RegionPicker).GetField("annotationHandoff",flags).SetValue(picker,true);
                    bool visited=false;Action<Bitmap,Rectangle,Rectangle,IWin32Window> edit=delegate(Bitmap image,Rectangle desktop,Rectangle selection,IWin32Window owner){using(image)using(var rendered=UiPaintingTests.NativePaint(picker,Color.Magenta)){visited=true;var inside=rendered.GetPixel(130,120);var outside=rendered.GetPixel(410,270);check(inside.B==190&&outside.B<80,"accepted surface reverted to full-screen hover");}};
                    typeof(RegionPicker).GetField("annotationEditor",flags).SetValue(picker,edit);picker.Select();check(visited,"annotation handoff not reached");var error=(Exception)typeof(RegionPicker).GetField("annotationError",flags).GetValue(picker);if(error!=null)throw error;
                }
            });
            run("capture transition/Alt+A remains a valid single global chord",delegate{uint modifiers;Keys key;check(Hotkeys.TryParse("Alt+A",out modifiers,out key)&&modifiers==1&&key==Keys.A,"user shortcut no longer accepted");});
        }
    }
}
