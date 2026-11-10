using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    public sealed class AnnotationStyle
    {
        public float Width=AnnotationSizing.DefaultWidth, FontPx=22, Radius,MarkerSize=28,EffectOpacity=68;
        public bool FillEnabled, SimpleArrow;
        public bool NoteAutoWidth=true,TextWrap=true,LeaderCounter;public float NoteWidth=320;
        public bool SmoothPen=true,FreehandArrow,ArrowWhiteOutline,ClosedPen;
        public int RedactionMode;
        public Color FillColor=Color.FromArgb(60,255,210,0);
        public DashStyle Dash;
        public int MosaicBlock=16;
        public bool HasNoteTextColor;public Color NoteTextColor;public float NoteFontPx;
    }

    public partial class Canvas
    {
        readonly AnnotationStyle standardStyle=new AnnotationStyle();
        readonly AnnotationStyle calloutStyle=new AnnotationStyle{HasNoteTextColor=true,NoteTextColor=System.Drawing.Color.White};
        public AnnotationStyle DefaultStyle{get{return Tool==Tool.Text||Painter.HasNote(Tool)?calloutStyle:standardStyle;}}
        bool released;
        internal Func<System.Windows.Forms.Keys> ModifierSource=delegate{return System.Windows.Forms.Control.ModifierKeys;};
        System.Windows.Forms.Keys InputModifiers{get{return ModifierSource();}}
        internal Point ImageToControl(PointF point){Fit();return ToScreen(point);}
        internal Point[] SelectionHandles(){Fit();return selected==null?new Point[0]:ScreenHandles(selected);}
        public Shape SelectedShape { get { return selected; } }
        internal void FocusInput(){Focus();}
        public int AnnotationCount { get { return shapes.Count; } }
        public Shape[] AnnotationSnapshot {get {return shapes.ConvertAll(delegate(Shape s){return s.Clone();}).ToArray();}}

        void ApplyDefaultStyle(Shape s)
        {
            s.Width=DefaultStyle.Width; s.FontPx=DefaultStyle.FontPx; s.CornerRadius=DefaultStyle.Radius;
            s.Dash=DefaultStyle.Dash; s.FillEnabled=DefaultStyle.FillEnabled; s.FillColor=DefaultStyle.FillColor;
            if(UsesOutlineFill(s.Kind))s.FillColor=System.Drawing.Color.FromArgb(s.FillColor.A,s.Color);
            s.MarkerSize=DefaultStyle.MarkerSize;s.EffectOpacity=DefaultStyle.EffectOpacity;
            if(s.Kind==Tool.Text){s.FillEnabled=true;s.FillColor=System.Drawing.Color.FromArgb(255,s.Color);s.CornerRadius=6;}
            s.SimpleArrow=DefaultStyle.SimpleArrow;s.NoteWidth=DefaultStyle.NoteWidth;s.NoteAutoWidth=DefaultStyle.NoteAutoWidth;s.TextWrap=DefaultStyle.TextWrap;s.LeaderCounter=DefaultStyle.LeaderCounter;
            s.ClosedPen=DefaultStyle.ClosedPen;s.SmoothPen=DefaultStyle.SmoothPen;s.FreehandArrow=DefaultStyle.FreehandArrow;s.ArrowWhiteOutline=DefaultStyle.ArrowWhiteOutline;
            s.RedactionMode=DefaultStyle.RedactionMode;
            s.HasNoteTextColor=DefaultStyle.HasNoteTextColor;s.NoteTextColor=DefaultStyle.NoteTextColor;s.NoteFontPx=DefaultStyle.NoteFontPx;
            if(s.Kind==Tool.Pixelate)s.MosaicBlock=DefaultStyle.MosaicBlock;
        }

        internal static bool UsesOutlineFill(Tool kind){return kind==Tool.Rect||kind==Tool.Ellipse||kind==Tool.Cloud||kind==Tool.Callout||kind==Tool.Pen;}
        public void SetShapeFill(bool enabled)
        {
            if(selected!=null)EditSelected(delegate(Shape s){s.FillEnabled=enabled;if(UsesOutlineFill(s.Kind))s.FillColor=System.Drawing.Color.FromArgb(s.FillColor.A,s.Color);
            if(s.Kind==Tool.Text){s.FillEnabled=true;s.FillColor=System.Drawing.Color.FromArgb(255,s.Color);s.CornerRadius=6;}});
            else ChangeDefaultStyle(delegate(AnnotationStyle d){d.FillEnabled=enabled;if(UsesOutlineFill(Tool))d.FillColor=System.Drawing.Color.FromArgb(d.FillColor.A,Color);});
        }

        public void EditSelected(Action<Shape> edit)
        {
            if(selected==null)return;
            foreach(var item in SelectedShapes){var candidate=item.Clone();edit(candidate);AnnotationDocument.ValidateShape(candidate);}
            Commit();foreach(var item in SelectedShapes){edit(item);item.DropCache();}
            PlaceBox();Fire(Changed); Fire(StateChanged); Invalidate();
        }

        public void AddAnnotation(Shape shape)
        {
            AnnotationDocument.ValidateShape(shape); CommitText(); Commit();
            var copy=shape.Clone(); shapes.Add(copy); SelectShape(copy); Fire(Changed); Invalidate();
        }

        public void EditNote()
        {
            if(selected==null)return;
            if(Painter.HasNote(selected.Kind))PlaceNoteInside(selected);
            StartText(selected.A,selected);
        }

        void PlaceNoteInside(Shape s)
        {
            s.NoteWidth=Math.Min(s.NoteWidth,Math.Max(60,Crop.Width-16));
            var bounds=Painter.NoteBounds(s);
            float dx=bounds.Right>Crop.Right?Crop.Right-bounds.Right-8:0;
            float dy=bounds.Bottom>Crop.Bottom?Crop.Bottom-bounds.Bottom-8:0;
            dx=Math.Max(dx,Crop.Left+8-bounds.Left); dy=Math.Max(dy,Crop.Top+8-bounds.Top);
            s.NoteOffset=new PointF(s.NoteOffset.X+dx,s.NoteOffset.Y+dy);
        }

        public void ResizeSelected(RectangleF rect)
        {
            if(selected==null)return;
            if(SelectionCount>1){var from=GroupBounds();EditSelected(s=>TransformShape(s,s.Clone(),from,rect));return;}
            EditSelected(delegate(Shape s) {
                var from=s.Box;
                if(s.Kind==Tool.Text){s.A=rect.Location;s.TextWrap=true;s.NoteAutoWidth=false;s.NoteWidth=Math.Max(60,Math.Min(800,rect.Width));s.NoteHeight=Math.Max(s.FontPx+16,Math.Min(10000,rect.Height));return;}
                if(s.Kind==Tool.Counter||s.Kind==Tool.Callout) {if(s.LeaderCounter)s.B=new PointF(rect.X+rect.Width/2,rect.Y+rect.Height/2);else{s.A=new PointF(rect.X+rect.Width/2,rect.Y+rect.Height/2);s.B=s.A;}s.MarkerSize=Math.Max(8,Math.Min(200,rect.Width));return;}
                if(s.Points!=null){var points=new List<PointF>();foreach(var p in s.Points)points.Add(new PointF(rect.X+(p.X-from.X)*rect.Width/Math.Max(.001f,from.Width),rect.Y+(p.Y-from.Y)*rect.Height/Math.Max(.001f,from.Height)));s.Points=points;}
                var oldA=s.A;var oldB=s.B;s.A=rect.Location;s.B=new PointF(rect.Right,rect.Bottom);
                if(s.Curved)s.C=Painter.Similar(s.C,oldA,oldB,s.A,s.B);
            });
        }
        public void ReorderSelected(int direction){ReorderSelected(direction,false);}
        public void ReorderSelected(int direction,bool toEdge)
        {
            if(selected==null||direction==0||Busy||Typing)return;
            var items=new HashSet<Shape>(SelectedShapes);var next=new List<Shape>(shapes);
            if(toEdge){next.Clear();next.AddRange(shapes.FindAll(s=>items.Contains(s)==(direction<0)));next.AddRange(shapes.FindAll(s=>items.Contains(s)!=(direction<0)));}
            else if(direction>0){for(int i=next.Count-2;i>=0;i--)if(items.Contains(next[i])&&!items.Contains(next[i+1])){var item=next[i];next[i]=next[i+1];next[i+1]=item;}}
            else{for(int i=1;i<next.Count;i++)if(items.Contains(next[i])&&!items.Contains(next[i-1])){var item=next[i];next[i]=next[i-1];next[i-1]=item;}}
            bool changed=false;for(int i=0;i<shapes.Count;i++)if(shapes[i]!=next[i]){changed=true;break;}if(!changed)return;
            Commit();shapes=next;NotifyEdit();
        }

        public AnnotationDocument ExportDocument()
        {
            if(!CompletePolygon())throw new InvalidOperationException("请至少添加三个不共线的锚点，再完成多边形。");CommitText();if(InPlaceViewport)return ExportCaptureDocument();return new AnnotationDocument(new Bitmap(Img),AnnotationSnapshot,Crop,BgOn,Bg){OutputCornerRadius=OutputCornerRadius};
        }

        public void LoadDocument(AnnotationDocument doc)
        {
            if(doc.Source.Width!=Img.Width || doc.Source.Height!=Img.Height)throw new ArgumentException("工程底图尺寸不一致。");
            foreach(var s in shapes)s.DropCache(); shapes=new List<Shape>(doc.Shapes);
            for(int i=0;i<shapes.Count;i++)shapes[i]=shapes[i].Clone();
            Crop=doc.Crop; OutputCornerRadius=doc.OutputCornerRadius; BgOn=doc.BackgroundOn; Bg=doc.Background.Clone();
            undo.Clear();redo.Clear();selected=null;selection.Clear();Fire(StateChanged);Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {if(captureScene!=null){captureScene.Dispose();captureScene=null;}if(box!=null){var old=box;box=null;DropBox(old);}Release();}
            base.Dispose(disposing);
        }
    }
}
