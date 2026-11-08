// Opt-in visible native probe: captures only the startup toast created by this process.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
class StartupToastProbe
{
    [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point p);
    [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr hwnd,int flag);
    [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")]static extern int DwmFlush();
    [STAThread]static void Main(string[] args)
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
        var type=assembly.GetType("Stackshot.StartupToast",true);
        using(var form=(Form)Activator.CreateInstance(type,BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Screen.PrimaryScreen.WorkingArea,"Alt+A",false},null)){
            form.Show();Application.DoEvents();
            ((Timer)type.GetField("timer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form)).Stop();
            var advance=type.GetMethod("Advance",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(int ms in new[]{0,90,180,2500}){
                advance.Invoke(form,new object[]{TimeSpan.FromMilliseconds(ms)});form.Refresh();Application.DoEvents();DwmFlush();
                var origin=form.PointToScreen(Point.Empty);
                var center=new Point(origin.X+form.Width/2,origin.Y+form.Height/2);
                if(GetAncestor(WindowFromPoint(center),2)!=form.Handle)throw new InvalidOperationException("Toast obscured; desktop capture skipped.");
                using(var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height,PixelFormat.Format24bppRgb))using(var g=Graphics.FromImage(image)){
                    g.CopyFromScreen(origin,Point.Empty,image.Size);image.Save(Path.Combine(output,"toast-"+ms+".png"));
                }
            }
            File.WriteAllText(Path.Combine(output,"visible-toast.txt"),"Version="+assembly.GetName().Version+"\r\nDPI="+GetDpiForWindow(form.Handle)+"\r\nClient="+form.ClientSize+"\r\nFont="+form.Font+"\r\nOpacity@2500="+form.Opacity+"\r\nSynthetic toast only; existing tray process untouched.");
        }
    }
}
