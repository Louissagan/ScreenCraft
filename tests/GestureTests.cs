using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class GestureTests
    {
        sealed class InputCanvas:Canvas
        {
            public Keys Modifiers;
            public InputCanvas(Bitmap b):base(b){ModifierSource=delegate{return Modifiers;};}
            public void Down(Point p){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            public void MoveTo(Point p){OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));}
            public void Up(Point p){OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            public void Draw(PointF a,PointF b){var x=ImageToControl(a);var y=ImageToControl(b);Down(x);MoveTo(y);Up(y);}
        }
        public static void Run(Action<string,Action> run,Action<bool,string> check)
        {
            run("gestures/release delivers the last annotation coordinate without a move message",delegate{
                using(var canvas=new InputCanvas(new Bitmap(600,400))){
                    canvas.Bounds=new Rectangle(0,0,800,600);
                    canvas.SetTool(Tool.Rect);
                    canvas.Down(canvas.ImageToControl(new PointF(50,50)));
                    canvas.Up(canvas.ImageToControl(new PointF(250,180)));
                    check(canvas.AnnotationCount==1&&Math.Abs(canvas.AnnotationSnapshot[0].B.X-250)<1,"release endpoint lost");
                }
            });
            run("gestures/production mouse handlers draw move and resize rectangle",delegate{
                using(var form=new Form{ClientSize=new Size(800,600)})using(var canvas=new InputCanvas(new Bitmap(600,400))){form.Controls.Add(canvas);canvas.Bounds=new Rectangle(0,0,800,600);canvas.SetTool(Tool.Rect);canvas.Draw(new PointF(50,50),new PointF(200,160));check(canvas.AnnotationCount==1,"drag did not create rectangle");var before=canvas.AnnotationSnapshot[0].Box;var p=canvas.ImageToControl(new PointF(90,50));canvas.Down(p);var to=canvas.ImageToControl(new PointF(120,80));canvas.MoveTo(to);canvas.Up(to);var moved=canvas.AnnotationSnapshot[0].Box;check(moved.X>before.X+25&&moved.Y>before.Y+25,"border drag did not move object");var handles=canvas.SelectionHandles();canvas.Down(handles[2]);canvas.MoveTo(new Point(handles[2].X+80,handles[2].Y+60));canvas.Up(new Point(handles[2].X+80,handles[2].Y+60));check(canvas.AnnotationSnapshot[0].Box.Width>moved.Width+50,"handle resize failed");check(!form.Visible,"test showed a form");}
            });
            run("spotlight/pointer and selection stay on the active layer",delegate{
                using(var c=new InputCanvas(new Bitmap(600,400))){
                    c.InPlaceViewport=true;c.Size=c.Img.Size;
                    var rectangle=ScreenCraftTests.Mark(Tool.Rect,120,100,340,220);
                    rectangle.FillEnabled=true;c.AddAnnotation(rectangle);
                    c.SetTool(Tool.Spotlight);
                    c.Draw(new PointF(150,130),new PointF(290,190));
                    var spotlight=c.SelectedShape.Clone();
                    check(spotlight.Kind==Tool.Spotlight&&c.AnnotationCount==2,"ordinary shape intercepted spotlight");

                    c.SetTool(Tool.Line);
                    c.Draw(new PointF(150,164),new PointF(400,164));
                    check(c.AnnotationCount==3&&c.SelectedShape.Kind==Tool.Line,"spotlight edge or filled rectangle intercepted drawing");
                    check(c.AnnotationSnapshot[1].Box==spotlight.Box&&c.AnnotationSnapshot[0].Box==rectangle.Box,"drawing moved an earlier object");
                    c.Modifiers=Keys.Alt;
                    c.Draw(new PointF(215,164),new PointF(285,270));
                    check(c.AnnotationCount==4,"selected line intercepted forced drawing");
                    c.Modifiers=Keys.None;
                    c.SelectAll();check(c.SelectionCount==3&&c.SelectedShapes.All(s=>s.Kind!=Tool.Spotlight),"select all mixed both layers");
                    c.SetTool(Tool.Line);
                    var lastLine=c.AnnotationSnapshot[3];
                    var lastLinePoint=new Point(265,240);c.Down(lastLinePoint);c.Up(lastLinePoint);
                    c.ReorderSelected(-1,true);
                    check(c.AnnotationSnapshot[0].Id==lastLine.Id&&c.AnnotationSnapshot[1].Id==spotlight.Id,"arrange did not stay within the ordinary layer");

                    c.SetTool(Tool.Spotlight);
                    var edge=new Point(150,145);c.Down(edge);c.Up(edge);
                    check(c.SelectionCount==1&&c.SelectedShape.Id==spotlight.Id,"spotlight behind annotations could not be picked");
                    c.Draw(edge,new PointF(165,160));
                    check(c.SelectedShape.A==new PointF(165,145),"selected spotlight could not be moved");
                    c.SelectAll();check(c.SelectionCount==1,"spotlight selection included an ordinary mark");
                    c.DeleteSelected();check(c.AnnotationCount==3,"spotlight delete removed other shapes");
                    c.Undo();check(c.AnnotationCount==4&&c.AnnotationSnapshot[1].Id==spotlight.Id,"undo lost editable spotlight");
                }
            });
            run("spotlight/ordinary marks stay above the mask in preview and output",delegate{
                using(var c=new InputCanvas(new Bitmap(600,400))){
                    c.InPlaceViewport=true;c.Size=c.Img.Size;
                    using(var g=Graphics.FromImage(c.Img))g.Clear(Color.White);
                    c.AddAnnotation(ScreenCraftTests.Mark(Tool.Pixelate,215,125,400,235));
                    var line=ScreenCraftTests.Mark(Tool.Line,55,170,420,170);line.Width=8;
                    c.AddAnnotation(line);c.SetTool(Tool.Spotlight);
                    c.Down(new Point(95,115));
                    using(var beforeDrag=UiPaintingTests.NativePaint(c,Color.Magenta))check(beforeDrag.GetPixel(50,50).ToArgb()==Color.White.ToArgb(),"zero-size spotlight dimmed the image");
                    c.MoveTo(new Point(190,225));
                    using(var preview=UiPaintingTests.NativePaint(c,Color.Magenta)){
                        check(preview.GetPixel(50,50).R<120&&preview.GetPixel(130,150).R==255,"draft spotlight not previewed");
                        check(preview.GetPixel(320,140).R==255&&preview.GetPixel(320,170).ToArgb()==line.Color.ToArgb(),"draft dimmed a mark or mosaic");
                    }
                    c.Up(new Point(190,225));
                    using(var output=c.Render()){
                        check(output.GetPixel(50,50).R<120&&output.GetPixel(130,150).R==255,"spotlight missing from output");
                        check(output.GetPixel(320,140).R==255&&output.GetPixel(320,170).ToArgb()==line.Color.ToArgb(),"output dimmed a mark or mosaic");
                    }
                }
            });
            run("gestures/cloud drag opens editable note and retains cloud",delegate{
                using(var form=new Form{ClientSize=new Size(800,600)})using(var canvas=new InputCanvas(new Bitmap(600,400))){form.Controls.Add(canvas);canvas.Bounds=new Rectangle(0,0,800,600);canvas.SetTool(Tool.Cloud);canvas.Draw(new PointF(80,80),new PointF(220,170));check(canvas.Typing,"cloud note did not open");var box=canvas.Controls.OfType<TextBox>().Single();box.Text="云线说明\r\n保留几何";canvas.CommitText();check(canvas.AnnotationCount==1&&canvas.AnnotationSnapshot[0].Kind==Tool.Cloud&&canvas.AnnotationSnapshot[0].Text.Contains("云线说明"),"cloud lost when text committed");canvas.Undo();check(canvas.AnnotationSnapshot[0].Text==null,"note undo");canvas.Redo();check(canvas.AnnotationSnapshot[0].Text.Contains("保留几何"),"note redo");}
            });
            run("gestures/counter single clicks auto-increment and notes edit",delegate{
                using(var form=new Form{ClientSize=new Size(800,600)})using(var canvas=new InputCanvas(new Bitmap(600,400))){form.Controls.Add(canvas);canvas.Bounds=new Rectangle(0,0,800,600);canvas.SetTool(Tool.Counter);foreach(var point in new[]{new PointF(80,80),new PointF(200,200)}){var p=canvas.ImageToControl(point);canvas.Down(p);canvas.Up(p);canvas.CommitText();}var all=canvas.AnnotationSnapshot;check(all.Length==2&&all[0].Number==1&&all[1].Number==2,"click sequence");canvas.EditSelected(delegate(Shape s){s.Number=42;});canvas.EditNote();var box=canvas.Controls.OfType<TextBox>().Single();box.Text="节点说明";canvas.CommitText();check(canvas.AnnotationSnapshot[1].Number==42&&canvas.AnnotationSnapshot[1].Text=="节点说明","editable number and note");}
            });
            run("gestures/pen stroke records intermediate nodes and can cancel",delegate{
                using(var form=new Form{ClientSize=new Size(800,600)})using(var canvas=new InputCanvas(new Bitmap(600,400))){form.Controls.Add(canvas);canvas.Bounds=new Rectangle(0,0,800,600);canvas.SetTool(Tool.Pen);canvas.Down(canvas.ImageToControl(new PointF(30,30)));canvas.MoveTo(canvas.ImageToControl(new PointF(100,150)));canvas.MoveTo(canvas.ImageToControl(new PointF(200,50)));canvas.Up(canvas.ImageToControl(new PointF(200,50)));check(canvas.AnnotationSnapshot[0].Points.Count==3,"freehand nodes missing");canvas.Down(canvas.ImageToControl(new PointF(350,250)));canvas.MoveTo(canvas.ImageToControl(new PointF(450,350)));canvas.CancelGesture();check(canvas.AnnotationCount==1,"cancel committed unfinished stroke");}
            });
        }
    }
}
