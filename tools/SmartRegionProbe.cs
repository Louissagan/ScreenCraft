// Only self-owned controls and a synthetic frozen image; no desktop pixels, text or clipboard are read.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stackshot;
class SmartRegionProbe
{
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Field(object target,string name){return target.GetType().GetField(name,flags).GetValue(target);}
    static object Call(object target,string name,params object[] args){return target.GetType().GetMethod(name,flags).Invoke(target,args);}
    static object Static(string method,params object[] args){return typeof(Canvas).Assembly.GetType("Stackshot.SmartRegions").GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
    static void Check(bool value,string detail){if(!value)throw new Exception(detail);}
    static void Wait(Func<bool> ready){var clock=System.Diagnostics.Stopwatch.StartNew();while(!ready()&&clock.ElapsedMilliseconds<6000){Application.DoEvents();Thread.Sleep(5);}Check(ready(),"probe timed out");}
    static void Mouse(RegionPicker p,string method,int x,int y){Call(p,method,new MouseEventArgs(MouseButtons.Left,1,x,y,0));}
    static void Save(RegionPicker p,string path){Call(p,"Present");using(var image=((Dib)Field(p,"back")).ToBitmap())image.Save(path);}
    [STAThread]static int Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);var messages=new List<string>();try{
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            var work=Screen.PrimaryScreen.WorkingArea;using(var target=new Form{Text="ScreenCraft · smart-region test",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(work.X+30,work.Y+30,840,640)}){
                var panel=new Panel{Bounds=new Rectangle(80,110,620,360)};var button=new Button{Text="Test button",Bounds=new Rectangle(40,40,120,55)};panel.Controls.Add(button);target.Controls.Add(panel);target.Show();Native.SetWindowPos(target.Handle,new IntPtr(-1),target.Left,target.Top,target.Width,target.Height,0x40);target.Activate();Application.DoEvents();
                var desktop=target.Bounds;var box=button.RectangleToScreen(button.ClientRectangle);var point=new Point(box.Left+box.Width/2,box.Top+box.Height/2);var native=(Rectangle[])Static("NativeRegions",target.Handle);Check(native.Contains(box),"native button boundary absent");messages.Add("PASS native child button and panel rectangles");
                var query=Task.Run(()=>(Rectangle[])Static("AutomationRegions",target.Handle,point));Wait(()=>query.IsCompleted);if(!query.Result.Any(r=>r==box)){query=Task.Run(()=>(Rectangle[])Static("AutomationRegions",target.Handle,point));Wait(()=>query.IsCompleted);}Check(query.Result.Any(r=>r.Contains(point)&&r.Width<desktop.Width),"UI Automation did not discover button; expected="+box+" point="+point+" detected="+string.Join(";",query.Result.Select(r=>r.ToString())));messages.Add("PASS UI Automation provides a containing region under the specified root; native HWND supplies exact button");
                var grab=new Dib(desktop.Width,desktop.Height);using(var g=grab.Graphics()){g.Clear(Color.FromArgb(240,244,248));var pr=panel.RectangleToScreen(panel.ClientRectangle);pr.Offset(-desktop.X,-desktop.Y);g.FillRectangle(Brushes.LightSteelBlue,pr);var br=box;br.Offset(-desktop.X,-desktop.Y);g.FillRectangle(Brushes.White,br);g.DrawRectangle(Pens.SteelBlue,br);using(var font=new Font("Segoe UI",16,GraphicsUnit.Pixel))g.DrawString("SMART REGION TEST",font,Brushes.DimGray,80,50);}
                var windows=new List<Grabber.Win>{new Grabber.Win{Handle=target.Handle,Bounds=desktop}};
                using(var picker=(RegionPicker)Activator.CreateInstance(typeof(RegionPicker),flags,null,new object[]{grab,desktop,windows,RegionPicker.Mode.Image},null)){
                    picker.Show();Native.SetWindowPos(picker.Handle,new IntPtr(-1),picker.Left,picker.Top,picker.Width,picker.Height,0x40);var local=new Point(point.X-desktop.X,point.Y-desktop.Y);Mouse(picker,"OnMouseMove",local.X,local.Y);Check(((Rectangle[])Field(picker,"smartChoices")).Length>=3,"nested native candidates not available synchronously");var small=(Rectangle)Field(picker,"hover");var expected=box;expected.Offset(-desktop.X,-desktop.Y);Check(small==expected,"picker did not snap to button");Save(picker,Path.Combine(args[0],"smart-button.png"));
                    var sizes=new List<string>{small.Size.ToString()};var choices=(Rectangle[])Field(picker,"smartChoices");for(int i=1;i<choices.Length;i++){Call(picker,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,local.X,local.Y,120));var r=(Rectangle)Field(picker,"hover");Check((long)r.Width*r.Height>(long)small.Width*small.Height,"wheel did not enlarge region");small=r;sizes.Add(r.Size.ToString());}Save(picker,Path.Combine(args[0],"smart-window.png"));messages.Add("PASS picker button, panel, client and window wheel ranges: "+string.Join(" -> ",sizes));
                    for(int i=1;i<choices.Length;i++)Call(picker,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,local.X,local.Y,-120));Check((Rectangle)Field(picker,"hover")==expected,"wheel did not return to button");
                    Mouse(picker,"OnMouseDown",16,18);Mouse(picker,"OnMouseMove",280,205);Mouse(picker,"OnMouseUp",280,205);var selected=(Rectangle)Field(picker,"result");Check(selected==new Rectangle(desktop.X+16,desktop.Y+18,265,188),"manual drag replaced by detected range");messages.Add("PASS manual drag and final mouse-up coordinates override smart selection");var crop=Field(picker,"crop") as Bitmap;if(crop!=null)crop.Dispose();
                }
            }
            File.WriteAllLines(Path.Combine(args[0],"native-result.txt"),messages);return 0;
        }catch(Exception ex){messages.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(args[0],"native-result.txt"),messages);return 1;}
    }
}
