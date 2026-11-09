using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class FeedbackPolishTests
    {
        [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr window,int index);
        sealed class Input:Canvas
        {
            internal Input():base(Source()){Size=new Size(800,600);InPlaceViewport=true;ModifierSource=delegate{return Keys.None;};}
            internal void Draw(PointF a,PointF b){var p=ImageToControl(a);OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));p=ImageToControl(b);OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
        }
        static Bitmap Source(){var b=new Bitmap(800,600);using(var g=Graphics.FromImage(b))g.Clear(Color.White);return b;}
        static void Click(Button button){typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});}
        public static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("fill/new rectangle ellipse and cloud follow outline even after tool switches",delegate{
                foreach(var kind in new[]{Tool.Rect,Tool.Ellipse,Tool.Cloud})using(var c=new Input()){
                    c.SetColor(Color.Blue);c.DefaultStyle.FillColor=Color.FromArgb(60,Color.DeepPink);c.SetTool(kind);c.SetShapeFill(true);
                    c.Draw(new PointF(100,100),new PointF(320,250));var s=c.SelectedShape;
                    check(s!=null&&s.FillEnabled&&s.FillColor.ToArgb()==Color.FromArgb(60,Color.Blue).ToArgb(),"stale pink fill on "+kind);
                    using(var image=c.Render()){var center=image.GetPixel(210,175);check(center.B>245&&Math.Abs(center.R-195)<2&&Math.Abs(center.G-195)<2,"render does not use translucent outline hue");image.Save(Path.Combine(output,"linked-fill-"+kind+".png"));}
                }
            });
            run("fill/recolor and toggle repair stale fill while preserving alpha and undo redo",delegate{
                using(var c=new Input()){
                    var mark=ScreenCraftTests.Mark(Tool.Rect,100,100,320,250);mark.Color=Color.Blue;mark.FillColor=Color.FromArgb(80,Color.DeepPink);c.AddAnnotation(mark);
                    c.SetShapeFill(true);check(c.SelectedShape.FillColor.ToArgb()==Color.FromArgb(80,Color.Blue).ToArgb(),"enabling fill reused stale hue");
                    c.SetColor(Color.Green);check(c.SelectedShape.FillColor.ToArgb()==Color.FromArgb(80,Color.Green).ToArgb(),"recolor changed alpha or missed hue");
                    c.Undo();check(c.AnnotationSnapshot.Single().FillColor.ToArgb()==Color.FromArgb(80,Color.Blue).ToArgb(),"undo did not restore linked colors");
                    c.Redo();check(c.AnnotationSnapshot.Single().FillColor.ToArgb()==Color.FromArgb(80,Color.Green).ToArgb(),"redo lost fill");
                    using(var doc=c.ExportDocument()){var file=Path.Combine(output,"linked-fill.sccraft");doc.Save(file);using(var opened=AnnotationDocument.Open(file))check(opened.Shapes[0].FillColor.ToArgb()==Color.FromArgb(80,Color.Green).ToArgb(),"save lost fill");}
                }
            });
            run("fill/cloud palette changes outline and fill outside text editing",delegate{
                using(var overlay=new CaptureOverlay(Source(),new Rectangle(0,0,800,600),new Rectangle(0,0,800,600),new[]{new Rectangle(0,0,800,600)},null,70)){
                    var mark=ScreenCraftTests.Mark(Tool.Cloud,100,100,320,250);mark.Text="说明";mark.FillEnabled=true;mark.FillColor=Color.FromArgb(60,Color.DeepPink);mark.HasNoteTextColor=true;mark.NoteTextColor=Color.Black;
                    overlay.AnnotationCanvas.AddAnnotation(mark);var menu=overlay.PrepareColorMenu();var palette=(AnnotationPalette)((ToolStripControlHost)menu.Items[0]).Control;
                    var blue=palette.Controls.OfType<PaletteSwatch>().Single(s=>s.Ink.ToArgb()==ColorTranslator.FromHtml("#0099FF").ToArgb());Click(blue);
                    var current=overlay.AnnotationCanvas.SelectedShape;check(current.Color==blue.Ink&&current.FillColor.ToArgb()==Color.FromArgb(60,blue.Ink).ToArgb()&&current.NoteTextColor==Color.White,"palette mixed outline and note roles");
                }
            });
            run("counter/default number sits at clicked point and fast drag releases at final point",delegate{
                using(var c=new Input()){
                    c.SetTool(Tool.Counter);var at=new PointF(240,200);c.Draw(at,at);var s=c.SelectedShape;
                    check(!s.LeaderCounter&&s.A==at&&s.B==at&&string.IsNullOrEmpty(s.Text)&&s.Number==1,"number became a separate text box");
                    c.CommitText();c.Draw(at,new PointF(350,280));s=c.SelectedShape;check(!s.LeaderCounter&&s.A==new PointF(350,280)&&s.A==s.B,"fast counter move missed mouse up");
                    using(var image=c.Render())image.Save(Path.Combine(output,"counter-on-point.png"));
                }
            });
            run("startup/scaled toast remains opaque during animation with sharp pixel fonts",delegate{
                foreach(float scale in new[]{1f,1.25f,1.5f,2f})using(var toast=new StartupToast(new Rectangle(-3000,0,3000,2000),"Ctrl+Shift+PrintScreen",false,scale)){
                    check(toast.Size==new Size((int)Math.Round(380*scale),(int)Math.Round(92*scale))&&toast.Font.Unit==GraphicsUnit.Pixel&&Math.Abs(toast.Font.Size-13*scale)<.01,"toast did not scale font and layout together");
                    var handle=toast.Handle;foreach(int ms in new[]{0,90,180,2500,2660}){check(toast.Advance(TimeSpan.FromMilliseconds(ms))&&toast.Opacity==1&&(GetWindowLong(handle,-20)&0x80000)==0,"animated text became a layered bitmap");}
                    check(!toast.Advance(TimeSpan.FromMilliseconds(2670)),"toast did not expire");
                    using(var image=toast.RenderOffscreen())image.Save(Path.Combine(output,"startup-toast-"+(int)(scale*100)+".png"));
                }
            });
        }
    }
}
