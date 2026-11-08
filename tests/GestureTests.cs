using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Stackshot
{
    static class GestureTests
    {
        sealed class InputCanvas:Canvas
        {
            public InputCanvas(Bitmap b):base(b){ModifierSource=delegate{return Keys.None;};}
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
