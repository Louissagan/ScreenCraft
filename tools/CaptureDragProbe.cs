using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenCraft;

// Opt-in native regression probe. Input is directed only to the synthetic overlay in this process.
class CaptureDragProbe {
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, int m, IntPtr w, IntPtr l);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point pt);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd, int type);
    static void Mouse(Canvas c, int msg, int x, int y) {
        SendMessage(c.Handle,msg,new IntPtr(msg==0x202?0:1),new IntPtr((y<<16)|(x&65535)));
    }
    static Bitmap Pic(CaptureOverlay o){
        Application.DoEvents(); DwmFlush();
        var origin=o.PointToScreen(Point.Empty);
        foreach(var offset in new[]{new Point(1,1),new Point(o.Width-2,1),new Point(o.Width/2,o.Height/2),new Point(1,o.Height-2),new Point(o.Width-2,o.Height-2)}){
            var at=origin+new Size(offset);
            if(GetAncestor(WindowFromPoint(at),2)!=o.Handle)throw new Exception("Test window obscured, skipping capture");
        }
        var b=new Bitmap(o.ClientSize.Width,o.ClientSize.Height,PixelFormat.Format24bppRgb);
        using(var g=Graphics.FromImage(b))g.CopyFromScreen(origin,Point.Empty,b.Size);
        return b;
    }
    static int Differences(Bitmap a, Bitmap b){
        int different=0;
        for(int y=0;y<a.Height;y++)
            for(int x=0;x<a.Width;x++)
                if(a.GetPixel(x,y)!=b.GetPixel(x,y))different++;
        return different;
    }
    [STAThread] static int Main(string[] args){
        try{return Run(args);}catch(Exception ex){Console.Error.WriteLine("Probe stopped: "+ex.Message);return 2;}
    }
    static int Run(string[] args){
        if(args.Length!=1){Console.Error.WriteLine("Supply the local output directory for synthetic test frames.");return 2;}
        Directory.CreateDirectory(args[0]);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        var bounds=new Rectangle(Screen.PrimaryScreen.WorkingArea.X+30,Screen.PrimaryScreen.WorkingArea.Y+40,960,540);
        var img=new Bitmap(bounds.Width,bounds.Height);
        using(var g=Graphics.FromImage(img))using(var fill=new LinearGradientBrush(new Rectangle(Point.Empty,img.Size),Color.FromArgb(204,225,232),Color.FromArgb(228,221,203),15f)){
            g.FillRectangle(fill,0,0,img.Width,img.Height);
            using(var p=new Pen(Color.FromArgb(112,139,148),2)){g.DrawRectangle(p,112,76,325,172);g.DrawLine(p,112,130,437,130);g.DrawLine(p,222,77,222,248);}
        }
        var range=new Rectangle(bounds.X+80,bounds.Y+65,660,220);
        using(var o=new CaptureOverlay(img,bounds,range,new[]{bounds})){
            o.Show(); var c=(Canvas)o.Controls[0]; Native.SetWindowDisplayAffinity(o.Handle,0); Application.DoEvents();
            using(var initial=Pic(o)){initial.Save(Path.Combine(args[0],"initial.png"));}
            int x=740,y=285;
            Mouse(c,0x201,x,y);
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<120;i++){
                x=265+((i*53)%605);y=155+((i*37)%260);Mouse(c,0x200,x,y);
                if(i%12==11)Application.DoEvents();
            }
            int duringMismatch;
            long ms;
            using(var during=Pic(o)){
                during.Save(Path.Combine(args[0],"drag.png"));
                ms=watch.ElapsedMilliseconds;
                c.Refresh();
                using(var fresh=Pic(o))duringMismatch=Differences(during,fresh);
            }
            Mouse(c,0x202,770,305);
            using(var after=Pic(o)){after.Save(Path.Combine(args[0],"released.png"));
                o.Refresh();c.Refresh();Application.DoEvents();
                using(var repainted=Pic(o)){
                    int changed=Differences(after,repainted);
                    Console.WriteLine("120 moves="+ms+" ms; final crop="+c.Crop+"; changed pixels after explicit repaint="+changed+"; mid-drag repaint difference="+duringMismatch+"; monitor scale="+CaptureHost.ScaleFor(Screen.FromControl(o)));
                    if(c.Crop!=new Rectangle(80,65,690,240)||duringMismatch!=0||changed!=0)return 1;
                }
            }
        }
        return 0;
    }
}
