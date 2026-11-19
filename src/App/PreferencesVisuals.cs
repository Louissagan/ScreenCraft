using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal static class PreferencesLook
    {
        internal static readonly Color Background=Color.FromArgb(242,243,245),Header=Color.FromArgb(242,243,245),Input=Color.FromArgb(245,246,247),Ink=Color.FromArgb(37,40,44),Muted=Color.FromArgb(98,103,112),Line=Color.FromArgb(224,227,231);
        internal static void Fill(Graphics g,Rectangle r,Color color){var state=g.Save();g.SmoothingMode=SmoothingMode.None;using(var brush=new SolidBrush(color))g.FillRectangle(brush,r);g.Restore(state);}
        internal static void Text(Graphics g,string text,Font font,Rectangle bounds,Color color,StringAlignment align=StringAlignment.Center){var state=g.Save();g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var brush=new SolidBrush(color))using(var format=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(text,font,brush,bounds,format);g.Restore(state);}
        internal static void Round(Graphics g,Rectangle r,Color fill,int radius,Color? edge){if(r.Width<2||r.Height<2)return;var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Theme.Round(new RectangleF(r.X+.5f,r.Y+.5f,r.Width-1,r.Height-1),radius))using(var brush=new SolidBrush(fill)){g.FillPath(brush,path);if(edge.HasValue)using(var pen=new Pen(edge.Value))g.DrawPath(pen,path);}g.Restore(state);}
        internal static void FloatingCard(Graphics g,Rectangle bounds,int radius){for(int i=3;i>0;i--){var shadow=Rectangle.Inflate(bounds,-2+i/2,-2+i/2);shadow.Offset(0,2);Round(g,shadow,Color.FromArgb(4,25,32,44),radius+i,null);}Round(g,Rectangle.Inflate(bounds,-2,-2),Color.White,radius,Line);}
        internal static void PaintSurface(Graphics g,Size size,int header=126,int footer=64){g.Clear(Background);using(var brush=new SolidBrush(Header))g.FillRectangle(brush,0,0,size.Width,header);using(var line=new Pen(Line)){g.DrawLine(line,0,header-1,size.Width,header-1);g.DrawLine(line,0,size.Height-footer,size.Width,size.Height-footer);}using(var border=new Pen(Line))g.DrawRectangle(border,0,0,size.Width-1,size.Height-1);}
    }
    // WM_PRINT skips children whose parent is hidden. Paint each actual control separately without showing it.
    internal static class PreferencesPreview
    {
        static readonly MethodInfo paint=typeof(Control).GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo backgroundPaint=typeof(Control).GetMethod("OnPaintBackground",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo drawItem=typeof(ListBox).GetMethod("OnDrawItem",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo ownState=typeof(Control).GetMethod("GetState",BindingFlags.Instance|BindingFlags.NonPublic);
        internal static void Draw(Graphics g,Control root,Control selectedPage)
        {
            using(var layer=Render(root,selectedPage))g.DrawImageUnscaled(layer,0,0);
        }
        static Bitmap Render(Control root,Control selectedPage)
        {
            // GDI TextRenderer does not preserve ARGB alpha. Opaque per-control layers avoid compositing text artifacts.
            var layer=new Bitmap(Math.Max(1,root.Width),Math.Max(1,root.Height),System.Drawing.Imaging.PixelFormat.Format24bppRgb);using(var g=Graphics.FromImage(layer)){
                Control ancestor=root;Color background=root.BackColor;while(background.A<255&&ancestor.Parent!=null){ancestor=ancestor.Parent;background=ancestor.BackColor;}if(background.A<255)background=PreferencesLook.Background;
                using(var brush=new SolidBrush(background))g.FillRectangle(brush,root.ClientRectangle);
                bool native=root is TextBox||root is ComboBox||root is CheckedListBox||root is TrackBar;
                var text=root as TextBox;var combo=root as ComboBox;var list=root as ListBox;
                // Native text HWNDs skip text when hidden. Read the actual field state for the explicitly synthetic preview.
                if(text!=null)PreferencesLook.Text(g,text.Text,text.Font,text.ClientRectangle,text.ForeColor,StringAlignment.Near);
                else if(combo!=null){var r=combo.ClientRectangle;PreferencesLook.Text(g,combo.Text,combo.Font,new Rectangle(3,0,Math.Max(1,r.Width-22),r.Height),combo.ForeColor,StringAlignment.Near);using(var p=new Pen(PreferencesLook.Muted,1.5f))g.DrawLines(p,new[]{new Point(r.Right-15,r.Height/2-2),new Point(r.Right-11,r.Height/2+2),new Point(r.Right-7,r.Height/2-2)});}
                else if(list!=null&&!(list is CheckedListBox)&&list.DrawMode==DrawMode.OwnerDrawFixed){for(int i=list.TopIndex;i<list.Items.Count;i++){var row=new Rectangle(0,(i-list.TopIndex)*list.ItemHeight,list.Width,list.ItemHeight);if(row.Top>=list.Height)break;drawItem.Invoke(list,new object[]{new DrawItemEventArgs(g,list.Font,row,i,i==list.SelectedIndex?DrawItemState.Selected:DrawItemState.None)});}}
                else if(native){var handle=root.Handle;using(var nativeLayer=new Bitmap(root.Width,root.Height)){root.DrawToBitmap(nativeLayer,root.ClientRectangle);g.DrawImageUnscaled(nativeLayer,0,0);}}
                else{if(root.BackColor.A<255||root is PreferencesRow||root is PreferencesGroup)backgroundPaint.Invoke(root,new object[]{new PaintEventArgs(g,root.ClientRectangle)});paint.Invoke(root,new object[]{new PaintEventArgs(g,root.ClientRectangle)});}
                // Separate local bitmaps keep GDI TextRenderer coordinates correct without translating the Graphics.
                if(!native)for(int i=root.Controls.Count-1;i>=0;i--){Control child=root.Controls[i];if(child.Parent==selectedPage.Parent&&child!=selectedPage)continue;if(ownState!=null&&!(bool)ownState.Invoke(child,new object[]{2}))continue;using(var childLayer=Render(child,selectedPage))g.DrawImageUnscaled(childLayer,child.Left,child.Top);}
            }return layer;
        }
    }
    internal class PreferencesPaintButton:Button
    {
        protected bool Hover;
        protected PreferencesPaintButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(BackColor);}
        protected override void OnMouseEnter(EventArgs e){Hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){Hover=false;Invalidate();base.OnMouseLeave(e);}
    }
    internal sealed class PreferencesButton:PreferencesPaintButton
    {
        public bool Primary;
        public PreferencesButton(){Size=new Size(112,32);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=PreferencesLook.Background;}
        internal void PaintAt(Graphics g,Rectangle r){PreferencesLook.Fill(g,r,BackColor);PreferencesLook.Round(g,r,!Enabled?PreferencesLook.Input:Primary?(Hover?Color.FromArgb(0,88,212):CaptureLook.Blue):(Hover?Color.FromArgb(235,236,240):Color.White),7,Primary?(Color?)null:PreferencesLook.Line);PreferencesLook.Text(g,Text,Font,r,!Enabled?Color.Gray:Primary?Color.White:PreferencesLook.Ink);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(r,-3,-3));}
        protected override void OnPaint(PaintEventArgs e){PaintAt(e.Graphics,ClientRectangle);}
    }
    internal sealed class PreferencesTab:PreferencesPaintButton
    {
        public string Glyph;public bool Selected;
        public PreferencesTab(){Size=new Size(96,67);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=PreferencesLook.Header;}
        internal void PaintAt(Graphics g,Rectangle r){PreferencesLook.Fill(g,r,BackColor);float scale=r.Height/67f;if(Selected||Hover)PreferencesLook.Round(g,r,Selected?Color.FromArgb(230,233,238):Color.FromArgb(236,238,242),Math.Max(1,(int)(9*scale)),null);var ink=Selected?CaptureLook.Blue:PreferencesLook.Muted;int icon=(int)(25*scale);CaptureLook.Icon(g,Glyph,new Rectangle(r.X+(r.Width-icon)/2,r.Y+(int)(9*scale),icon,icon),ink);PreferencesLook.Text(g,Text,Font,new Rectangle(r.X,r.Y+(int)(39*scale),r.Width,(int)(22*scale)),ink);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(r,-3,-3));}
        protected override void OnPaint(PaintEventArgs e){PaintAt(e.Graphics,ClientRectangle);}
    }
    internal sealed class PreferencesSwitch:CheckBox
    {
        public PreferencesSwitch(){AutoSize=false;Size=new Size(42,26);BackColor=Color.White;UseVisualStyleBackColor=false;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(BackColor);}
        internal void PaintAt(Graphics g,Rectangle r){PreferencesLook.Fill(g,r,BackColor);float s=r.Height/26f;var track=new Rectangle(r.Right-(int)(40*s),r.Y+(r.Height-(int)(24*s))/2,(int)(40*s),(int)(24*s));PreferencesLook.Round(g,track,Checked&&Enabled?CaptureLook.Blue:Color.FromArgb(204,205,211),Math.Max(1,(int)(12*s)),null);g.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Enabled?Color.White:Color.FromArgb(240,240,243)))g.FillEllipse(b,Checked?track.Right-22*s:track.Left+3*s,track.Top+3*s,18*s,18*s);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,r);}
        protected override void OnPaint(PaintEventArgs e){PaintAt(e.Graphics,ClientRectangle);}
    }
    internal sealed class PreferencesField:Panel
    {
        public readonly Control Editor;
        public PreferencesField(Control editor,int width){Editor=editor;Size=new Size(width,34);Padding=new Padding(10,7,10,4);BackColor=Color.White;editor.Dock=DockStyle.Fill;editor.BackColor=Color.White;var text=editor as TextBox;if(text!=null){text.BorderStyle=BorderStyle.None;text.ForeColor=editor is HotkeyInput?CaptureLook.Blue:PreferencesLook.Ink;}Controls.Add(editor);editor.Enter+=delegate{Invalidate();};editor.Leave+=delegate{Invalidate();};}
        protected override void OnPaint(PaintEventArgs e){PreferencesLook.Round(e.Graphics,ClientRectangle,Color.White,7,ContainsFocus?CaptureLook.Blue:PreferencesLook.Line);base.OnPaint(e);}
    }
    internal sealed class PreferencesRow:Panel
    {
        internal bool BottomDivider;
        readonly Label caption;readonly Control editor;readonly int desiredWidth;
        public PreferencesRow(string label,Control control,int width=270){Height=58;DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;editor=control;desiredWidth=width;caption=new Label{Text=label,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,ForeColor=PreferencesLook.Ink,BackColor=Color.Transparent};Controls.Add(caption);Controls.Add(control);}
        protected override void OnPaintBackground(PaintEventArgs e){var group=Parent as PreferencesGroup;if(group==null){base.OnPaintBackground(e);return;}var state=e.Graphics.Save();e.Graphics.TranslateTransform(-Left,-Top);group.PaintSurface(e.Graphics);e.Graphics.Restore(state);}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(editor==null)return;int pad=(int)Math.Round(18*DeviceDpi/96f),desired=(int)Math.Round(desiredWidth*DeviceDpi/96f),width=Math.Min(desired,Math.Max(100,Width/2));caption.Bounds=new Rectangle(pad,8,Math.Max(1,Width-width-pad*2-18),Height-16);editor.Bounds=new Rectangle(Width-width-pad,Math.Max(4,(Height-editor.Height)/2),width,editor.Height);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(BottomDivider)using(var p=new Pen(PreferencesLook.Line))e.Graphics.DrawLine(p,16,Height-1,Width-16,Height-1);}
    }
    internal sealed class PreferencesGroup:Panel
    {
        readonly List<Control> rows=new List<Control>();
        public PreferencesGroup(){BackColor=PreferencesLook.Background;DoubleBuffered=true;}
        public void AddRow(Control row){if(rows.Count>0){var previous=rows[rows.Count-1] as PreferencesRow;if(previous!=null)previous.BottomDivider=true;}rows.Add(row);Controls.Add(row);PerformLayout();}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);int y=4;foreach(var row in rows){row.Bounds=new Rectangle(4,y,Math.Max(1,Width-8),row.Height);y+=row.Height;}Height=y+4;}
        internal void PaintSurface(Graphics g){PreferencesLook.Fill(g,ClientRectangle,BackColor);PreferencesLook.FloatingCard(g,ClientRectangle,10);}
        protected override void OnPaintBackground(PaintEventArgs e){PreferencesLook.Fill(e.Graphics,ClientRectangle,BackColor);}
        protected override void OnPaint(PaintEventArgs e){PaintSurface(e.Graphics);base.OnPaint(e);}
    }
}
