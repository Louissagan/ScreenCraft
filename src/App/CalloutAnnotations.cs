using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    public partial class Shape
    {
        public bool NoteAutoWidth,TextWrap,LeaderCounter;
    }
    public static partial class Painter
    {
        public static bool HasNote(Tool kind){return kind==Tool.Callout||kind==Tool.Cloud||kind==Tool.Counter;}
        internal static float WrappedWidth(string text,float font,float maximum,bool auto,float padding)
        {
            maximum=Math.Max(60,Math.Min(800,maximum));if(!auto)return maximum;float natural=0;foreach(var line in (text??"").Split('\n'))natural=Math.Max(natural,measure.MeasureString(line.Length==0?" ":line,TextFont(font)).Width+padding*2);return Math.Max(40,Math.Min(maximum,natural));
        }
        internal static RectangleF WrappedTextBounds(Shape s){float pad=8,width=WrappedWidth(s.Text,s.FontPx,s.NoteWidth,s.NoteAutoWidth,pad);var size=measure.MeasureString(s.Text??"",TextFont(s.FontPx),new SizeF(Math.Max(1,width-2*pad),10000));return new RectangleF(s.A.X,s.A.Y,width,Math.Max(s.NoteHeight,Math.Max(s.FontPx+2*pad,size.Height+2*pad)));}
        public static RectangleF CounterBadge(Shape s){float r=CounterRadius(s);return new RectangleF(s.B.X-r,s.B.Y-r,r*2,r*2);}
        internal static void DrawPointLeader(Graphics g,Shape s,RectangleF target)
        {
            var end=new PointF(Math.Max(target.Left,Math.Min(target.Right,s.A.X)),Math.Max(target.Top,Math.Min(target.Bottom,s.A.Y)));using(var pen=new Pen(s.Color,LeaderWidth(s)))g.DrawLine(pen,s.A,end);using(var ink=new SolidBrush(s.Color))g.FillEllipse(ink,s.A.X-MarkerDiameter(s)/2,s.A.Y-MarkerDiameter(s)/2,MarkerDiameter(s),MarkerDiameter(s));
        }
        internal static void DrawCounterLeader(Graphics g,Shape s)
        {
            var badge=CounterBadge(s);DrawPointLeader(g,s,badge);using(var path=Theme.Round(badge,Math.Min(8,badge.Width/4)))using(var fill=new SolidBrush(s.Color))g.FillPath(fill,path);using(var ink=new SolidBrush(Contrast(s.Color)))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){float px=s.FontPx;var digits=s.Number.ToString();var size=g.MeasureString(digits,CounterFont(px));if(size.Width>badge.Width*.82f)px*=badge.Width*.82f/size.Width;g.DrawString(digits,CounterFont(px),ink,badge,format);}
        }
    }
    public partial class Canvas
    {
        bool movingCounterBadge;
        Rectangle lastInputBounds;
        public bool ActiveNumberOnly{get{return ActiveKind==Tool.Counter&&!Typing&&(selected==null||string.IsNullOrEmpty(selected.Text));}}
        public float ActiveNumberSize{get{return selected==null?DefaultStyle.FontPx:selected.FontPx;}}
        public void SetNumberSize(float size){if(selected==null)ChangeDefaultStyle(delegate(AnnotationStyle d){d.FontPx=size;});else EditSelected(delegate(Shape s){s.FontPx=size;});}
        public float ActiveNoteWidth{get{return selected==null?DefaultStyle.NoteWidth:selected.NoteWidth;}}
        public void SetNoteWidth(float width){width=Math.Max(60,Math.Min(800,width));if(selected==null)ChangeDefaultStyle(delegate(AnnotationStyle d){d.NoteWidth=width;d.NoteAutoWidth=false;});else EditSelected(delegate(Shape s){s.NoteWidth=width;s.NoteAutoWidth=false;if(s.Kind==Tool.Text)s.TextWrap=true;});}
        public void SetNoteAutoWidth(bool value){if(selected==null)ChangeDefaultStyle(delegate(AnnotationStyle d){d.NoteAutoWidth=value;});else EditSelected(delegate(Shape s){s.NoteAutoWidth=value;if(s.Kind==Tool.Text)s.TextWrap=true;});}
        static bool LeaderHit(PointF p,PointF a,RectangleF body,float tolerance){if(body.Contains(p))return true;var b=new PointF(Math.Max(body.Left,Math.Min(body.Right,a.X)),Math.Max(body.Top,Math.Min(body.Bottom,a.Y)));float dx=b.X-a.X,dy=b.Y-a.Y,den=dx*dx+dy*dy;float t=den<.001f?0:Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Y-a.Y)*dy)/den));float ex=p.X-a.X-t*dx,ey=p.Y-a.Y-t*dy;return ex*ex+ey*ey<=tolerance*tolerance;}
        public void SetCounterLeader(bool value){if(selected!=null&&selected.Kind==Tool.Counter)EditSelected(delegate(Shape s){s.LeaderCounter=value;if(value&&s.B==s.A)s.B=new PointF(s.A.X+64,s.A.Y+48);});else ChangeDefaultStyle(delegate(AnnotationStyle d){d.LeaderCounter=value;});}
        public void SetAnnotationMode(Tool kind)
        {
            if(!Painter.HasNote(kind))return;
            if(selected==null||!ActiveHasNote){SetTool(kind);return;}
            EditSelected(delegate(Shape s){var note=Painter.NoteBounds(s);var at=s.Kind==Tool.Cloud?new PointF(s.Box.Left+s.Box.Width/2,s.Box.Top+s.Box.Height/2):s.A;s.Kind=kind;s.LeaderCounter=false;s.A=at;s.B=kind==Tool.Cloud?new PointF(at.X+100,at.Y+64):at;if(kind==Tool.Counter&&s.Number==0)s.Number=NextNumber();s.NoteOffset=new PointF(note.X-(kind==Tool.Cloud?s.Box.Right:s.A.X),note.Y-(kind==Tool.Cloud?s.Box.Top:s.A.Y));});movingNote=true;Fire(StateChanged);
        }
        void AddCallout(PointF at){var s=NewShape(Tool.Callout);s.A=s.B=at;s.Text="";Commit();shapes.Add(s);SelectShape(s);movingNote=true;PlaceNoteInside(s);StartText(at,s);Fire(Changed);Invalidate();}
        RectangleF ActiveNoteBounds(Shape s){return s.Kind==Tool.Text?Painter.TextBounds(s):Painter.NoteBounds(s);}
        void ResizeNote(Shape s,Shape original,int corner,PointF to)
        {
            var before=ActiveNoteBounds(original);var after=ResizeBounds(before,corner,to,false);
            s.NoteWidth=Math.Max(60,Math.Min(800,after.Width));s.NoteHeight=Math.Max(NoteMinimumHeight(original),Math.Min(10000,after.Height));s.NoteAutoWidth=false;
            if(s.Kind==Tool.Text){s.A=after.Location;s.TextWrap=true;}else s.NoteOffset=new PointF(original.NoteOffset.X+after.X-before.X,original.NoteOffset.Y+after.Y-before.Y);

        }
        static float NoteMinimumHeight(Shape s){return (s.Kind==Tool.Text?s.FontPx:Painter.NoteFont(s))+16;}
        public float ActiveMarkerSize{get{return selected==null?DefaultStyle.MarkerSize:Painter.MarkerDiameter(selected);}}
        public void SetMarkerSize(float size){size=Math.Max(8,Math.Min(200,size));if(selected==null)ChangeDefaultStyle(d=>d.MarkerSize=size);else EditSelected(s=>s.MarkerSize=size);}
        void MoveAnnotation(float dx,float dy)
        {
            if(movingNote)selected.NoteOffset=new PointF(orig.NoteOffset.X+dx,orig.NoteOffset.Y+dy);
            else if(movingCounterBadge){selected.B=new PointF(orig.B.X+dx,orig.B.Y+dy);selected.NoteOffset=new PointF(orig.NoteOffset.X-dx,orig.NoteOffset.Y-dy);}
            else if(selected.Kind==Tool.Counter&&selected.LeaderCounter){selected.A=new PointF(orig.A.X+dx,orig.A.Y+dy);selected.B=orig.B;}
            else{selected.Offset(orig,dx,dy);if(Painter.HasNote(selected.Kind)&&!string.IsNullOrEmpty(selected.Text))selected.NoteOffset=new PointF(orig.NoteOffset.X-dx,orig.NoteOffset.Y-dy);}
        }
    }
}
