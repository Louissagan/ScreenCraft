using System;
using System.Drawing;
namespace Stackshot
{
    public partial class Canvas
    {
        public bool ActiveHasNote{get{return Painter.HasNote(ActiveKind);}}
        public string ActiveText{get{return box!=null?box.Text:selected==null?"":selected.Text??"";}}
        void PaintEditingAnchor(Graphics g)
        {
            if(editing==null||!Painter.HasNote(editing.Kind))return;var live=editing.Clone();live.Text=box==null?editing.Text:box.Text;var bounds=Painter.NoteBounds(live);if(editing.Kind==Tool.Callout){Painter.DrawPointLeader(g,editing,bounds);return;}var anchor=editing.Clone();anchor.Text=null;Painter.DrawMarks(g,new[]{anchor},null,null,Img,Crop);var start=editing.Kind==Tool.Counter?(editing.LeaderCounter?editing.B:editing.A):new PointF(editing.Box.Right,editing.Box.Top+editing.Box.Height/2);var end=new PointF(Math.Max(bounds.Left,Math.Min(bounds.Right,start.X)),Math.Max(bounds.Top,Math.Min(bounds.Bottom,start.Y)));
            if(editing.Kind==Tool.Counter){float dx=end.X-start.X,dy=end.Y-start.Y,d=(float)Math.Sqrt(dx*dx+dy*dy);if(d>0){float r=Math.Min(Painter.MarkerDiameter(editing)/2,d);start=new PointF(start.X+dx*r/d,start.Y+dy*r/d);}}
            using(var pen=new Pen(editing.Color,Painter.LeaderWidth(editing)))g.DrawLine(pen,start,end);
        }
        public float ActiveTextSize{get{return selected!=null?(ActiveHasNote?Painter.NoteFont(selected):selected.FontPx):ActiveHasNote&&DefaultStyle.NoteFontPx>0?DefaultStyle.NoteFontPx:DefaultStyle.FontPx;}}
        public Color ActiveTextColor{get{return System.Drawing.Color.White;}}
        // Compatibility API: there is one annotation color, used behind fixed white label text.
        public void SetTextColor(Color ink){SetColor(ink);}
        public void SetTextSize(float value){if(selected!=null)EditSelected(delegate(Shape s){if(Painter.HasNote(s.Kind))s.NoteFontPx=value;else s.FontPx=value;});else ChangeDefaultStyle(delegate(AnnotationStyle d){if(ActiveHasNote)d.NoteFontPx=value;else d.FontPx=value;});}
        public void ApplyTextPreset(bool background)
        {
            if(selected!=null)EditSelected(delegate(Shape s){if(s.Kind==Tool.Text){s.Color=Painter.LabelColor(s);s.FillEnabled=true;s.FillColor=System.Drawing.Color.FromArgb(255,s.Color);}s.HasNoteTextColor=true;s.NoteTextColor=System.Drawing.Color.White;});
            else ChangeDefaultStyle(delegate(AnnotationStyle d){d.HasNoteTextColor=true;d.NoteTextColor=System.Drawing.Color.White;});
        }
    }
}
