using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace ScreenCraft
{
    // No Form.Show, desktop capture, global hotkey, clipboard or injected input in this suite.
    static class ScreenCraftTests
    {
        static int passed,failed;
        static string output;
        static readonly List<string> rows=new List<string>();
        [STAThread] static int Main(string[] arguments)
        {
            Console.OutputEncoding=Encoding.UTF8; Theme.Init();
            output=Environment.GetEnvironmentVariable("SCREENCRAFT_TEST_OUTPUT")??Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results");
            Directory.CreateDirectory(output);
            if(Array.IndexOf(arguments,"--ocr-probe")>=0)return NativeOcrProbe.Run(output);
            if(Environment.GetEnvironmentVariable("SCREENCRAFT_DAILY_ONLY")=="1"){DailyWorkflowTests.Run(Run,Check,output);BoardTests.Run(Run,Check,output);DailySurfaceTests.Run(Run,Check,output);File.WriteAllLines(Path.Combine(output,"daily-results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);Console.WriteLine("DAILY "+passed+" PASS, "+failed+" FAIL; desktop excluded");return failed==0?0:1;}
            if(Environment.GetEnvironmentVariable("SCREENCRAFT_EXTRAS_ONLY")=="1"){AnnotationExtrasTests.Run(Run,Check,output);File.WriteAllLines(Path.Combine(output,"extras-results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);Console.WriteLine("EXTRAS "+passed+" PASS, "+failed+" FAIL; desktop excluded");return failed==0?0:1;}
            if(Environment.GetEnvironmentVariable("SCREENCRAFT_CALLOUT_ONLY")=="1"){CalloutAndGlassTests.Run(Run,Check,output);File.WriteAllLines(Path.Combine(output,"callout-glass-results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);Console.WriteLine("CALLOUT "+passed+" PASS, "+failed+" FAIL; desktop excluded");return failed==0?0:1;}
            if(Environment.GetEnvironmentVariable("SCREENCRAFT_FEEDBACK_ONLY")=="1"){PopupAndCaptureFeedbackTests.Run(Run,Check,output);FeedbackPolishTests.Run(Run,Check,output);ColorPickerTests.Run(Run,Check,output);OcrColorSettingsTests.Run(Run,Check);SmartRegionTests.Run(Run,Check);DesktopSaveTests.Run(Run,Check,output);AnnotationInteractionTests.Run(Run,Check,output);File.WriteAllLines(Path.Combine(output,"feedback-results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);Console.WriteLine("FEEDBACK "+passed+" PASS, "+failed+" FAIL; desktop excluded");return failed==0?0:1;}
            if(Environment.GetEnvironmentVariable("SCREENCRAFT_UI_ONLY")=="1"){UiPaintingTests.Run(Run,Check,output);SurfaceConsistencyTests.Run(Run,Check,output);File.WriteAllLines(Path.Combine(output,"ui-paint-results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);Console.WriteLine("UI PAINT "+passed+" PASS, "+failed+" FAIL; desktop excluded");return failed==0?0:1;}
            Run("rectangle/alpha fill preserves exact composited pixel",delegate {
                var s=Mark(Tool.Rect,20,20,180,140);s.FillEnabled=true;s.FillColor=Color.FromArgb(64,255,0,0);
                using(var b=Render(s)){var c=b.GetPixel(80,80);Check(c.R==255&&Math.Abs(c.G-191)<=1&&Math.Abs(c.B-191)<=1,"incorrect alpha fill");}
            });
            Run("rectangle/round corner differs from square export",delegate {
                var s=Mark(Tool.Rect,20,20,180,140);s.FillEnabled=true;s.FillColor=Color.Red;
                using(var square=Render(s)){s.CornerRadius=35;using(var round=Render(s)){Check(square.GetPixel(22,22).G<40,"square");Check(round.GetPixel(22,22).G>230,"rounded corner");}}
            });
            Run("rectangle/solid dash dot and dashdot produce distinct pixels",delegate {
                var hashes=new HashSet<string>();foreach(var dash in new[]{DashStyle.Solid,DashStyle.Dash,DashStyle.Dot,DashStyle.DashDot,DashStyle.DashDotDot}){var s=Mark(Tool.Rect,20,20,250,150);s.Dash=dash;using(var b=Render(s))hashes.Add(Hash(b));}Check(hashes.Count==5,"dash styles identical");
            });
            Run("ellipse/fill and outline render",delegate{var s=Mark(Tool.Ellipse,20,20,180,140);s.FillEnabled=true;s.FillColor=Color.Blue;using(var b=Render(s))Check(b.GetPixel(100,80).B>240&&b.GetPixel(100,80).R<20,"ellipse fill");});
            Run("arrow/tapered and simple arrow render differently",delegate{var s=Mark(Tool.Arrow,30,30,210,130);using(var a=Render(s)){s.SimpleArrow=true;using(var b=Render(s))Check(Hash(a)!=Hash(b),"arrow style");}});
            Run("pen/clone and offset do not mutate undo source",delegate{var s=Mark(Tool.Pen,10,20,80,70);s.Points=new List<PointF>{new PointF(10,20),new PointF(40,60),new PointF(80,70)};var c=s.Clone();c.Offset(c,15,20);Check(s.Points[0]==new PointF(10,20),"aliased points");Check(c.Points[0]==new PointF(25,40),"pen offset");});
            Run("pen/canvas resize and undo transform every node",delegate{using(var c=NewCanvas()){var s=Mark(Tool.Pen,10,20,80,70);s.Points=new List<PointF>{s.A,new PointF(40,60),s.B};c.AddAnnotation(s);c.ResizeSelected(new RectangleF(20,40,140,100));Check(c.SelectedShape.Points[2]==new PointF(160,140),"resize endpoint");c.Undo();Check(c.AnnotationSnapshot[0].Points[2]==new PointF(80,70),"resize undo");}});
            Run("cloud/scalloped boundary and note actually render",delegate{var s=Mark(Tool.Cloud,20,25,130,115);s.Text="Cloud note\n可编辑说明";s.NoteOffset=new PointF(8,0);s.NoteWidth=160;using(var b=Render(s)){b.Save(Path.Combine(output,"cloud-note.png"));Check(b.GetPixel(200,70)!=Color.White,"note absent");}using(var p=Painter.CloudPath(s.Box))Check(p.PointCount>30,"not a scalloped path");});
            Run("counter/edit number note geometry and pixels",delegate{var s=Mark(Tool.Counter,70,70,70,70);s.Number=42;s.Text="节点 42";s.NoteOffset=new PointF(35,-10);using(var b=Render(s)){b.Save(Path.Combine(output,"counter-note.png"));Check(Painter.Bounds(s).Right>200,"note not included in extent");}});
            Run("redaction/export removes source pixels",delegate{var s=Mark(Tool.Redact,30,30,120,120);using(var b=Render(s))Check(b.GetPixel(70,70).ToArgb()==Color.FromArgb(22,22,26).ToArgb(),"not opaque");});
            Run("mosaic/render replaces detailed source with coarse pixels",delegate{var image=new Bitmap(240,180);for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)image.SetPixel(x,y,((x/3+y/4)%2)==0?Color.Black:Color.White);using(var c=new Canvas(image)){c.AddAnnotation(Mark(Tool.Pixelate,20,20,210,150));using(var b=c.Render()){int changed=0;for(int y=30;y<140;y++)for(int x=30;x<200;x++)if(b.GetPixel(x,y).ToArgb()!=image.GetPixel(x,y).ToArgb())changed++;Check(changed>1000,"mosaic did not change detailed source");b.Save(Path.Combine(output,"mosaic-render-offscreen.png"));}}});
            Run("canvas/edit width radius alpha undo redo",delegate{using(var c=NewCanvas()){c.AddAnnotation(Mark(Tool.Rect,20,20,160,100));c.EditSelected(delegate(Shape s){s.Width=9;s.CornerRadius=20;s.FillEnabled=true;s.FillColor=Color.FromArgb(77,0,120,255);});Check(c.SelectedShape.Width==9,"style");c.Undo();Check(c.AnnotationSnapshot[0].Width==3,"undo");c.Redo();Check(c.AnnotationSnapshot[0].CornerRadius==20,"redo");}});
            Run("canvas/move resize delete and undo preserve object identity",delegate{using(var c=NewCanvas()){var s=Mark(Tool.Rect,20,20,160,100);c.AddAnnotation(s);c.Nudge(10,-5);c.ResizeSelected(new RectangleF(40,50,80,90));Check(c.SelectedShape.Box==new RectangleF(40,50,80,90),"resize");c.DeleteSelected();Check(c.AnnotationCount==0,"delete");c.Undo();Check(c.AnnotationSnapshot[0].Id==s.Id,"identity lost");}});
            Run("counter/deletion does not overwrite custom numbers",delegate{using(var c=NewCanvas()){var a=Mark(Tool.Counter,40,40,40,40);a.Number=7;var b=Mark(Tool.Counter,90,90,90,90);b.Number=42;c.AddAnnotation(a);c.AddAnnotation(b);c.DeleteSelected();Check(c.AnnotationSnapshot[0].Number==7,"renumbered user value");c.Undo();Check(c.AnnotationSnapshot[1].Number==42,"undo lost number");}});
            Run("document/save reopen preserves editable model and rendered hash",delegate{using(var c=NewCanvas()){var s=Mark(Tool.Rect,20,20,180,140);s.CornerRadius=18;s.Dash=DashStyle.Dash;s.FillEnabled=true;s.FillColor=Color.FromArgb(50,0,80,255);c.AddAnnotation(s);using(var doc=c.ExportDocument()){var path=Path.Combine(output,"中文可编辑工程.sccraft");doc.Save(path);doc.Save(path);using(var reopened=AnnotationDocument.Open(path))using(var a=doc.Render())using(var b=reopened.Render()){Check(Hash(a)==Hash(b),"roundtrip pixels");Check(reopened.Shapes[0].Id==s.Id,"roundtrip id");}}}});
            Run("document/malformed project rejected",delegate{var path=Path.Combine(output,"broken.sccraft");File.WriteAllText(path,"not a zip");Throws<InvalidDataException>(delegate{using(var d=AnnotationDocument.Open(path)){};});});
            Run("model/nan width and excessive text rejected",delegate{var s=Mark(Tool.Rect,0,0,10,10);s.Width=float.NaN;Throws<InvalidDataException>(delegate{AnnotationDocument.ValidateShape(s);});s.Width=3;s.Text=new string('x',20001);Throws<InvalidDataException>(delegate{AnnotationDocument.ValidateShape(s);});});
            Run("canvas/200 render edit cancel undo cycles",delegate{using(var c=NewCanvas()){for(int i=0;i<200;i++){c.AddAnnotation(Mark(Tool.Rect,10,10,180,120));c.Nudge(1,1);using(var b=c.Render())Check(b.Width==360,"size changed");c.Undo();c.Undo();Check(c.AnnotationCount==0,"state leaked");}Check(!c.HasMarks,"marks remain");}});
            Run("offscreen/render all annotation tools into sample",delegate{using(var c=new Canvas(White(1000,620))){var tools=new[]{Tool.Rect,Tool.Ellipse,Tool.Arrow,Tool.Line,Tool.Text,Tool.Counter,Tool.Cloud,Tool.Pixelate,Tool.Redact,Tool.Pen};for(int i=0;i<tools.Length;i++){int x=30+(i%5)*195,y=40+(i/5)*270;var s=Mark(tools[i],x,y,x+130,y+130);s.Text=tools[i]==Tool.Text?"文字 Text":tools[i]==Tool.Cloud?"云线说明":tools[i]==Tool.Counter?"序号说明":null;s.Number=12;s.NoteWidth=150;s.NoteOffset=new PointF(-20,80);if(tools[i]==Tool.Pen)s.Points=new List<PointF>{s.A,new PointF(x+50,y+90),new PointF(x+100,y+10),s.B};c.AddAnnotation(s);}using(var b=c.Render())b.Save(Path.Combine(output,"annotation-render-offscreen.png"));}});
            PinTests.Run(Run,Check,output);
            PreferencesTests.Run(Run,Check,output);
            HotkeyInputTests.Run(Run,Check);
            GestureTests.Run(Run,Check);
            UtilityTests.Run(Run,Check,output);
            InPlaceCaptureTests.Run(Run,Check,output);
            ToolbarSettingsTests.Run(Run,Check,output);
            TrayAndSelectionTests.Run(Run,Check,output);
            UiPaintingTests.Run(Run,Check,output);
            SurfaceConsistencyTests.Run(Run,Check,output);
            CaptureEditingTests.Run(Run,Check,output);
            PopupAndCaptureFeedbackTests.Run(Run,Check,output);FeedbackPolishTests.Run(Run,Check,output);ColorPickerTests.Run(Run,Check,output);OcrColorSettingsTests.Run(Run,Check);SmartRegionTests.Run(Run,Check);DesktopSaveTests.Run(Run,Check,output);AnnotationInteractionTests.Run(Run,Check,output);
            CalloutAndGlassTests.Run(Run,Check,output);
            AnnotationExtrasTests.Run(Run,Check,output);
            DailyWorkflowTests.Run(Run,Check,output);
            BoardTests.Run(Run,Check,output);
            DailySurfaceTests.Run(Run,Check,output);
            CoreRewriteTests.Run(Run,Check,output);
            Run("canvas/duplicate generates fresh IDs export remains editable",delegate{using(var c=NewCanvas()){c.AddAnnotation(Mark(Tool.Rect,20,20,160,100));c.DuplicateSelected();var list=c.AnnotationSnapshot;Check(list.Length==2&&list[0].Id!=list[1].Id,"duplicate ID");using(var doc=c.ExportDocument())Check(doc.Shapes.Length==2,"export");}});
            Run("editor/hidden control tree exposes full annotation controls",delegate{
                using(var editor=new CraftEditor(White(600,400))){editor.CreateControl();Check(!editor.Visible,"editor became visible");var labels=new List<string>();CollectLabels(editor,labels);foreach(var name in new[]{"云线 W","画笔 B","钉在屏幕 P","线宽（px）","线型","圆角半径（px）","填充不透明度（%）","应用位置与大小","应用文字与序号"})Check(labels.Contains(name),"missing control: "+name);}
            });
            Run("editor/property controls change selected model and rendered pixels",delegate{
                using(var editor=new CraftEditor(White(600,400))){var c=editor.AnnotationCanvas;c.AddAnnotation(Mark(Tool.Rect,30,30,180,150));var width=(NumericUpDown)FindControl(editor,"线宽（px）");width.Value=11;var round=(NumericUpDown)FindControl(editor,"圆角半径（px）");round.Value=25;var dash=(PreferencesChoice)FindControl(editor,"线型");dash.SelectedIndex=2;var fill=(CheckBox)FindControl(editor,"启用填充");fill.Checked=true;Check(c.SelectedShape.Width==11&&c.SelectedShape.CornerRadius==25&&c.SelectedShape.Dash==DashStyle.Dot&&c.SelectedShape.FillEnabled,"property events disconnected");using(var b=c.Render())Check(b.GetPixel(90,90).ToArgb()!=Color.White.ToArgb(),"no fill in output");}
            });
            Run("editor/hidden layout reserves canvas and inspector space",delegate{
                using(var editor=new CraftEditor(White(600,400))){editor.CreateControl();editor.PerformLayout();Check(editor.AnnotationCanvas.Width>600,"canvas crowded out");Check(editor.AnnotationCanvas.Height>500,"toolbar crowded out canvas");Check(!editor.Visible,"layout showed editor");var layout=new List<string>();CollectLayout(editor,layout);File.WriteAllLines(Path.Combine(output,"editor-layout-hidden.txt"),layout);}
            });
            File.WriteAllLines(Path.Combine(output,"results.csv"),new[]{"test,status,detail"}.Concat(rows),Encoding.UTF8);
            Console.WriteLine("TOTAL "+passed+" PASS, "+failed+" FAIL; GUI NOT RUN (desktop excluded)");return failed==0?0:1;
        }
        static void Run(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);rows.Add('"'+name+"\",PASS,\"\"");}catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" :: "+ex);rows.Add('"'+name+"\",FAIL,\""+ex.Message.Replace("\"","\"\"")+"\"");}}
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void CollectLabels(Control control,List<string> into){if(!string.IsNullOrEmpty(control.Text))into.Add(control.Text);if(!string.IsNullOrEmpty(control.AccessibleName))into.Add(control.AccessibleName);var strip=control as ToolStrip;if(strip!=null)foreach(ToolStripItem item in strip.Items)into.Add(item.Text);foreach(Control child in control.Controls)CollectLabels(child,into);}
        static void CollectLayout(Control control,List<string> into){into.Add(control.GetType().Name+" "+control.AccessibleName+" "+control.Bounds);foreach(Control child in control.Controls)CollectLayout(child,into);}
        static Control FindControl(Control root,string name){if(root.AccessibleName==name)return root;foreach(Control c in root.Controls){var found=FindControl(c,name);if(found!=null)return found;}return null;}
        static Control FindByText(Control root,string name){if(root.Text==name)return root;foreach(Control c in root.Controls){var found=FindByText(c,name);if(found!=null)return found;}return null;}
        static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
        internal static Shape Mark(Tool tool,float x,float y,float right,float bottom){return new Shape{Kind=tool,A=new PointF(x,y),B=new PointF(right,bottom),Width=3,FontPx=20,Color=Color.FromArgb(225,35,45),Number=1};}
        static Bitmap White(int w,int h){var b=new Bitmap(w,h,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(b))g.Clear(Color.White);return b;}
        static Canvas NewCanvas(){return new Canvas(White(360,240));}
        static Bitmap Render(Shape s){var b=White(360,240);using(var g=Graphics.FromImage(b))Painter.Draw(g,s,b);return b;}
        static string Hash(Bitmap b){using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(ms.ToArray()));}}
    }
}
