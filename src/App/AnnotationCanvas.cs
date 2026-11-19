using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenCraft
{
    internal enum LineStyle { NoArrow, ThinArrow, TaperedArrow, WhiteOutline }
    internal enum ShapeFill { None, Translucent, Hatch }

    public sealed class AnnotationStyle
    {
        public float Width=AnnotationSizing.DefaultWidth, FontPx=22, Radius,MarkerSize=28,EffectOpacity=68;
        public bool FillEnabled, HatchedFill, SimpleArrow;
        public bool TransparentText=true;
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
        readonly AnnotationStyle penPathStyle=new AnnotationStyle();
        readonly AnnotationStyle spotlightStyle=new AnnotationStyle();
        public AnnotationStyle DefaultStyle{get{return Tool==Tool.PenPath?penPathStyle:Tool==Tool.Spotlight?spotlightStyle:Tool==Tool.Text||Painter.HasNote(Tool)?calloutStyle:standardStyle;}}
        bool released;
        internal Func<System.Windows.Forms.Keys> ModifierSource=delegate{return System.Windows.Forms.Control.ModifierKeys;};
        System.Windows.Forms.Keys InputModifiers{get{return ModifierSource();}}
        internal Point ImageToControl(PointF point){Fit();return ToScreen(point);}
        internal Point[] SelectionHandles(){Fit();return selected==null?new Point[0]:ScreenHandles(selected);}
        public Shape SelectedShape { get { return selected; } }
        internal void FocusInput(){Focus();}
        public int AnnotationCount { get { return shapes.Count; } }
        public Shape[] AnnotationSnapshot {get {return shapes.ConvertAll(delegate(Shape s){return s.Clone();}).ToArray();}}

        internal LineStyle ActiveLineStyle
        {
            get{
                if(ActiveKind==Tool.Line)return LineStyle.NoArrow;
                bool simple=selected==null?DefaultStyle.SimpleArrow:selected.SimpleArrow;
                bool outline=selected==null?DefaultStyle.ArrowWhiteOutline:selected.ArrowWhiteOutline;
                return simple?LineStyle.ThinArrow:outline?LineStyle.WhiteOutline:LineStyle.TaperedArrow;
            }
        }

        internal void SetLineStyle(LineStyle style)
        {
            if(!Shape.IsLinear(ActiveKind))return;
            Tool kind=style==LineStyle.NoArrow?Tool.Line:Tool.Arrow;
            bool simple=style==LineStyle.ThinArrow,outline=style==LineStyle.WhiteOutline;
            if(selected!=null){
                EditSelected(s=>{
                    if(!Shape.IsLinear(s.Kind))return;
                    s.Kind=kind;s.SimpleArrow=simple;s.ArrowWhiteOutline=outline;
                    if(outline)s.Width=Math.Max(6,s.Width);
                });
                return;
            }
            // Keep the last arrow appearance available when switching to a plain line.
            if(kind==Tool.Arrow){
                DefaultStyle.SimpleArrow=simple;DefaultStyle.ArrowWhiteOutline=outline;
                if(outline)DefaultStyle.Width=Math.Max(6,DefaultStyle.Width);
            }
            SetTool(kind);
        }

        void ApplyDefaultStyle(Shape s)
        {
            s.Width=DefaultStyle.Width; s.FontPx=DefaultStyle.FontPx; s.CornerRadius=DefaultStyle.Radius;
            s.Dash=DefaultStyle.Dash; s.FillEnabled=DefaultStyle.FillEnabled; s.HatchedFill=DefaultStyle.HatchedFill;var pattern=FillDefaults;pattern.StrokePercent=pattern.EffectiveStrokePercent;pattern.Apply(s); s.FillColor=DefaultStyle.FillColor;
            if(UsesOutlineFill(s.Kind))s.FillColor=System.Drawing.Color.FromArgb(s.FillColor.A,s.Color);
            s.MarkerSize=DefaultStyle.MarkerSize;s.EffectOpacity=DefaultStyle.EffectOpacity;
            if(s.Kind==Tool.Text){s.TransparentText=DefaultStyle.TransparentText;s.FillEnabled=!s.TransparentText;s.FillColor=System.Drawing.Color.FromArgb(255,s.Color);s.CornerRadius=6;}
            s.SimpleArrow=DefaultStyle.SimpleArrow;s.NoteWidth=DefaultStyle.NoteWidth;s.NoteAutoWidth=DefaultStyle.NoteAutoWidth;s.TextWrap=DefaultStyle.TextWrap;s.LeaderCounter=DefaultStyle.LeaderCounter;
            s.ClosedPen=DefaultStyle.ClosedPen;s.SmoothPen=DefaultStyle.SmoothPen;s.FreehandArrow=DefaultStyle.FreehandArrow;s.ArrowWhiteOutline=DefaultStyle.ArrowWhiteOutline;
            s.RedactionMode=DefaultStyle.RedactionMode;
            s.HasNoteTextColor=DefaultStyle.HasNoteTextColor;s.NoteTextColor=DefaultStyle.NoteTextColor;s.NoteFontPx=DefaultStyle.NoteFontPx;
            if(s.Kind==Tool.Pixelate)s.MosaicBlock=DefaultStyle.MosaicBlock;
        }

        internal static bool UsesOutlineFill(Tool kind){return kind==Tool.Rect||kind==Tool.Ellipse||kind==Tool.Cloud||kind==Tool.Callout||kind==Tool.Pen||kind==Tool.PenPath;}
        static bool SupportsShapeFill(Tool kind,bool closedPen){return kind==Tool.Rect||kind==Tool.Ellipse||kind==Tool.Cloud||kind==Tool.PenPath||kind==Tool.Pen&&closedPen;}
        internal bool CanFillShape{get{return SupportsShapeFill(ActiveKind,selected==null?DefaultStyle.ClosedPen:selected.ClosedPen);}}
        internal ShapeFill ActiveShapeFill
        {
            get{
                bool enabled=selected==null?DefaultStyle.FillEnabled:selected.FillEnabled;
                bool hatch=selected==null?DefaultStyle.HatchedFill:selected.HatchedFill;
                return !enabled?ShapeFill.None:hatch?ShapeFill.Hatch:ShapeFill.Translucent;
            }
        }
        public void SetShapeFill(bool enabled){SetShapeFill(enabled,null);}
        internal void SetShapeFill(ShapeFill mode){SetShapeFill(mode!=ShapeFill.None,mode);}
        void SetShapeFill(bool enabled,ShapeFill? mode)
        {
            if(mode.HasValue&&!CanFillShape)return;
            if(selected!=null)EditSelected(s=>{
                if(mode.HasValue&&!SupportsShapeFill(s.Kind,s.ClosedPen))return;
                s.FillEnabled=enabled;
                if(mode.HasValue&&enabled)s.HatchedFill=mode==ShapeFill.Hatch;
                if(UsesOutlineFill(s.Kind))s.FillColor=System.Drawing.Color.FromArgb(s.FillColor.A,s.Color);
                if(s.Kind==Tool.Text){s.FillEnabled=!s.TransparentText;s.FillColor=System.Drawing.Color.FromArgb(255,s.Color);s.CornerRadius=6;}
            });
            else ChangeDefaultStyle(d=>{
                d.FillEnabled=enabled;
                if(mode.HasValue&&enabled)d.HatchedFill=mode==ShapeFill.Hatch;
                if(UsesOutlineFill(Tool))d.FillColor=System.Drawing.Color.FromArgb(d.FillColor.A,Color);
            });
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
                if(s.PathTangents!=null)s.PathTangents=s.PathTangents.ConvertAll(p=>new PointF(p.X*rect.Width/Math.Max(.001f,from.Width),p.Y*rect.Height/Math.Max(.001f,from.Height)));
                if(s.Points!=null){var points=new List<PointF>();foreach(var p in s.Points)points.Add(new PointF(rect.X+(p.X-from.X)*rect.Width/Math.Max(.001f,from.Width),rect.Y+(p.Y-from.Y)*rect.Height/Math.Max(.001f,from.Height)));s.Points=points;}
                var oldA=s.A;var oldB=s.B;s.A=rect.Location;s.B=new PointF(rect.Right,rect.Bottom);
                if(s.Curved)s.C=Painter.Similar(s.C,oldA,oldB,s.A,s.B);
            });
        }
        public void ReorderSelected(int direction){ReorderSelected(direction,false);}
        public void ReorderSelected(int direction,bool toEdge)
        {
            if(selected==null||selected.Kind==Tool.Spotlight||direction==0||Busy||Typing)return;
            var items=new HashSet<Shape>(SelectedShapes);
            var marks=shapes.FindAll(s=>s.Kind!=Tool.Spotlight);
            var next=new List<Shape>(marks);
            if(toEdge){next.Clear();next.AddRange(marks.FindAll(s=>items.Contains(s)==(direction<0)));next.AddRange(marks.FindAll(s=>items.Contains(s)!=(direction<0)));}
            else if(direction>0){for(int i=next.Count-2;i>=0;i--)if(items.Contains(next[i])&&!items.Contains(next[i+1])){var item=next[i];next[i]=next[i+1];next[i+1]=item;}}
            else{for(int i=1;i<next.Count;i++)if(items.Contains(next[i])&&!items.Contains(next[i-1])){var item=next[i];next[i]=next[i-1];next[i-1]=item;}}
            bool changed=false;for(int i=0;i<marks.Count;i++)if(marks[i]!=next[i]){changed=true;break;}if(!changed)return;
            Commit();
            int index=0;
            for(int i=0;i<shapes.Count;i++)if(shapes[i].Kind!=Tool.Spotlight)shapes[i]=next[index++];
            NotifyEdit();
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
            if(disposing) {if(fillMenu!=null){fillMenu.Dispose();fillMenu=null;}if(captureScene!=null){captureScene.Dispose();captureScene=null;}if(box!=null){var old=box;box=null;DropBox(old);}Release();}
            base.Dispose(disposing);
        }
    }
}
