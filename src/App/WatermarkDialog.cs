using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class WatermarkDialog:Form
    {
        readonly TextBox text=new TextBox{MaxLength=160,AccessibleName="水印文字"};readonly NumericUpDown font=Number(8,96,24,"水印字号"),opacity=Number(5,80,20,"水印不透明度"),angle=Number(-90,90,-25,"水印角度"),spacing=Number(20,600,100,"水印间距");readonly InspectorColorButton color=new InspectorColorButton{Text="选择颜色…",Swatch=Color.FromArgb(70,76,88)};readonly Label status=new Label{ForeColor=Color.FromArgb(181,47,56)};
        internal WatermarkDialog(Canvas canvas)
        {
            var existing=canvas.SelectedShape!=null&&canvas.SelectedShape.Kind==Tool.Watermark?canvas.SelectedShape:null;
            if(existing!=null){text.Text=existing.Text;font.Value=Math.Max(font.Minimum,Math.Min(font.Maximum,(decimal)existing.FontPx));opacity.Value=Math.Max(opacity.Minimum,Math.Min(opacity.Maximum,(decimal)(existing.FillColor.A*100.0/255)));angle.Value=(decimal)existing.WatermarkAngle;spacing.Value=(decimal)existing.WatermarkSpacing;color.Swatch=existing.Color;}
            Text="ScreenCraft · 文字水印";Icon=ShotStack.AppIcon;Font=new Font("Microsoft YaHei UI",9);BackColor=PreferencesLook.Background;ClientSize=new Size(460,470);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var title=new Label{Text="重复文字水印",Font=new Font(Font.FontFamily,14,FontStyle.Bold),Bounds=new Rectangle(24,18,400,32)};var group=new PreferencesGroup{Location=new Point(22,62),Width=416};group.AddRow(new PreferencesRow("文字",new PreferencesField(text,230),230));group.AddRow(new PreferencesRow("字号（px）",new PreferencesField(font,100),100));group.AddRow(new PreferencesRow("不透明度（%）",new PreferencesField(opacity,100),100));group.AddRow(new PreferencesRow("角度（°）",new PreferencesField(angle,100),100));group.AddRow(new PreferencesRow("间距（px）",new PreferencesField(spacing,100),100));group.AddRow(new PreferencesRow("颜色",color,150));var apply=new PreferencesButton{Text=existing==null?"添加水印":"应用修改",Primary=true,Location=new Point(326,420)};status.Bounds=new Rectangle(24,420,290,38);Controls.AddRange(new Control[]{title,group,status,apply});
            color.Click+=delegate{CraftColors.Show(color,color.Swatch,delegate(Color chosen){color.Swatch=chosen;color.Invalidate();});};apply.Click+=delegate{try{var replacement=Create(canvas.Crop,text.Text,(int)font.Value,(int)opacity.Value,(int)angle.Value,(int)spacing.Value,color.Swatch);if(existing==null)canvas.AddAnnotation(replacement);else canvas.EditSelected(delegate(Shape s){s.Text=replacement.Text;s.FontPx=replacement.FontPx;s.Color=replacement.Color;s.FillColor=replacement.FillColor;s.WatermarkAngle=replacement.WatermarkAngle;s.WatermarkSpacing=replacement.WatermarkSpacing;});Close();}catch(Exception ex){status.Text=ex.Message;}};
        }
        static NumericUpDown Number(int min,int max,int value,string name){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,BorderStyle=BorderStyle.None,AccessibleName=name};}
        internal static Shape Create(RectangleF range,string text,int font,int percent,int angle,int spacing,Color color){if(string.IsNullOrWhiteSpace(text))throw new ArgumentException("请输入水印文字。");var mark=new Shape{Kind=Tool.Watermark,A=range.Location,B=new PointF(range.Right,range.Bottom),Text=text,Width=3,FontPx=font,Color=color,FillColor=Color.FromArgb((int)Math.Round(percent*255.0/100),color),WatermarkAngle=angle,WatermarkSpacing=spacing};AnnotationDocument.ValidateShape(mark);return mark;}
    }
}
