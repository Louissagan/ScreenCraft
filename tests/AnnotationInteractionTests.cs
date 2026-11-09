using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ScreenCraft
{
    static class AnnotationInteractionTests
    {
        sealed class Input:Canvas
        {
            internal Keys Modifiers;
            internal Input():base(Source()){InPlaceViewport=true;Size=new Size(900,650);ModifierSource=delegate{return Modifiers;};}
            internal void Down(Point p){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            internal void Move(Point p){OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));}
            internal void Up(Point p){OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));}
            internal void Click(int x,int y){Down(new Point(x,y));Up(new Point(x,y));}
            internal void Drag(Point a,Point b){Down(a);Up(b);}
        }
        static Bitmap Source(){var b=new Bitmap(900,650);using(var g=Graphics.FromImage(b))g.Clear(Color.FromArgb(238,241,245));return b;}
        static System.Collections.Generic.IEnumerable<Control> Children(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Children(c))yield return child;}}
        static void Click(Button b){typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,new object[]{EventArgs.Empty});}
        static void Pair(Input c){c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,100,100,220,220));c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,350,100,470,220));c.SetTool(Tool.Rect);}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("selection/Shift and Ctrl toggle objects and one color edit undoes together",delegate{
                using(var c=new Input()){Pair(c);c.Click(140,100);c.Modifiers=Keys.Shift;c.Click(390,100);check(c.SelectionCount==2,"Shift did not add");c.Modifiers=Keys.Control;c.Click(140,100);check(c.SelectionCount==1,"Ctrl did not remove");c.Click(140,100);check(c.SelectionCount==2,"Ctrl did not re-add");c.Modifiers=Keys.None;var old=c.AnnotationSnapshot[0].Color;c.SetColor(Color.Blue);check(c.AnnotationSnapshot.All(s=>s.Color.ToArgb()==Color.Blue.ToArgb()),"group color missed member");c.Undo();check(c.AnnotationSnapshot.All(s=>s.Color==old),"color undo was not atomic");}
            });
            run("selection/group fast drag cancel duplicate and delete are atomic",delegate{
                using(var c=new Input()){Pair(c);c.SelectAll();c.Drag(new Point(140,100),new Point(180,140));check(c.SelectionCount==2&&c.AnnotationSnapshot[0].A==new PointF(140,140)&&c.AnnotationSnapshot[1].A==new PointF(390,140),"group release coordinates");
                    var before=c.AnnotationSnapshot;c.Down(new Point(180,140));c.Move(new Point(210,190));c.CancelGesture();c.Up(new Point(210,190));check(c.AnnotationSnapshot[0].A==before[0].A&&c.AnnotationSnapshot[1].A==before[1].A,"cancel lost a member");
                    c.DuplicateSelected();check(c.AnnotationCount==4&&c.SelectionCount==2&&c.AnnotationSnapshot.Select(s=>s.Id).Distinct().Count()==4,"group duplicate identities");c.DeleteSelected();check(c.AnnotationCount==2,"group delete left member");c.Undo();check(c.AnnotationCount==4,"delete undo");c.Undo();check(c.AnnotationCount==2,"duplicate undo");}
            });
            run("selection/eight handles resize single axes and group geometry",delegate{
                using(var c=new Input()){c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,100,100,220,220));var h=c.SelectionHandles();check(h.Length==8,"four midpoints missing");c.Drag(h[5],new Point(h[5].X+80,h[5].Y));check(c.SelectedShape.Box==new RectangleF(100,100,200,120),"right handle changed vertical axis");h=c.SelectionHandles();c.Drag(h[4],new Point(h[4].X,h[4].Y-30));check(c.SelectedShape.Box==new RectangleF(100,70,200,150),"top handle changed horizontal axis");
                    c.AddAnnotation(ScreenCraftTests.Mark(Tool.Rect,400,100,520,220));c.SelectAll();h=c.SelectionHandles();check(h.Length==8,"group midpoints missing");var old=c.AnnotationSnapshot;c.Drag(h[5],new Point(h[5].X+100,h[5].Y));check(c.AnnotationSnapshot[0].Box.Width>old[0].Box.Width&&c.AnnotationSnapshot[1].Box.Width>old[1].Box.Width,"group resize stacked or skipped objects");c.Undo();check(c.AnnotationSnapshot[0].Box==old[0].Box&&c.AnnotationSnapshot[1].Box==old[1].Box,"group resize undo");}
            });
            run("labels/typing recoloring and reediting retain selected background and white glyphs",delegate{
                using(var c=new Input()){c.SetTool(Tool.Text);c.SetColor(Color.Purple);c.Click(100,100);var input=c.Controls.OfType<TextBox>().Single();input.Text="建筑分析";check(input.BackColor.ToArgb()==Color.Purple.ToArgb()&&input.ForeColor.ToArgb()==Color.White.ToArgb(),"initial editing colors wrong");c.SetColor(Color.Blue);check(input.BackColor.ToArgb()==Color.Blue.ToArgb()&&input.ForeColor.ToArgb()==Color.White.ToArgb()&&c.Typing,"palette changed text instead of background");c.CommitText();var shape=c.SelectedShape;check(shape.FillColor.ToArgb()==Color.Blue.ToArgb()&&shape.CornerRadius==6,"commit changed label style");c.EditNote();input=c.Controls.OfType<TextBox>().Single();check(input.BackColor.ToArgb()==Color.Blue.ToArgb()&&input.ForeColor.ToArgb()==Color.White.ToArgb(),"reedit changed color roles");c.CommitText();using(var image=c.Render())image.Save(Path.Combine(output,"unified-text-label.png"));}
            });
            run("labels/point number and cloud share note geometry and proportional leaders",delegate{
                using(var c=new Input()){c.SetTool(Tool.Callout);c.SetColor(Color.Teal);c.Click(120,180);c.Controls.OfType<TextBox>().Single().Text="材料与空间";c.CommitText();var body=Painter.NoteBounds(c.SelectedShape);c.SetMarkerSize(48);check(Math.Abs(Painter.LeaderWidth(c.SelectedShape)-4)<.01,"leader ratio not coupled");c.SetAnnotationMode(Tool.Counter);check(c.SelectedShape.Kind==Tool.Counter&&!c.SelectedShape.LeaderCounter&&c.SelectedShape.Number>0&&Painter.NoteBounds(c.SelectedShape).Location==body.Location,"number mode lost point or body");c.SetAnnotationMode(Tool.Cloud);check(c.SelectedShape.Kind==Tool.Cloud&&Painter.NoteBounds(c.SelectedShape).Location==body.Location&&Painter.NoteInk(c.SelectedShape)==Color.White,"cloud changed text box");
                    var h=c.SelectionHandles();check(h.Length==8,"note side handles missing");c.Drag(h[6],new Point(h[6].X,h[6].Y+70));check(c.SelectedShape.NoteHeight>=body.Height+60,"bottom text handle did not add height");using(var image=c.Render())image.Save(Path.Combine(output,"unified-cloud-comment.png"));}
            });
            run("output/rounded PNG preserves alpha interior and editable project undo",delegate{
                using(var c=new Input()){c.SetOutputCornerRadius(48);using(var image=c.Render()){check(image.GetPixel(0,0).A==0&&image.GetPixel(450,325).ToArgb()==c.Img.GetPixel(450,325).ToArgb(),"rounded export destroyed interior or retained opaque corner");image.Save(Path.Combine(output,"rounded-export.png"));}
                    using(var doc=c.ExportDocument()){var path=Path.Combine(output,"rounded-export.sccraft");doc.Save(path);using(var reopened=AnnotationDocument.Open(path))using(var image=reopened.Render())check(reopened.OutputCornerRadius==48&&image.GetPixel(0,0).A==0,"project lost round corners");}
                    c.Undo();check(c.OutputCornerRadius==0,"radius undo");c.Redo();check(c.OutputCornerRadius==48,"radius redo");}
            });
            run("toolbar/old order migrates and hidden effects still expose editable parameters",delegate{
                var legacy=new ToolbarSettings{Order=new[]{"Rect","Ellipse","Line","Arrow","Pen","Text","Counter","Cloud","Pixelate","Highlight","Redact"},Hidden=new[]{"Highlight","Redact"}};legacy.Keys=legacy.Keys.Where(p=>legacy.Order.Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value);var migrated=ToolbarSettings.Safe(legacy);check(migrated.Order.Length==ToolbarSettings.Known.Length&&migrated.Order[0]=="Rect"&&migrated.Keys["Arrow"]=="A","legacy toolbar reset");
                using(var overlay=new CaptureOverlay(Source(),new Rectangle(0,0,900,650),new Rectangle(50,50,740,400),new[]{new Rectangle(0,0,900,650)})){
                    var config=new ToolbarSettings();config.Move(Array.IndexOf(config.Order,"Crop"),0);config.Hidden=new[]{"Spotlight","Counter","Cloud"};overlay.ApplyToolbarConfiguration(config);var menu=overlay.PrepareMoreMenu();menu.Items.OfType<CraftMenuItem>().Single(i=>i.Text.StartsWith("聚光灯")).PerformClick();check(overlay.AnnotationCanvas.Tool==Tool.Spotlight,"hidden effect unreachable");var slider=Children(overlay).OfType<CaptureSlider>().Single(x=>x.AccessibleName=="线宽或字号");check(overlay.IsOptionShown(slider),"hidden tool lost properties");slider.Value=43;check(overlay.AnnotationCanvas.DefaultStyle.EffectOpacity==43,"spotlight opacity disconnected");
                    var crop=Children(overlay).OfType<Button>().Single(b=>b.AccessibleName=="裁剪与导出圆角");Click(crop);var radius=Children(overlay).OfType<CaptureSlider>().Single(x=>x.AccessibleName=="圆角半径快捷调整");radius.Value=36;check(overlay.AnnotationCanvas.OutputCornerRadius==36,"rounded output control disconnected");using(var image=overlay.RenderSyntheticPreview())image.Save(Path.Combine(output,"toolbar-rounded-export.png"));
                }
            });
            run("watermark/parameters reopen prefilled and update the same annotation",delegate{
                using(var c=new Input()){c.AddAnnotation(WatermarkDialog.Create(c.Crop,"校核",24,20,-25,100,Color.Gray));var id=c.SelectedShape.Id;using(var dialog=new WatermarkDialog(c)){var controls=Children(dialog).ToArray();check(controls.OfType<TextBox>().Single(x=>x.AccessibleName=="水印文字").Text=="校核","existing text not loaded");controls.OfType<NumericUpDown>().Single(x=>x.AccessibleName=="水印角度").Value=35;Click(controls.OfType<Button>().Single(b=>b.Text=="应用修改"));check(c.AnnotationCount==1&&c.SelectedShape.Id==id&&c.SelectedShape.WatermarkAngle==35,"edit created another watermark");}}
            });
        }
    }
}
