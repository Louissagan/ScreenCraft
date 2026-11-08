using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace Stackshot
{
    static class ColorPickerTests
    {
        static void Event(Control control,string method,object args){typeof(Control).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(control,new[]{args});}
        static void Click(Button button){typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(button,new object[]{EventArgs.Empty});}
        static void Drag(Control control,Point from,Point to){Event(control,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,from.X,from.Y,0));Event(control,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,to.X,to.Y,0));}
        internal static void Run(Action<string,Action> run,Action<bool,string> check,string output)
        {
            run("color picker/HSV roundtrip preserves preset RGB and grayscale",delegate{
                foreach(var hex in AnnotationColors.CommonHex.Concat(AnnotationColors.AnalysisHex).Concat(new[]{"171717","ABCDEF","010203"})){
                    var c=ColorTranslator.FromHtml("#"+hex);double h=210,s,v;AnnotationColors.ToHsv(c,ref h,out s,out v);check(AnnotationColors.FromHsv(h,s,v).ToArgb()==c.ToArgb(),"HSV changed "+hex);
                }
            });
            run("color picker/drag final coordinates and keyboard preview do not commit",delegate{
                int applied=0;using(var palette=new AnnotationPalette(Color.Red,delegate{applied++;},null)){
                    palette.SetExpanded(true);var editor=palette.Editor;Drag(editor.Spectrum,new Point(50,50),new Point(editor.Spectrum.Width-1,0));check(editor.Preview.ToArgb()==Color.Red.ToArgb()&&applied==0,"drag committed or missed final position");
                    Drag(editor.HueStrip,Point.Empty,new Point((editor.HueStrip.Width-1)/2,5));check(editor.Preview.G>245&&editor.Preview.B>240&&applied==0,"hue did not update preview");
                    Event(editor.Spectrum,"OnKeyDown",new KeyEventArgs(Keys.Down|Keys.Shift));check(editor.Preview.G<245&&applied==0,"keyboard brightness did not work");
                }
            });
            run("color picker/RGB HEX validation and apply create one selected color",delegate{
                int applied=0;Color selected=Color.Empty;using(var palette=new AnnotationPalette(Color.Red,delegate(Color c){selected=c;applied++;},null)){
                    palette.SetExpanded(true);var editor=palette.Editor;var apply=editor.Controls.OfType<Button>().Single();editor.Hex.Text="#85C4C2";
                    check(editor.Rgb[0].Text=="133"&&editor.Rgb[1].Text=="196"&&editor.Rgb[2].Text=="194"&&applied==0,"HEX failed to sync RGB");
                    editor.Rgb[0].Text="999";Click(apply);check(!apply.Enabled&&applied==0,"invalid RGB committed");editor.Rgb[0].Text="100";Click(apply);check(applied==1&&selected.ToArgb()==Color.FromArgb(100,196,194).ToArgb(),"RGB apply lost value");
                    editor.Hex.Text="bad";Click(apply);check(applied==1&&!apply.Enabled,"partial HEX committed");palette.SetCurrent(selected);check(apply.Enabled&&editor.Hex.Text=="#64C4C2","reopen retained invalid edit");
                }
            });
            run("color picker/popup reopens without pending changes or disposed native handle",delegate{
                using(var owner=new Form())using(var anchor=new Button())using(var popup=new ColorPopup(anchor)){
                    owner.Controls.Add(anchor);int applied=0;var menu=popup.Prepare(Color.Red,delegate{applied++;});var handle=menu.Handle;
                    for(int i=0;i<20;i++){popup.Palette.SetExpanded(true);popup.Palette.Editor.Hex.Text="#59899F";PopupAndCaptureFeedbackTests.Closed(menu);popup.Prepare(Color.Red,delegate{applied++;});check(!popup.Palette.Expanded&&popup.Palette.Editor.Preview.ToArgb()==Color.Red.ToArgb()&&applied==0,"dismiss applied a draft");check(menu==popup.Menu&&menu.Handle==handle&&!menu.IsDisposed,"native popup lifetime");}
                    owner.Dispose();check(menu.IsDisposed,"owner disposal leaked palette");
                }
            });
            run("color picker/negative-screen placement keeps expanded popup above bottom toolbar",delegate{
                var area=new Rectangle(-1920,-100,1920,1040);var size=new Size(380,478);var at=ColorPopup.Placement(new Point(-500,900),new Point(-500,930),size,area);check(area.Contains(new Rectangle(at,size))&&at.Y<900,"picker escaped negative monitor");
            });
            run("color picker/presets and custom editor render at four DPI sizes",delegate{
                foreach(float scale in new[]{1f,1.25f,1.5f,2f})using(var palette=new AnnotationPalette(AnnotationColors.Default,delegate{},delegate{},scale)){
                    check(palette.Width==(int)Math.Round(304*scale),"palette DPI width");using(var b=palette.RenderOffscreen())b.Save(Path.Combine(output,"palette-presets-"+(int)(scale*100)+".png"));
                    palette.SetExpanded(true);check(palette.ClientRectangle.Contains(palette.Editor.Bounds),"custom editor clipped");using(var b=palette.RenderOffscreen())b.Save(Path.Combine(output,"palette-custom-"+(int)(scale*100)+".png"));
                }
            });
        }
    }
}
