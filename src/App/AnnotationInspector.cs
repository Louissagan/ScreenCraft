using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Stackshot
{
    internal sealed class InspectorColorButton:PreferencesPaintButton
    {
        public Color Swatch;
        public InspectorColorButton(){Size=new Size(228,34);BackColor=PreferencesLook.Background;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;PreferencesLook.Fill(g,ClientRectangle,BackColor);PreferencesLook.Round(g,ClientRectangle,Color.White,8,PreferencesLook.Line);g.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Swatch))g.FillEllipse(b,Width-30,7,20,20);PreferencesLook.Text(g,Text,Font,new Rectangle(10,0,Width-46,Height),PreferencesLook.Ink,StringAlignment.Near);}
    }
    public sealed class AnnotationInspector : UserControl
    {
        readonly Canvas canvas;
        readonly FlowLayoutPanel fields=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(12)};
        readonly Label target=new Label{AutoSize=true,MaximumSize=new Size(235,0)};
        readonly NumericUpDown width=Number(.25m,128,3,2),radius=Number(0,1000,0,0),alpha=Number(0,100,24,0),font=Number(8,300,22,0),number=Number(0,99999,1,0);
        readonly NumericUpDown x=Number(-100000,100000,0,0),y=Number(-100000,100000,0,0),w=Number(1,100000,100,0),h=Number(1,100000,100,0);
        readonly CheckBox fill=new PreferencesSwitch{Text="启用填充"},simpleArrow=new PreferencesSwitch{Text="细线箭头"};
        readonly PreferencesChoice dash=new PreferencesChoice("实线","虚线","点线","点划线","双点划线");
        readonly NumericUpDown markerSize=Number(8,200,28,0);

        readonly NumericUpDown noteWidth=Number(60,800,320,0);readonly PreferencesSwitch noteAuto=new PreferencesSwitch();
        readonly TextBox note=new TextBox{Multiline=true,Width=218,Height=90,ScrollBars=ScrollBars.Vertical,MaxLength=20000,AcceptsReturn=true};
        readonly InspectorColorButton strokeColor=new InspectorColorButton{Text="颜色…"};PreferencesGroup group;
        bool syncing;
        public AnnotationInspector(Canvas c)
        {
            canvas=c;Dock=DockStyle.Right;Width=280;BackColor=PreferencesLook.Background;Font=new Font("Microsoft YaHei UI",9);fields.BackColor=BackColor;Controls.Add(fields);
            fields.Controls.Add(new Label{Text="标注属性",AutoSize=true,Font=new Font(SystemFonts.MessageBoxFont,FontStyle.Bold)});fields.Controls.Add(target);
            BeginGroup();Labelled("线宽（px）",width);Labelled("圆角半径（px）",radius);Labelled("线型",dash);Labelled("细线箭头",simpleArrow);Labelled("启用填充",fill);
            fields.Controls.Add(strokeColor);BeginGroup();Labelled("填充不透明度（%）",alpha);Labelled("字号（px）",font);Labelled("序号号码",number);Labelled("点大小／引线比例",markerSize);
            Labelled("文本宽度（px）",noteWidth);Labelled("随输入扩展",noteAuto);
            BeginGroup();
            Labelled("位置 X",x);Labelled("位置 Y",y);Labelled("宽度（px）",w);Labelled("高度（px）",h);
            var applyGeometry=new PreferencesButton{Text="应用位置与大小",Width=228};applyGeometry.Click+=delegate{if(canvas.SelectedShape!=null)canvas.ResizeSelected(new RectangleF((float)x.Value,(float)y.Value,(float)w.Value,(float)h.Value));};fields.Controls.Add(applyGeometry);
            note.AccessibleName="文字／气泡说明";var noteField=new PreferencesField(note,228){Height=100};fields.Controls.Add(noteField);
            var applyText=new PreferencesButton{Text="应用文字与序号",Width=228};applyText.Click+=delegate{string desired=note.Text;int n=(int)number.Value;canvas.CommitText();if(canvas.SelectedShape!=null)canvas.EditSelected(delegate(Shape s){s.Text=desired;s.Number=n;});};fields.Controls.Add(applyText);
            var inline=new PreferencesButton{Text="在图上编辑文字",Width=215,Height=32};inline.Click+=delegate{canvas.EditNote();};fields.Controls.Add(inline);
            var remove=new PreferencesButton{Text="删除选中标注",Width=215,Height=32};remove.Click+=delegate{canvas.DeleteSelected();};fields.Controls.Add(remove);
            var forward=new PreferencesButton{Text="标注上移一层",Width=215,Height=32};forward.Click+=delegate{canvas.ReorderSelected(1);};fields.Controls.Add(forward);
            var backward=new PreferencesButton{Text="标注下移一层",Width=215,Height=32};backward.Click+=delegate{canvas.ReorderSelected(-1);};fields.Controls.Add(backward);
            fields.Controls.Add(new Label{Text="在图上点选标注后可拖动、调整控制点；双击文字、序号或云线可编辑说明。",AutoSize=true,MaximumSize=new Size(218,0),Margin=new Padding(0,12,0,12)});
            width.ValueChanged+=delegate{Change(delegate(Shape s){s.Width=(float)width.Value;},delegate{c.DefaultStyle.Width=(float)width.Value;});};
            radius.ValueChanged+=delegate{Change(delegate(Shape s){s.CornerRadius=(float)radius.Value;},delegate{c.DefaultStyle.Radius=(float)radius.Value;});};
            alpha.ValueChanged+=delegate{Change(delegate(Shape s){s.FillColor=Color.FromArgb((int)Math.Round((double)alpha.Value*255/100),s.FillColor);},delegate{c.DefaultStyle.FillColor=Color.FromArgb((int)Math.Round((double)alpha.Value*255/100),c.DefaultStyle.FillColor);});};
            font.ValueChanged+=delegate{if(!syncing)c.SetTextSize((float)font.Value);};
            markerSize.ValueChanged+=delegate{if(!syncing)c.SetMarkerSize((float)markerSize.Value);};
            noteWidth.ValueChanged+=delegate{if(!syncing)canvas.SetNoteWidth((float)noteWidth.Value);};noteAuto.CheckedChanged+=delegate{if(!syncing)canvas.SetNoteAutoWidth(noteAuto.Checked);};
            fill.CheckedChanged+=delegate{if(!syncing)c.SetShapeFill(fill.Checked);};
            simpleArrow.CheckedChanged+=delegate{Change(delegate(Shape s){s.SimpleArrow=simpleArrow.Checked;},delegate{c.DefaultStyle.SimpleArrow=simpleArrow.Checked;});};
            dash.SelectionChanged+=delegate{Change(delegate(Shape s){s.Dash=(DashStyle)dash.SelectedIndex;},delegate{c.DefaultStyle.Dash=(DashStyle)dash.SelectedIndex;});};
            strokeColor.Click+=delegate{CraftColors.Show(strokeColor,c.ActiveColor,c.SetColor,delegate{c.BeginColorPick(c.SetColor);c.Focus();});};
            c.StateChanged+=CanvasStateChanged;UpdateValues();
        }
        void CanvasStateChanged(object sender,EventArgs e){UpdateValues();}
        void Change(Action<Shape> selected,Action defaults){if(syncing)return;if(canvas.SelectedShape!=null)canvas.EditSelected(selected);else{defaults();UpdateValues();}}
        void BeginGroup(){group=new PreferencesGroup{Width=228,Margin=new Padding(0,8,0,8)};fields.Controls.Add(group);}
        void Labelled(string label,Control control){control.AccessibleName=label;Control editor=control;if(control is NumericUpDown)editor=new PreferencesField(control,88);string caption=label.Replace("（px）","").Replace("填充不透明度（%）","透明度").Replace("字号／序号大小","字号");group.AddRow(new PreferencesRow(caption,editor,control is CheckBox?42:control is NumericUpDown?88:102));}
        static NumericUpDown Number(decimal min,decimal max,decimal value,int places){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,DecimalPlaces=places,Increment=places>0?.25m:1,Width=102,BorderStyle=BorderStyle.None};}
        static void Value(NumericUpDown input,decimal value){input.Value=Math.Min(input.Maximum,Math.Max(input.Minimum,value));}
        void UpdateValues()
        {
            if(IsDisposed)return;syncing=true;
            try{
                var s=canvas.SelectedShape;var d=canvas.DefaultStyle;
                target.Text=s==null?"下一笔的默认属性":canvas.SelectionCount>1?"已选中 "+canvas.SelectionCount+" 个标注":"已选中："+ToolbarSettings.Name(s.Kind.ToString());
                Value(width,(decimal)(s==null?d.Width:s.Width));Value(radius,(decimal)(s==null?d.Radius:s.CornerRadius));
                Value(font,(decimal)canvas.ActiveTextSize);Value(alpha,(decimal)((s==null?d.FillColor:s.FillColor).A*100.0/255));
                Value(markerSize,(decimal)canvas.ActiveMarkerSize);markerSize.Enabled=canvas.ActiveKind==Tool.Callout||canvas.ActiveKind==Tool.Counter;
                Value(noteWidth,(decimal)canvas.ActiveNoteWidth);noteAuto.Checked=s==null?d.NoteAutoWidth:s.NoteAutoWidth;
                fill.Checked=s==null?d.FillEnabled:s.FillEnabled;simpleArrow.Checked=s==null?d.SimpleArrow:s.SimpleArrow;dash.SelectedIndex=(int)(s==null?d.Dash:s.Dash);
                strokeColor.Swatch=canvas.ActiveColor;strokeColor.Invalidate();
                bool text=canvas.ActiveKind==Tool.Text||canvas.ActiveHasNote;
                font.Enabled=text||canvas.ActiveKind==Tool.Watermark;noteWidth.Enabled=noteAuto.Enabled=text;
                fill.Enabled=alpha.Enabled=canvas.ActiveKind==Tool.Rect||canvas.ActiveKind==Tool.Ellipse||canvas.ActiveKind==Tool.Cloud;
                width.Enabled=!text||canvas.ActiveKind==Tool.Cloud;radius.Enabled=canvas.ActiveKind==Tool.Rect;
                number.Enabled=s!=null&&s.Kind==Tool.Counter;note.Enabled=s!=null&&(s.Kind==Tool.Text||s.Kind==Tool.Watermark||Painter.HasNote(s.Kind));
                Value(number,s==null?1:s.Number);note.Text=canvas.ActiveText;
                if(s!=null){var b=canvas.SelectionCount>1?canvas.SelectedBounds:s.Kind==Tool.Text?Painter.TextBounds(s):s.Kind==Tool.Counter?new RectangleF(s.A.X-Painter.CounterRadius(s),s.A.Y-Painter.CounterRadius(s),Painter.CounterRadius(s)*2,Painter.CounterRadius(s)*2):s.Box;Value(x,(decimal)b.X);Value(y,(decimal)b.Y);Value(w,(decimal)b.Width);Value(h,(decimal)b.Height);}
            }finally{syncing=false;}
        }
        protected override void Dispose(bool disposing){if(disposing)canvas.StateChanged-=CanvasStateChanged;base.Dispose(disposing);}
    }
}
