using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenCraft
{
    static class AnnotationTransformTests
    {
        sealed class Input:Canvas
        {
            internal Keys Modifiers;
            internal Input():base(White()){InPlaceViewport=true;Size=new Size(900,650);ModifierSource=delegate{return Modifiers;};}
            internal void Down(Point p){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            internal void MoveTo(Point p){OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));}
            internal void Up(Point p){OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            internal void Click(PointF p){var at=Point.Round(p);Down(at);Up(at);}
            internal void Key(Keys keys){OnKeyDown(new KeyEventArgs(keys));}
            internal void Preview(string path){using(var image=new Bitmap(Width,Height))using(var g=Graphics.FromImage(image)){g.Clear(Color.White);OnPaint(new PaintEventArgs(g,ClientRectangle));image.Save(path);}}
        }
        static Bitmap White(){var b=new Bitmap(900,650);using(var g=Graphics.FromImage(b))g.Clear(Color.White);return b;}
        static Shape Rect(float x,float y,float width,float height){var s=ScreenCraftTests.Mark(Tool.Rect,x,y,x+width,y+height);s.FillEnabled=true;s.FillColor=Color.FromArgb(60,s.Color);return s;}
        static Shape Pen(bool closed,bool smooth){var s=ScreenCraftTests.Mark(Tool.Pen,120,120,340,320);s.Points=new List<PointF>{new PointF(120,120),new PointF(340,120),new PointF(340,320),new PointF(240,300)};s.ClosedPen=closed;s.SmoothPen=smooth;s.Width=4;return s;}
        static string Hash(Bitmap image){using(var bytes=new MemoryStream()){image.Save(bytes,System.Drawing.Imaging.ImageFormat.Png);using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(bytes.ToArray()));}}
        static bool Near(PointF a,PointF b,float tolerance){return Math.Abs(a.X-b.X)<tolerance&&Math.Abs(a.Y-b.Y)<tolerance;}
        static bool FormKey(Form form,Keys keys){return (bool)form.GetType().GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(form,new object[]{new Message(),keys});}
        static IEnumerable<Control> Children(Control root){foreach(Control child in root.Controls){yield return child;foreach(var nested in Children(child))yield return nested;}}
        static void Click(Button button){typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(button,new object[]{EventArgs.Empty});}
        static void Pair(Input c){c.AddAnnotation(Rect(120,150,180,80));c.AddAnnotation(Rect(480,300,100,60));c.SelectAll();}
        static void OrderCheck(Canvas c,string[] expected,Action<bool,string> check){check(c.AnnotationSnapshot.Select(s=>s.Id).SequenceEqual(expected),"incorrect stack order");}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("arrange/Illustrator shortcuts step and send to edges preserve undo and boundary redo",delegate{
                using(var c=new Input()){for(int i=0;i<4;i++)c.AddAnnotation(Rect(60+i*150,100,80,60));var ids=c.AnnotationSnapshot.Select(s=>s.Id).ToArray();
                    c.Key(Keys.Control|Keys.OemOpenBrackets);OrderCheck(c,new[]{ids[0],ids[1],ids[3],ids[2]},check);
                    c.Key(Keys.Control|Keys.Shift|Keys.OemOpenBrackets);OrderCheck(c,new[]{ids[3],ids[0],ids[1],ids[2]},check);
                    c.Key(Keys.Control|Keys.Shift|Keys.OemCloseBrackets);OrderCheck(c,ids,check);c.Undo();
                    c.ReorderSelected(-1,true);check(c.CanRedo,"boundary no-op discarded redo");c.Redo();OrderCheck(c,ids,check);
                }
            });
            run("arrange/multi-selection keeps relative order at front and back",delegate{
                using(var c=new Input()){for(int i=0;i<4;i++)c.AddAnnotation(Rect(60+i*150,100,80,60));var ids=c.AnnotationSnapshot.Select(s=>s.Id).ToArray();c.SelectShape(null);c.Click(new PointF(80,120));c.Modifiers=Keys.Control;c.Click(new PointF(380,120));c.Modifiers=Keys.None;check(c.SelectionCount==2,"fixture multi selection");
                    c.ReorderSelected(1,true);OrderCheck(c,new[]{ids[1],ids[3],ids[0],ids[2]},check);c.ReorderSelected(-1,true);OrderCheck(c,new[]{ids[0],ids[2],ids[1],ids[3]},check);c.Undo();OrderCheck(c,new[]{ids[1],ids[3],ids[0],ids[2]},check);
                }
            });
            run("arrange/capture and file editor route bracket shortcuts while typing keeps text input",delegate{
                using(var o=new CaptureOverlay(White(),new Rectangle(0,0,900,650),new Rectangle(30,30,800,540),null)){
                    var c=o.AnnotationCanvas;c.AddAnnotation(Rect(100,100,100,60));c.AddAnnotation(Rect(300,200,100,60));var id=c.SelectedShape.Id;check(FormKey(o,Keys.Control|Keys.Shift|Keys.OemOpenBrackets)&&c.AnnotationSnapshot[0].Id==id,"capture shortcut");
                    var text=ScreenCraftTests.Mark(Tool.Text,500,300,500,300);text.Text="输入";c.AddAnnotation(text);c.EditNote();var before=c.AnnotationSnapshot.Select(s=>s.Id).ToArray();check(!c.ApplyArrangeKey(Keys.Control|Keys.OemCloseBrackets),"typing intercepted arrange");OrderCheck(c,before,check);c.Undo();
                }
                using(var e=new CraftEditor(White())){var c=e.AnnotationCanvas;c.AddAnnotation(Rect(100,100,100,60));c.AddAnnotation(Rect(300,200,100,60));var id=c.SelectedShape.Id;check(FormKey(e,Keys.Control|Keys.OemOpenBrackets)&&c.AnnotationSnapshot[0].Id==id,"editor shortcut");}
            });
            run("arrange/board uses identical shortcuts and preserves layer undo",delegate{
                using(var doc=new BoardDocument())using(var view=new BoardView(doc)){var a=doc.AddImage(White(),new PointF(0,0));var b=doc.AddImage(White(),new PointF(100,0));var c=doc.AddImage(White(),new PointF(200,0));view.SelectedId=c;
                    check(view.ApplyKey(Keys.Control|Keys.OemOpenBrackets)&&doc.Items.Select(i=>i.Id).SequenceEqual(new[]{a,c,b}),"board step");
                    view.ApplyKey(Keys.Control|Keys.Shift|Keys.OemOpenBrackets);check(doc.Items[0].Id==c,"board send back");view.ApplyKey(Keys.Control|Keys.Shift|Keys.OemCloseBrackets);check(doc.Items[2].Id==c,"board bring front");doc.Undo();check(doc.Items[0].Id==c,"board arrange undo");}
            });
            run("pen/automatic closure joins final endpoint and fills interior with chosen color",delegate{
                using(var c=new Input()){var pen=Pen(false,false);pen.Color=Color.Blue;pen.FillEnabled=true;pen.FillColor=Color.FromArgb(100,Color.Red);c.AddAnnotation(pen);
                    using(var open=c.Render()){check(open.GetPixel(180,210).ToArgb()==Color.White.ToArgb(),"open pen has closing edge");c.SetClosedPen(true);using(var closed=c.Render()){check(closed.GetPixel(180,210).B>closed.GetPixel(180,210).R+100,"closing edge missing");var interior=closed.GetPixel(260,180);check(interior.B>interior.R&&interior.R<250,"closed fill did not follow stroke color");closed.Save(Path.Combine(output,"closed-freehand-export.png"));}}
                    check(c.SelectedShape.Points.Count==4,"closure rewrote editable raw points");c.Undo();check(!c.AnnotationSnapshot[0].ClosedPen,"closure undo");c.Redo();check(c.AnnotationSnapshot[0].ClosedPen,"closure redo");
                }
            });
            run("pen/closed mode starts point polygon without drag nodes",delegate{
                using(var c=new Input()){c.SetTool(Tool.Pen);c.SetClosedPen(true);c.Down(new Point(120,120));c.MoveTo(new Point(340,120));c.Up(new Point(340,120));check(c.DrawingPolygon&&c.AnnotationCount==0&&c.PolygonPointCount==1,"point mode still uses dragging");c.Click(new PointF(340,120));c.Click(new PointF(340,320));check(c.CompletePolygon()&&c.SelectedShape.Polygon&&c.SelectedShape.ClosedPen&&!c.SelectedShape.SmoothPen&&c.SelectedShape.Points.Count==3,"point polygon not committed");c.SetTool(Tool.Pen);check(c.DefaultStyle.ClosedPen,"new polygons forgot mode");}
            });
            run("pen/closed path can be selected on closure edge and short strokes remain valid",delegate{
                using(var c=new Input()){c.AddAnnotation(Pen(true,false));c.SelectShape(null);c.Click(new PointF(180,210));check(c.SelectionCount==1,"closing edge cannot be selected");
                    c.AddAnnotation(ScreenCraftTests.Mark(Tool.Pen,500,100,500,100));c.EditSelected(s=>{s.Points=new List<PointF>{s.A};s.ClosedPen=true;});using(var image=c.Render())check(image.Width==900,"single node crash");}
            });
            run("rotation/render uses selected center and hit testing follows rotated shape",delegate{
                using(var c=new Input()){c.AddAnnotation(Rect(200,200,200,60));c.SetSelectedRotation(90);var shape=c.SelectedShape;var bounds=Painter.Bounds(shape);check(bounds.Height>190&&bounds.Width<75,"world bounds not rotated");using(var image=c.Render()){check(image.GetPixel(300,150).R>image.GetPixel(300,150).G+20,"rotated interior missing");check(image.GetPixel(210,230).ToArgb()==Color.White.ToArgb(),"unrotated ghost remains");}
                    c.SelectShape(null);c.Click(new PointF(300,150));check(c.HasSelection,"rotated shape cannot be selected");c.SelectShape(null);c.Click(new PointF(210,230));check(!c.HasSelection,"old location still hits");}
            });
            run("rotation/pointer-up alone commits snapped angle and one undo restores raw geometry",delegate{
                using(var c=new Input()){c.AddAnnotation(Rect(200,220,200,80));var before=c.SelectedShape.Clone();var center=Painter.RotationCenter(before);var handle=c.SelectionRotationHandle();c.Down(handle);c.Modifiers=Keys.Shift;var target=Painter.RotatePoint(handle,center,43);c.Up(Point.Round(target));check(Math.Abs(c.SelectedShape.Rotation-45)<.1,"rotation final coordinate or snap lost");check(c.SelectedShape.A==before.A&&c.SelectedShape.B==before.B,"rotation destroyed local geometry");c.Undo();check(c.AnnotationSnapshot.Single().Rotation==0,"rotation not single undo");c.Redo();check(Math.Abs(c.AnnotationSnapshot.Single().Rotation-45)<.1,"rotation redo");}
            });
            run("rotation/cancelled gesture restores pixels and existing redo branch",delegate{
                using(var c=new Input()){c.AddAnnotation(Rect(200,220,200,80));c.RotateSelected(15);c.Undo();var handle=c.SelectionRotationHandle();var pivot=Painter.RotationCenter(c.SelectedShape);c.Down(handle);c.MoveTo(Point.Round(Painter.RotatePoint(handle,pivot,60)));c.CancelGesture();check(c.AnnotationSnapshot[0].Rotation==0&&c.CanRedo,"cancel lost angle or redo");c.Redo();check(c.AnnotationSnapshot[0].Rotation==15,"old redo replaced");}
            });
            run("rotation/eight local resize handles keep opposite edge anchored in world coordinates",delegate{
                for(int i=0;i<8;i++)using(var c=new Input()){c.AddAnnotation(Rect(250,240,180,80));c.SetSelectedRotation(37);var old=c.SelectedShape.Clone();var handles=c.SelectionHandles();var opposite=handles[(i<4?(i+2)%4:i==4?6:i==5?7:i==6?4:5)];
                    var local=Canvas.BoxHandles(old.Box)[i];var delta=new PointF(i==0||i==3||i==7?-36:i==1||i==2||i==5?36:0,i==0||i==1||i==4?-24:i==2||i==3||i==6?24:0);var target=Painter.WorldPoint(old,new PointF(local.X+delta.X,local.Y+delta.Y));c.Down(handles[i]);c.MoveTo(Point.Round(target));c.Up(Point.Round(target));var after=c.SelectionHandles();var fixedHandle=after[(i<4?(i+2)%4:i==4?6:i==5?7:i==6?4:5)];check(Near(opposite,fixedHandle,2),"opposite rotated handle drift "+i);check(Math.Abs(c.SelectedShape.Rotation-37)<.01,"resize reset angle");}
            });
            run("rotation/multiple objects orbit common center with one atomic history step",delegate{
                using(var c=new Input()){Pair(c);var old=c.AnnotationSnapshot;var center=Painter.Center(c.SelectedBounds);c.RotateSelected(90);var all=c.AnnotationSnapshot;for(int i=0;i<all.Length;i++)check(Near(Painter.RotationCenter(all[i]),Painter.RotatePoint(Painter.RotationCenter(old[i]),center,90),.01f)&&all[i].Rotation==90,"group pivot incorrect");c.Undo();check(c.AnnotationSnapshot.All(s=>s.Rotation==0)&&c.AnnotationSnapshot[0].A==old[0].A&&c.AnnotationSnapshot[1].A==old[1].A,"group rotation undo");}
            });
            run("rotation/group pointer gesture snaps as one group and blocks arrange until release",delegate{
                using(var c=new Input()){Pair(c);var before=c.AnnotationSnapshot;var pivot=Painter.Center(c.SelectedBounds);var handle=c.SelectionRotationHandle();c.Down(handle);c.Modifiers=Keys.Shift;c.MoveTo(Point.Round(Painter.RotatePoint(handle,pivot,60)));c.ApplyArrangeKey(Keys.Control|Keys.Shift|Keys.OemOpenBrackets);check(c.AnnotationSnapshot.Select(s=>s.Id).SequenceEqual(before.Select(s=>s.Id)),"arrange changed active gesture");c.Up(Point.Round(Painter.RotatePoint(handle,pivot,60)));check(c.AnnotationSnapshot.All(s=>Math.Abs(s.Rotation-60)<.01),"group rotation snap");c.Undo();check(c.AnnotationSnapshot.All(s=>s.Rotation==0),"group gesture was not atomic");}
            });
            run("rotation/rotated move and repeated resize gestures use original coordinates",delegate{
                using(var c=new Input()){c.AddAnnotation(Rect(250,240,180,80));c.SetSelectedRotation(45);var old=c.SelectedShape.Clone();var start=Point.Round(Painter.RotationCenter(old));c.Down(start);c.MoveTo(new Point(start.X+30,start.Y+40));c.Up(new Point(start.X+30,start.Y+40));check(Near(c.SelectedShape.A,new PointF(old.A.X+30,old.A.Y+40),1),"world move rotated delta");var bounds=c.SelectedShape.Box;var handle=c.SelectionHandles()[5];c.Down(handle);c.MoveTo(new Point(handle.X+30,handle.Y+30));c.MoveTo(new Point(handle.X+60,handle.Y+60));c.Up(new Point(handle.X+60,handle.Y+60));check(c.SelectedShape.Box.Width>bounds.Width+75&&c.SelectedShape.Box.Height==bounds.Height,"resize accumulated drift");}
            });
            run("rotation/effects use rotated mask but sample original world pixels",delegate{
                using(var c=new Input()){using(var g=Graphics.FromImage(c.Img)){g.Clear(Color.Blue);g.FillRectangle(Brushes.Red,0,0,900,200);}var effect=ScreenCraftTests.Mark(Tool.Pixelate,250,260,450,320);effect.MosaicBlock=12;c.AddAnnotation(effect);c.SetSelectedRotation(90);using(var image=c.Render()){check(image.GetPixel(350,210).B>200&&image.GetPixel(350,210).R<30,"mosaic rotated source texture");check(image.GetPixel(270,290).B>200,"mosaic old location");}var spot=ScreenCraftTests.Mark(Tool.Spotlight,250,260,450,320);spot.EffectOpacity=70;c.AddAnnotation(spot);c.SetSelectedRotation(90);using(var image=c.Render()){check(image.GetPixel(350,210).B>200&&image.GetPixel(270,290).B<120,"spotlight ignored rotated aperture");}}
            });
            run("document/rotation closed freehand and cropped export survive save reopen",delegate{
                using(var c=new Input()){var s=Pen(true,true);s.FillEnabled=true;s.Rotation=27;c.AddAnnotation(s);var label=ScreenCraftTests.Mark(Tool.Text,450,300,450,300);label.Text="建筑分析";label.Rotation=315;c.AddAnnotation(label);c.Crop=new RectangleF(60,60,740,500);using(var doc=c.ExportDocument()){string path=Path.Combine(output,"旋转与闭合.sccraft");doc.Save(path);using(var read=AnnotationDocument.Open(path))using(var a=doc.Render())using(var b=read.Render()){check(read.Shapes[0].ClosedPen&&read.Shapes[0].Rotation==27&&read.Shapes[1].Rotation==315,"project discarded transform");check(Hash(a)==Hash(b),"project or crop changed rendered pixels");b.Save(Path.Combine(output,"rotated-project-export.png"));}}}
            });
            run("rotation/legacy defaults and invalid angle validation remain safe",delegate{
                var s=Rect(100,100,100,60);var dto=AnnotationDocument.ShapeDto.From(s);check(dto.ToShape().Rotation==0&&!dto.ToShape().ClosedPen,"legacy default transform");s.Rotation=float.NaN;bool rejected=false;try{AnnotationDocument.ValidateShape(s);}catch(InvalidDataException){rejected=true;}check(rejected,"NaN angle accepted");using(var c=new Input()){c.AddAnnotation(Rect(100,100,100,60));c.SetSelectedRotation(-45);check(c.SelectedShape.Rotation==315,"negative angle normalization");c.SetSelectedRotation(360);check(c.SelectedShape.Rotation==0,"full rotation normalization");}
            });
            run("rotation/text editing preserves angle and normal color roles",delegate{
                using(var c=new Input()){var label=ScreenCraftTests.Mark(Tool.Text,300,280,300,280);label.Text="旧文字";label.Color=Color.Teal;label.Rotation=30;c.AddAnnotation(label);c.EditNote();var input=c.Controls.OfType<TextBox>().Single();input.Text="新文字";check(input.ForeColor==Color.White&&input.BackColor.ToArgb()==Color.Teal.ToArgb(),"rotated editor colors");c.CommitText();check(c.SelectedShape.Rotation==30&&c.SelectedShape.Text=="新文字","text edit discarded transform");c.Undo();check(c.AnnotationSnapshot[0].Rotation==30&&c.AnnotationSnapshot[0].Text=="旧文字","text edit undo");}
            });
            run("transform UI/secondary closure and rotation plus inspector numeric angle are wired",delegate{
                using(var o=new CaptureOverlay(White(),new Rectangle(0,0,900,650),new Rectangle(30,30,800,540),null)){var c=o.AnnotationCanvas;var controls=Children(o).ToArray();c.SetTool(Tool.Pen);Click(controls.OfType<Button>().Single(b=>b.AccessibleName=="逐点多边形"));check(c.DefaultStyle.ClosedPen,"closure button disconnected");c.AddAnnotation(Pen(true,false));Click(controls.OfType<Button>().Single(b=>b.AccessibleName!=null&&b.AccessibleName.StartsWith("顺时针旋转 90°")));check(c.SelectedShape.Rotation==90,"rotation button disconnected");o.CreateControl();o.PerformLayout();using(var image=new Bitmap(900,650)){using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,o,o);image.Save(Path.Combine(output,"closed-pen-toolbar-offscreen.png"));}}
                using(var e=new CraftEditor(White())){var c=e.AnnotationCanvas;c.AddAnnotation(Rect(200,200,200,60));var control=Children(e).OfType<NumericUpDown>().Single(n=>n.AccessibleName=="旋转角度（°）");control.Value=32;check(c.SelectedShape.Rotation==32,"numeric rotation disconnected");c.SetTool(Tool.Pen);var button=Children(e).OfType<CaptureIconButton>().Single(b=>b.AccessibleName=="逐点多边形");Click(button);check(c.DefaultStyle.ClosedPen,"file editor closure control disconnected");e.CreateControl();e.PerformLayout();using(var image=new Bitmap(e.ClientSize.Width,e.ClientSize.Height)){using(var g=Graphics.FromImage(image))PreferencesPreview.Draw(g,e,e);image.Save(Path.Combine(output,"rotation-editor-offscreen.png"));}}
            });
            run("transform UI/offscreen selected closed shape retains rotated handles",delegate{
                using(var c=new Input()){var shape=Pen(true,true);shape.FillEnabled=true;shape.Color=Color.Teal;shape.Rotation=25;c.AddAnnotation(shape);var rect=Rect(480,260,210,130);rect.Rotation=330;c.AddAnnotation(rect);c.Preview(Path.Combine(output,"rotation-handles-offscreen.png"));check(c.SelectionHandles().Length==8,"rotated selection handles missing");check(!c.Visible||c.Parent==null,"probe used desktop");}
            });
        }
    }
}
