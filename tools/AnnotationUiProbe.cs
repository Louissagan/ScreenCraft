// Opt-in visible test of self-owned synthetic windows; no desktop content is used.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenCraft;
class AnnotationUiProbe
{
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,string l);
    [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr hwnd);
    static string output;
    static System.Collections.Generic.IEnumerable<Control> Children(Control c){foreach(Control child in c.Controls){yield return child;foreach(var descendant in Children(child))yield return descendant;}}
    static void Click(Control b){typeof(Control).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,new object[]{EventArgs.Empty});Application.DoEvents();}
    static object Field(object o,string name){return o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
    static void Snap(Control c,string name){c.Refresh();Application.DoEvents();using(var b=new Bitmap(c.ClientSize.Width,c.ClientSize.Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b)){var dc=g.GetHdc();bool ok;try{ok=PrintWindow(c.Handle,dc,3);}finally{g.ReleaseHdc(dc);}if(!ok)throw new Exception("PrintWindow failed: "+name);b.Save(Path.Combine(output,name+".png"));}}
    static void Mouse(Canvas c,int msg,int x,int y){SendMessage(c.Handle,msg,new IntPtr(msg==0x202?0:1),new IntPtr((y<<16)|(x&65535)));}
    static Shape Mark(Tool t,int x,int y,int bx,int by,Color color,string text){return new Shape{Kind=t,A=new PointF(x,y),B=new PointF(bx,by),Color=color,Text=text,FontPx=22,NoteFontPx=22,MarkerSize=32,NoteWidth=220,NoteAutoWidth=true,TextWrap=true,NoteOffset=new PointF(100,-25),Width=3};}
    [STAThread]static int Main(string[] args)
    {
        try{
            output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            var work=Screen.PrimaryScreen.WorkingArea;var bounds=new Rectangle(work.X+20,work.Y+20,Math.Min(1180,work.Width-40),Math.Min(760,work.Height-40));var image=new Bitmap(bounds.Width,bounds.Height);
            using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(243,245,247));using(var f=new Font("Microsoft YaHei UI",28,GraphicsUnit.Pixel))g.DrawString("空间分析 · 标注工作区",f,Brushes.DimGray,74,60);using(var p=new Pen(Color.FromArgb(210,219,224),2))for(int x=80;x<1000;x+=70)g.DrawRectangle(p,x,130,48,220);}
            using(var o=new CaptureOverlay(image,bounds,new Rectangle(bounds.X+50,bounds.Y+40,bounds.Width-100,440),new[]{bounds})){
                o.Show();Native.SetWindowDisplayAffinity(o.Handle,0);Application.DoEvents();var c=(Canvas)o.Controls[0];
                var point=Mark(Tool.Callout,130,165,130,165,Color.FromArgb(89,137,159),"主要步行入口");c.AddAnnotation(point);
                var number=Mark(Tool.Counter,460,165,460,165,Color.FromArgb(231,90,93),"保留既有结构");number.Number=1;c.AddAnnotation(number);
                var cloud=Mark(Tool.Cloud,790,145,950,250,Color.FromArgb(133,196,194),"公共活动区域");cloud.NoteOffset=new PointF(-130,160);c.AddAnnotation(cloud);
                var label=Mark(Tool.Text,160,350,160,350,Color.FromArgb(89,137,159),"建筑与开放空间");label.FillEnabled=true;label.FillColor=label.Color;c.AddAnnotation(label);
                c.SelectShape(c.AnnotationSnapshot.Length==0?null:c.SelectedShape);c.SetTool(Tool.Callout);Snap(o,"comments-toolbar-native");
                c.AddAnnotation(Mark(Tool.Rect,420,340,530,420,Color.SteelBlue,null));c.AddAnnotation(Mark(Tool.Rect,630,340,740,420,Color.SteelBlue,null));
                Mouse(c,0x201,450,340);Mouse(c,0x202,450,340);typeof(Canvas).GetField("ModifierSource",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,(Func<Keys>)(()=>Keys.Shift));Mouse(c,0x201,660,340);Mouse(c,0x202,660,340);typeof(Canvas).GetField("ModifierSource",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,(Func<Keys>)(()=>Keys.None));
                if(c.SelectionCount!=2)throw new Exception("Native Shift selection failed");Snap(o,"multiselect-native");
                c.SetTool(Tool.Callout);Click(Children(o).Single(x=>x.AccessibleName=="颜色"));var menu=(ToolStripDropDown)Field(o,"colorMenu");Snap(menu,"palette-presets-native");
                Click(Children(menu).Single(x=>x.AccessibleName=="自定义颜色"));Snap(menu,"palette-custom-native");var hex=Children(menu).OfType<TextBox>().Single(x=>x.AccessibleName=="HEX 颜色");hex.Focus();SendMessage(hex.Handle,0x000C,IntPtr.Zero,"#85C4C2");Click(Children(menu).Single(x=>x.AccessibleName=="应用自定义颜色"));if(c.Color.ToArgb()!=Color.FromArgb(133,196,194).ToArgb()||menu.Visible)throw new Exception("Native custom color did not apply and close");
                for(int i=0;i<5;i++){Click(Children(o).Single(x=>x.AccessibleName=="颜色"));menu.Close();Application.DoEvents();}if(menu.IsDisposed)throw new Exception("Popup disposed on close");
                c.SetTool(Tool.Crop);c.SetOutputCornerRadius(48);Snap(o,"rounded-preview-native");using(var png=c.Render())png.Save(Path.Combine(output,"rounded-annotations.png"));
                File.WriteAllText(Path.Combine(output,"native-result.txt"),"PASS native mouse Shift selection; 8-handle group screenshot; native HEX edit/apply; 5 popup reopen cycles; rounded preview/export. DPI="+GetDpiForWindow(o.Handle));
            }
            return 0;
        }catch(Exception ex){File.WriteAllText(Path.Combine(output??".","native-error.txt"),ex.ToString());return 1;}
    }
}
