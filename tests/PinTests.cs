using System;
using System.Drawing;
using System.Linq;
namespace ScreenCraft
{
    static class PinTests
    {
        public static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("pins/20 registry entries remain independent",delegate{
                var registry=new PinRegistry<object>();var items=new object[20];for(int i=0;i<20;i++){items[i]=new object();registry.Add(i.ToString(),items[i]);}
                registry.Remove("0");check(registry.Count==19,"other pins removed");for(int i=1;i<20;i++)check(registry.Find(i.ToString())==items[i],"id changed");
                bool rejected=false;try{registry.Add("1",new object());}catch(InvalidOperationException){rejected=true;}check(rejected,"silently overwrote old pin");
            });
            run("pins/opacity changes by 5 percent and clamps 20 to 100",delegate{check(PinGeometry.StepOpacity(100,-120)==95,"step");check(PinGeometry.StepOpacity(20,-120)==20,"lower");check(PinGeometry.StepOpacity(100,120)==100,"upper");});
            run("pins/state serialization preserves ID negative bounds and per-window flags",delegate{var s=new PinState{Bounds=new Rectangle(-1400,50,480,300),OpacityPercent=55,Zoom=1.4f,ClickThrough=true,Thumbnail=true};var r=PinState.FromJson(s.ToJson());check(r.Id==s.Id&&r.Bounds==s.Bounds&&r.OpacityPercent==55&&r.ClickThrough&&r.Thumbnail,"state roundtrip");});
            run("pins/offscreen negative-coordinate migration remains reachable",delegate{var a=new[]{new Rectangle(-1920,0,1920,1080),new Rectangle(0,0,1920,1080)};var b=PinGeometry.KeepReachable(new Rectangle(-6000,-4000,400,300),a);check(Rectangle.Intersect(b,a[0]).Width>=40,"outside monitors");var good=new Rectangle(-1500,100,400,300);check(PinGeometry.KeepReachable(good,a)==good,"valid negative coordinate moved");});
            run("pins/placement staggers and stays inside work area",delegate{var work=new Rectangle(-1600,50,1600,900);var previous=Rectangle.Empty;for(int i=0;i<8;i++){var p=PinGeometry.Place(new Size(300,200),work,new Point(-800,500),i);check(work.Contains(p),"placement outside work area");check(p!=previous,"no stagger");previous=p;}});
            run("pins/20 actual hidden forms own unique HWND and topmost style",delegate{
                var pins=new System.Collections.Generic.List<PinWindow>();
                try{
                    int initial=PinWindow.All.Count;var handles=new System.Collections.Generic.HashSet<IntPtr>();
                    for(int i=0;i<20;i++){
                        var image=new Bitmap(120,80);using(var g=Graphics.FromImage(image))g.Clear(Color.FromArgb(255,(20*i)%256,100,150));
                        var pin=PinWindow.CreateHidden(AnnotationDocument.FromImage(image),new Rectangle(0,0,1920,1080));pins.Add(pin);
                        var handle=pin.Handle;check(handles.Add(handle),"shared HWND");check(!Native.IsWindowVisible(handle),"test unexpectedly showed a window");check(pin.TopMost,"TopMost false");check((Native.GetWindowLong(handle,-20)&8)!=0,"WS_EX_TOPMOST missing");
                    }
                    var proof=pins.Select(p=>new{Id=p.PinId,HWND=p.Handle.ToInt64(),TopMost=p.TopMost,Visible=Native.IsWindowVisible(p.Handle),ExtendedStyle=Native.GetWindowLong(p.Handle,-20)}).ToArray();
                    System.IO.File.WriteAllText(System.IO.Path.Combine(output,"hidden-pin-evidence.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new{Kind="hidden integration test; not desktop GUI acceptance",Count=proof.Length,Windows=proof}));
                    check(PinWindow.All.Count==initial+20,"registry count");var others=pins.Skip(1).Select(p=>p.Handle).ToArray();pins[0].Close();
                    check(PinWindow.All.Count==initial+19,"close A affected others");for(int i=1;i<pins.Count;i++)check(!pins[i].IsDisposed&&pins[i].Handle==others[i-1],"other pin lost");
                    pins[1].SetOpacityPercent(55);pins[1].SetClickThrough(true);check((Native.GetWindowLong(pins[1].Handle,-20)&0x20)!=0,"no mouse pass-through flag");pins[1].SetClickThrough(false);check((Native.GetWindowLong(pins[1].Handle,-20)&0x20)==0,"mouse pass-through not restored");
                    pins[1].ZoomAt(1.5f,new Point(60,40));check(Math.Abs(pins[1].State.Zoom-1.5f)<.01,"zoom");check(pins[2].State.OpacityPercent==100,"opacity leaked between pins");
                }finally{foreach(var pin in pins)pin.Dispose();}
                check(PinWindow.All.Count==0,"pin resources remain registered");
            });
            run("pins/re-edit retains original source and independent annotations",delegate{
                using(var canvas=new Canvas(new Bitmap(300,200))){var s=ScreenCraftTests.Mark(Tool.Rect,20,20,100,100);canvas.AddAnnotation(s);using(var pin=PinWindow.CreateHidden(canvas.ExportDocument(),new Rectangle(0,0,1920,1080)))using(var doc=pin.ExportDocument())using(var edit=new Canvas(new Bitmap(doc.Source))){edit.LoadDocument(doc);check(edit.AnnotationCount==1,"annotations flattened");edit.AddAnnotation(ScreenCraftTests.Mark(Tool.Ellipse,140,30,220,100));using(var unchanged=pin.ExportDocument())check(unchanged.Shapes.Length==1,"editing mutated pin without apply");pin.ReplaceDocument(edit.ExportDocument());using(var updated=pin.ExportDocument())check(updated.Shapes.Length==2,"apply lost annotations");}}
            });
        }
    }
}

