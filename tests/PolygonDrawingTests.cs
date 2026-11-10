using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenCraft
{
    static class PolygonDrawingTests
    {
        sealed class Input:Canvas
        {
            internal Input():base(Source()){InPlaceViewport=true;Size=new Size(900,650);ModifierSource=delegate{return Keys.None;};SetTool(Tool.Pen);SetClosedPen(true);}
            internal void Down(int x,int y,int clicks=1,MouseButtons button=MouseButtons.Left){OnMouseDown(new MouseEventArgs(button,clicks,x,y,0));}
            internal void Up(int x,int y,MouseButtons button=MouseButtons.Left){OnMouseUp(new MouseEventArgs(button,1,x,y,0));}
            internal void Click(int x,int y,int clicks=1){Down(x,y,clicks);Up(x,y);}
            internal void MoveTo(int x,int y){OnMouseMove(new MouseEventArgs(MouseButtons.None,0,x,y,0));}
            internal void Key(Keys keys){OnKeyDown(new KeyEventArgs(keys));}
            internal Bitmap View(){var b=new Bitmap(Width,Height);using(var g=Graphics.FromImage(b)){g.Clear(Color.White);OnPaint(new PaintEventArgs(g,ClientRectangle));}return b;}
        }
        static Bitmap Source(){var image=new Bitmap(900,650);using(var g=Graphics.FromImage(image))g.Clear(Color.White);return image;}
        static string Hash(Bitmap image){using(var bytes=new MemoryStream()){image.Save(bytes,System.Drawing.Imaging.ImageFormat.Png);using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(bytes.ToArray()));}}
        static void Triangle(Input c){c.Click(120,120);c.Click(360,120);c.Click(360,320);}
        static void Click(Canvas c,int x,int y){var methods=BindingFlags.Instance|BindingFlags.NonPublic;typeof(Canvas).GetMethod("OnMouseDown",methods).Invoke(c,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});typeof(Canvas).GetMethod("OnMouseUp",methods).Invoke(c,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
        static System.Collections.Generic.IEnumerable<Control> Children(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Children(c))yield return child;}}
        static bool Key(Form form,Keys keys){return (bool)form.GetType().GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{new Message(),keys});}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("polygon/preview stays open and unchanged when pointer or mouse-up moves",delegate{
                using(var c=new Input()){Triangle(c);using(var before=c.View()){check(before.GetPixel(240,220).ToArgb()==Color.White.ToArgb(),"preview has rubber-band closing edge");c.MoveTo(600,470);c.Up(650,500);using(var after=c.View()){check(Hash(before)==Hash(after)&&c.PolygonPointCount==3&&c.AnnotationCount==0,"pointer added a temporary rod or drag point");before.Save(Path.Combine(output,"point-polygon-draft.png"));}}check(c.Busy&&!c.Capture,"polygon holds native capture between clicks");}
            });
            run("polygon/clicking first anchor commits one exact polygon and fills only after closure",delegate{
                using(var c=new Input()){c.SetColor(Color.Teal);c.SetShapeFill(true);Triangle(c);using(var draft=c.View())check(draft.GetPixel(300,180).ToArgb()==Color.White.ToArgb(),"draft filled before closure");c.Click(122,121);check(!c.DrawingPolygon&&c.AnnotationCount==1&&c.SelectedShape.Polygon&&c.SelectedShape.ClosedPen&&!c.SelectedShape.SmoothPen,"start click did not close");check(c.SelectedShape.Points.Count==3&&c.SelectedShape.Points[0]==new PointF(120,120),"snap inserted extra anchor");using(var image=c.Render()){check(image.GetPixel(240,220).G>image.GetPixel(240,220).R+40,"final closing edge absent");check(image.GetPixel(300,180).R<250,"final fill absent");image.Save(Path.Combine(output,"point-polygon-completed.png"));}c.Undo();check(c.AnnotationCount==0,"completion was not one history entry");c.Redo();check(c.AnnotationSnapshot.Single().Polygon,"redo lost polygon");}
            });
            run("polygon/double click final anchor never adds duplicate or starts another draft",delegate{
                using(var c=new Input()){c.Click(120,120);c.Click(360,120);c.Click(360,320);c.Click(360,320,2);check(c.AnnotationCount==1&&!c.DrawingPolygon&&c.SelectedShape.Points.Count==3,"double click duplicates final anchor");}
                using(var c=new Input()){Triangle(c);c.Click(120,120);c.Click(120,120,2);check(c.AnnotationCount==1&&!c.DrawingPolygon,"double-clicking start restarted polygon");}
            });
            run("polygon/right-click backspace undo and redo operate on anchors without touching prior history",delegate{
                using(var c=new Input()){Triangle(c);c.Down(700,600,1,MouseButtons.Right);c.Up(700,600,MouseButtons.Right);check(c.PolygonPointCount==2&&c.CanRedo,"right-click did not remove last point");c.Redo();check(c.PolygonPointCount==3,"point redo failed");c.Key(Keys.Back);check(c.PolygonPointCount==2,"backspace failed");c.Key(Keys.Control|Keys.Z);check(c.PolygonPointCount==1,"point undo failed");c.Key(Keys.Control|Keys.Y);check(c.PolygonPointCount==2,"keyboard redo failed");c.Click(360,320);c.Key(Keys.Enter);check(c.AnnotationCount==1,"point redo corrupted closure");}
            });
            run("polygon/Escape or changing tool cancels all anchors and preserves old redo branch",delegate{
                using(var c=new Input()){c.SetClosedPen(false);c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,500,400,650,500));c.Undo();check(c.CanRedo,"fixture redo");c.SetClosedPen(true);Triangle(c);c.Key(Keys.Escape);check(!c.DrawingPolygon&&c.AnnotationCount==0&&c.CanRedo,"cancel committed or cleared old redo");c.Redo();check(c.AnnotationCount==1,"old redo lost");c.SetTool(Tool.Pen);Triangle(c);c.SetTool(Tool.Ellipse);check(!c.DrawingPolygon&&c.AnnotationCount==1&&!c.Busy,"tool change kept partial polygon");}
            });
            run("polygon/invalid completion stays in drawing and cannot export unfinished geometry",delegate{
                using(var c=new Input()){c.Click(120,120);c.Click(360,120);c.Click(120,120);check(c.PolygonPointCount==2,"start click added duplicate anchor");check(!c.CompletePolygon()&&c.DrawingPolygon&&c.AnnotationCount==0,"two anchors accepted");c.Click(500,120);check(!c.CompletePolygon(),"collinear region accepted");bool rejected=false;try{using(var doc=c.ExportDocument()){}}catch(InvalidOperationException){rejected=true;}check(rejected&&c.DrawingPolygon,"export silently dropped unfinished polygon");c.Click(420,320);check(c.CompletePolygon(),"valid continuation rejected");}
            });
            run("polygon/clicks over crop handles or outside crop never start range adjustment",delegate{
                using(var c=new Input()){c.Crop=new RectangleF(60,60,600,400);int rangeDown=0;c.FullCaptureSurface=true;c.CaptureDown=delegate{rangeDown++;return false;};c.Click(120,120);c.Click(360,120);int before=rangeDown;c.Click(800,500);c.Click(60,60);check(rangeDown==before&&c.Crop==new RectangleF(60,60,600,400)&&c.DrawingPolygon&&c.PolygonPointCount==3,"crop callback intercepted polygon vertices");}
            });
            run("polygon/style updates apply during draft and do not create history until completion",delegate{
                using(var c=new Input()){Triangle(c);c.SetColor(Color.Blue);AnnotationSizing.Apply(c,8);c.SetShapeFill(true);c.Key(Keys.Enter);check(c.SelectedShape.Color.ToArgb()==Color.Blue.ToArgb()&&c.SelectedShape.Width==8&&c.SelectedShape.FillEnabled,"draft style edits disconnected");c.Undo();check(c.AnnotationCount==0,"draft styling created extra model history");}
            });
            run("polygon/ordinary freehand remains a continuous drag after disabling point mode",delegate{
                using(var c=new Input()){Triangle(c);c.Key(Keys.Enter);c.SetClosedPen(false);check(!c.SelectedShape.Polygon&&!c.DefaultStyle.ClosedPen,"point mode did not turn off");c.SetTool(Tool.Pen);c.Down(500,350);c.MoveTo(600,450);c.Up(700,400);check(c.AnnotationCount==2&&!c.DrawingPolygon&&!c.SelectedShape.Polygon&&!c.SelectedShape.ClosedPen&&c.SelectedShape.Points.Count==3,"ordinary pen became point tool");}
            });
            run("polygon/straight anchors survive rotation resize undo and editable project roundtrip",delegate{
                using(var c=new Input()){Triangle(c);c.Key(Keys.Enter);c.RotateSelected(27);c.ResizeSelected(new RectangleF(180,180,300,210));check(c.SelectedShape.Polygon&&c.SelectedShape.Points.Count==3,"transform lost nodes");using(var doc=c.ExportDocument()){string path=Path.Combine(output,"逐点多边形.sccraft");doc.Save(path);using(var read=AnnotationDocument.Open(path))using(var a=doc.Render())using(var b=read.Render()){check(read.Shapes.Single().Polygon&&read.Shapes.Single().Rotation==27&&Hash(a)==Hash(b),"saved polygon became smoothed stroke or changed pixels");}}c.Undo();check(c.AnnotationSnapshot.Single().Polygon,"resize undo lost polygon mode");}
            });
            run("polygon/capture Enter closes region before a later Enter completes capture",delegate{
                using(var overlay=new CaptureOverlay(Source(),new Rectangle(0,0,900,650),new Rectangle(40,40,800,500),null)){var c=overlay.AnnotationCanvas;c.SetTool(Tool.Pen);c.SetClosedPen(true);int copies=0;overlay.CopyOutput=delegate{copies++;};Click(c,120,120);Click(c,360,120);Click(c,360,320);check(Key(overlay,Keys.Control|Keys.Shift|Keys.Enter)&&copies==0&&c.DrawingPolygon,"copy shortcut consumed unfinished region");Key(overlay,Keys.Enter);check(c.AnnotationCount==1&&!c.DrawingPolygon&&copies==0&&!overlay.Completed,"Enter copied instead of closing region");Key(overlay,Keys.Enter);check(copies==1&&overlay.Completed,"completed polygon cannot finish capture");}
                using(var editor=new CraftEditor(Source())){var c=editor.AnnotationCanvas;c.InPlaceViewport=true;c.SetTool(Tool.Pen);c.SetClosedPen(true);int copies=0;editor.CopyOutput=delegate{copies++;};Click(c,120,120);Click(c,360,120);Click(c,360,320);Key(editor,Keys.Enter);check(c.AnnotationCount==1&&!c.DrawingPolygon&&copies==0,"file editor Enter exported draft");}
            });
            run("polygon UI/finish control commits pending anchors and secondary row renders without a rubber band",delegate{
                using(var overlay=new CaptureOverlay(Source(),new Rectangle(0,0,900,650),new Rectangle(40,40,800,500),null)){var c=overlay.AnnotationCanvas;c.SetTool(Tool.Pen);c.SetClosedPen(true);Click(c,150,150);Click(c,400,150);Click(c,400,320);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var buttons=Children(overlay).OfType<CaptureIconButton>().ToArray();check(buttons.Single(b=>b.AccessibleName=="逐点多边形").Selected,"polygon mode has no selected state");using(var image=new Bitmap(900,650)){using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,overlay,overlay);image.Save(Path.Combine(output,"point-polygon-toolbar-offscreen.png"));}typeof(Button).GetMethod("OnClick",flags).Invoke(buttons.Single(b=>b.AccessibleName.StartsWith("完成多边形")),new object[]{EventArgs.Empty});check(c.AnnotationCount==1&&!c.DrawingPolygon&&c.SelectedShape.Polygon,"finish control disconnected");}
            });
            run("polygon/legacy smooth closed paths preserve prior rendering and new icon exposes point mode",delegate{
                using(var c=new Input()){var old=ScreenCraftTests.Mark(Tool.Pen,100,100,400,300);old.Points=new System.Collections.Generic.List<PointF>{new PointF(100,100),new PointF(400,100),new PointF(400,300)};old.SmoothPen=true;old.ClosedPen=true;c.AddAnnotation(old);using(var path=Painter.FreehandPath(c.SelectedShape))check(path.PathTypes.Any(type=>(type&7)==3),"legacy curve converted to straight polygon");using(var document=c.ExportDocument()){string file=Path.Combine(output,"旧闭合曲线.sccraft");document.Save(file);using(var read=AnnotationDocument.Open(file))check(!read.Shapes[0].Polygon&&read.Shapes[0].SmoothPen&&read.Shapes[0].ClosedPen,"legacy flags changed");}}
            });
        }
    }
}
