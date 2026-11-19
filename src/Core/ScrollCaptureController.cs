using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScreenCraft
{
    public static class ScrollCapture
    {
        static ScrollSession current;
        public static event Action Completed;
        public static bool Active{get{return current!=null;}}
        public static void Toggle(CaptureHost owner,Settings settings)
        {
            if(current!=null){current.Finish();return;}Bitmap frozen;Rectangle bounds;IntPtr window;var area=RegionPicker.Pick(RegionPicker.Mode.Scroll,out frozen,out bounds,out window);if(frozen!=null)frozen.Dispose();Start(owner,area);
        }
        public static void Start(CaptureHost owner,Rectangle area)
        {
            if(current!=null||area.IsEmpty)return;if(area.Width<40||area.Height<40){owner.Notify("滚动截图","请选择至少 40 × 40 像素的区域。");return;}
            var session=new ScrollSession(owner,area);current=session;try{session.Start();}catch{session.AbortStart();Ended(session);throw;}
        }
        public static void Stop(){if(current!=null)current.Cancel();}
        internal static void Ended(ScrollSession session){if(current!=session)return;current=null;var handler=Completed;if(handler!=null)handler();}
    }
    public sealed class ScrollSession
    {
        readonly CaptureHost owner;readonly Rectangle area;readonly ScrollFrameProcessor processor;
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer poll=new System.Windows.Forms.Timer{Interval=80};
        ScrollCapturePanel panel;Thread worker;
        volatile bool stopping,cancelled,automatic;
        volatile Stitcher.Result lastResult;
        int stopped,lastGrowth,autoStalls;
        long nextWheel,escapeAt=-5000,finishAt=-5000;
        bool escapeDown,enterDown;
        Point? savedCursor;
        Point autoPoint;
        public ScrollSession(CaptureHost host,Rectangle bounds){owner=host;area=bounds;processor=new ScrollFrameProcessor(bounds.Width,bounds.Height,Math.Min(16,bounds.Width/20),30000);poll.Tick+=Poll;}
        public int Height{get{return processor.Stitcher.Height;}}
        public bool Auto{get{return automatic;}}
        public bool Full{get{return processor.Stitcher.IsFull;}}
        public bool Losing{get{return lastResult==Stitcher.Result.Lost;}}
        public bool WentUp{get{return lastResult==Stitcher.Result.Up;}}
        public bool EscArmed{get{return clock.ElapsedMilliseconds-escapeAt<1600;}}
        public bool DoneArmed{get{return clock.ElapsedMilliseconds-finishAt<1600;}}
        public int AreaHeight{get{return area.Height;}}
        public bool Stuck{get{return autoStalls>=3;}}
        public void Start()
        {
            panel=new ScrollCapturePanel(this,area);panel.Show();worker=new Thread(Collect){IsBackground=true,Name="ScreenCraft scroll collector"};worker.SetApartmentState(ApartmentState.MTA);worker.Start();poll.Start();
        }
        internal void AbortStart(){cancelled=stopping=true;poll.Stop();poll.Dispose();if(panel!=null)panel.Dispose();if(worker==null||!worker.IsAlive)processor.Dispose();}
        public void Finish(){if(stopping)return;if(Height<=area.Height&&!DoneArmed){finishAt=clock.ElapsedMilliseconds;if(panel!=null)panel.RefreshStatus("还没有向下滚动，再次完成将保留当前画面。");return;}automatic=false;stopping=true;}
        public void Cancel(){cancelled=true;automatic=false;stopping=true;}
        public void ToggleAuto()
        {
            if(stopping)return;automatic=!automatic;if(automatic){if(!savedCursor.HasValue)savedCursor=Cursor.Position;autoPoint=new Point(area.Left+area.Width/2,area.Top+area.Height/2);Cursor.Position=autoPoint;nextWheel=clock.ElapsedMilliseconds+250;autoStalls=0;lastGrowth=Height;}if(panel!=null)panel.RefreshStatus(null);
        }
        [DllImport("user32.dll")] static extern void mouse_event(uint flags,uint dx,uint dy,int data,UIntPtr extra);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        void Poll(object sender,EventArgs e)
        {
            if(stopping)return;bool escape=(GetAsyncKeyState(27)&0x8000)!=0,enter=(GetAsyncKeyState(13)&0x8000)!=0;
            if(escape&&!escapeDown){if(Height>area.Height&&!EscArmed){escapeAt=clock.ElapsedMilliseconds;panel.RefreshStatus("再次按 Esc 放弃已采集内容；按 Enter 保留。");}else Cancel();}if(enter&&!enterDown)Finish();escapeDown=escape;enterDown=enter;
            if(automatic){var cursor=Cursor.Position;if(Math.Abs(cursor.X-autoPoint.X)>4||Math.Abs(cursor.Y-autoPoint.Y)>4){automatic=false;panel.RefreshStatus("已暂停自动滚动。");}
                else if(clock.ElapsedMilliseconds>=nextWheel){if(Height==lastGrowth)autoStalls++;else autoStalls=0;lastGrowth=Height;if(Full||autoStalls>=4){Finish();if(!stopping)Finish();}else{mouse_event(0x800,0,0,-120,UIntPtr.Zero);nextWheel=clock.ElapsedMilliseconds+650;}}}
            panel.RefreshStatus(null);
        }
        static bool Same(int[] first,int[] second){long sum=0;int count=0;for(int i=0;i<first.Length;i+=Math.Max(1,first.Length/4000)){int a=first[i],b=second[i];sum+=Math.Abs((a&255)-(b&255))+Math.Abs(((a>>8)&255)-((b>>8)&255))+Math.Abs(((a>>16)&255)-((b>>16)&255));count++;}return sum<=count*2;}
        void Collect()
        {
            string error=null,message=null;Bitmap output=null;
            try{Thread.Sleep(160);using(var source=new GdiScrollFrameSource(area)){
                int[] pixels=new int[area.Width*area.Height],verify=new int[pixels.Length];
                while(!stopping){source.Read(pixels);Thread.Sleep(40);if(stopping)break;source.Read(verify);if(Same(pixels,verify)){Stitcher.Result result;int added;processor.Push(ref verify,out result,out added);lastResult=result;if(Full)stopping=true;}Thread.Sleep(60);}
            }}catch(Exception ex){error=ex.Message;}
            try{output=processor.Complete(cancelled,error,out message);}catch(Exception ex){message="滚动截图无法导出："+ex.Message;}finally{processor.Dispose();}
            var image=output;var notice=message;owner.Ui(delegate{if(Interlocked.Exchange(ref stopped,1)!=0){if(image!=null)image.Dispose();return;}poll.Stop();poll.Dispose();if(panel!=null){panel.AllowClose=true;panel.Close();panel.Dispose();}if(savedCursor.HasValue&&Cursor.Position==autoPoint)Cursor.Position=savedCursor.Value;
                ScrollCapture.Ended(this);if(notice!=null)owner.Notify("滚动截图",notice);if(image!=null)owner.SaveCapture(image,"ScreenCraft-scroll");});
        }
    }
    internal sealed class ScrollCapturePanel:Form
    {
        readonly ScrollSession session;readonly Label status=new Label{AutoSize=false,TextAlign=ContentAlignment.MiddleLeft};readonly PreferencesButton automatic=new PreferencesButton{Text="自动滚动",Width=90};
        internal bool AllowClose;
        internal ScrollCapturePanel(ScrollSession collector,Rectangle area)
        {
            session=collector;Text="ScreenCraft · 滚动截图";Icon=CaptureHost.AppIcon;FormBorderStyle=FormBorderStyle.FixedToolWindow;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;ClientSize=new Size(540,90);Font=new Font("Microsoft YaHei UI",9);BackColor=PreferencesLook.Background;
            var done=new PreferencesButton{Text="完成 Enter",Primary=true,Width=100};var cancel=new PreferencesButton{Text="取消 Esc",Width=90};done.Click+=delegate{session.Finish();};cancel.Click+=delegate{session.Cancel();};automatic.Click+=delegate{session.ToggleAuto();};
            status.SetBounds(14,8,512,28);automatic.Location=new Point(14,46);cancel.Location=new Point(322,46);done.Location=new Point(424,46);Controls.AddRange(new Control[]{status,automatic,cancel,done});var work=Screen.FromRectangle(area).WorkingArea;Location=new Point(Math.Max(work.Left,Math.Min(work.Right-Width,area.Right-Width)),area.Bottom+Height+8<work.Bottom?area.Bottom+8:Math.Max(work.Top,area.Top-Height-8));RefreshStatus(null);
        }
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.SetWindowDisplayAffinity(Handle,0x11);}
        internal void RefreshStatus(string message){status.Text=message??(session.Losing?"未找到可靠重叠，请向上退回一点后缓慢向下滚动。":session.WentUp?"已暂停拼接，请回到上次位置后继续向下。":"已采集 "+session.Height+" 像素 · 在所选区域向下滚动");automatic.Text=session.Auto?"暂停自动":"自动滚动";}
        protected override void OnFormClosing(FormClosingEventArgs e){if(!AllowClose){e.Cancel=true;session.Cancel();}base.OnFormClosing(e);}
    }
}
