using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace ScreenCraft
{
    // The control is a view/controller over image-coordinate marks. A gesture has
    // exactly one pre-state and commits at most one history entry; mouse-up is input.
    public partial class Canvas : Control
    {
        sealed class Snapshot { internal List<Shape> Shapes;internal RectangleF Crop;internal Settings Background;internal bool BackgroundOn;internal float OutputCornerRadius;internal object Extra; }
        enum Drag { None,Draw,Move,Resize }
        public Func<object> SaveExtra;public Action<object> LoadExtra;
        public static readonly float[] Weights={.6f,1,1.6f};
        public static bool PaintUnderCard;
        public Bitmap Img{get;private set;}
        public float Ui=1;
        public Tool Tool=Tool.Rect;
        public Color Color=AnnotationColors.Default;
        public int Weight=1;
        public RectangleF Crop;
        public Settings Bg;
        public bool BgOn,Video;
        public bool Live{get;set;}
        public event EventHandler Changed,StateChanged;
        public event Action DrawStart;
        List<Shape> shapes=new List<Shape>();
        readonly List<Snapshot> undo=new List<Snapshot>(),redo=new List<Snapshot>();
        Shape cur,selected,hover,orig,editing,nudged;
        Drag drag;
        int handle,version,nudgedAt;
        bool movingNote,panning,spacePan;
        PointF dragFrom,grab,off,pan,panStart;
        Point panFrom;
        Point lastPointer;
        RectangleF backdropViewport;
        MouseButtons panButton;
        float k=1,zoom=1;
        TextBox box;Font inputFont;
        PointF boxAt;
        Snapshot gestureBefore;
        Snapshot[] gestureRedo;
        bool gestureCommitted;
        Backdrop.ShotInfo backdropInfo;
        Bitmap playFrame;

        public Canvas(Bitmap image)
        {
            if(image==null)throw new ArgumentNullException("image");
            Img=image;Crop=new RectangleF(PointF.Empty,image.Size);
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
            BackColor=PreferencesLook.Background;TabStop=true;
        }
        public bool Typing{get{return box!=null;}}
        public bool HasSelection{get{return selected!=null;}}
        public bool Busy{get{return drag!=Drag.None||panning||sampledColor!=null;}}
        public bool CanUndo{get{return undo.Count!=0||Typing;}}
        public bool CanRedo{get{return redo.Count!=0&&!Typing;}}
        public bool IsCropped{get{return Crop!=new RectangleF(PointF.Empty,Img.Size);}}
        public bool HasMarks{get{return shapes.Count!=0;}}
        public Color ActiveColor{get{return selected==null?Color:selected.Kind==Tool.Text?Painter.LabelColor(selected):selected.Color;}}
        public Tool ActiveKind{get{return selected==null?Tool:selected.Kind;}}
        public int ActiveWeight{get{return selected==null?Weight:selected.Weight;}}
        public Rectangle CropRect{get{return Rectangle.Round(Crop);}}
        Size OutputSizeRaw{get{return new Size(Math.Max(1,(int)Math.Round(Crop.Width)),Math.Max(1,(int)Math.Round(Crop.Height)));}}
        public Size OutputSize{get{if(!BgOn||Bg==null)return OutputSizeRaw;Size size;Rectangle inner,card;var info=Info();Backdrop.Measure(OutputSizeRaw,Bg,false,info==null?Padding.Empty:info.Inset,out size,out card,out inner);return size;}}
        int Pu(float value){return Math.Max(1,(int)Math.Round(value*Ui));}
        float StrokeFor(int weight){return 3*Weights[Math.Max(0,Math.Min(2,weight))];}
        float FontFor(int weight){return 22*(.7f+.3f*Math.Max(0,Math.Min(2,weight)));}
        void Fire(EventHandler handler){if(handler!=null)handler(this,EventArgs.Empty);}
        void NotifyEdit(){Fire(Changed);Fire(StateChanged);Invalidate();}
        Backdrop.ShotInfo Info(){if(Video||Bg==null)return null;var next=Backdrop.Study(Img,CropRect,Bg,backdropInfo);if(backdropInfo!=null&&next!=backdropInfo)backdropInfo.Dispose();return backdropInfo=next;}
        public Bitmap BlurThumb(){var info=Info();return info==null?null:Backdrop.BlurOf(info,Img,CropRect);}
        public void BackdropChanged(){Rescaled();Invalidate();}

        void Fit()
        {
            if(InPlaceViewport){k=1;off=PointF.Empty;return;}
            if(ClientSize.Width<2||ClientSize.Height<2){k=1;off=PointF.Empty;return;}
            if(BgOn&&Bg!=null){
                Size frame;Rectangle card,inner;var info=Info();Backdrop.Measure(OutputSizeRaw,Bg,false,info==null?Padding.Empty:info.Inset,out frame,out card,out inner);
                float fitFrame=Math.Min(Math.Max(1,Width-48)/(float)frame.Width,Math.Max(1,Height-48)/(float)frame.Height);k=Math.Max(.001f,fitFrame*zoom);
                backdropViewport=new RectangleF((Width-frame.Width*k)/2+pan.X,(Height-frame.Height*k)/2+pan.Y,frame.Width*k,frame.Height*k);
                off=new PointF(backdropViewport.X+(inner.X-Crop.X)*k,backdropViewport.Y+(inner.Y-Crop.Y)*k);return;
            }
            float fit=Math.Min(Math.Max(1,ClientSize.Width-48)/(float)Img.Width,Math.Max(1,ClientSize.Height-48)/(float)Img.Height);
            k=Math.Max(.001f,fit*zoom);off=new PointF((ClientSize.Width-Img.Width*k)*.5f+pan.X,(ClientSize.Height-Img.Height*k)*.5f+pan.Y);
        }
        Point ToScreen(PointF point){return new Point((int)Math.Round(off.X+point.X*k),(int)Math.Round(off.Y+point.Y*k));}
        PointF ToImg(Point point,bool clamp){float x=(point.X-off.X)/k,y=(point.Y-off.Y)/k;if(clamp){x=Math.Max(0,Math.Min(Img.Width,x));y=Math.Max(0,Math.Min(Img.Height,y));}return new PointF(x,y);}
        Rectangle ScreenRect(RectangleF rectangle){var a=ToScreen(rectangle.Location);var b=ToScreen(new PointF(rectangle.Right,rectangle.Bottom));return Rectangle.FromLTRB(a.X,a.Y,b.X,b.Y);}
        public void Rescaled(){Fit();PlaceBox();Invalidate();}
        public int ZoomPercent{get{Fit();return (int)Math.Round(k*100);}}
        public bool Zoomed{get{return Math.Abs(zoom-1)>.001||pan!=PointF.Empty;}}
        public void ZoomAt(Point point,float factor)
        {
            if(InPlaceViewport||factor<=0||float.IsNaN(factor)||float.IsInfinity(factor))return;
            Fit();PointF anchor=ToImg(point,false);zoom=Math.Max(.05f,Math.Min(32,zoom*factor));Fit();pan.X+=point.X-(off.X+anchor.X*k);pan.Y+=point.Y-(off.Y+anchor.Y*k);Rescaled();
        }
        public void ZoomBy(float factor){ZoomAt(new Point(ClientSize.Width/2,ClientSize.Height/2),factor);}
        public void ZoomFit(){zoom=1;pan=PointF.Empty;Rescaled();}
        public void ZoomActual(){Fit();zoom/=k;pan=PointF.Empty;Rescaled();}
        protected override void OnMouseWheel(MouseEventArgs e){if(!InPlaceViewport&&!Busy)ZoomAt(e.Location,e.Delta>0?1.2f:1/1.2f);base.OnMouseWheel(e);}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Img!=null)Rescaled();}

        protected override void OnPaint(PaintEventArgs e)
        {
            if(released||Img==null)return;if(PaintFrozenSurface(e))return;Fit();var g=e.Graphics;
            if(BgOn&&Bg!=null){using(var output=RenderContent(true))g.DrawImage(output,backdropViewport);PaintAdornments(g);return;}
            var state=g.Save();try{
                g.TranslateTransform(off.X,off.Y);g.ScaleTransform(k,k);g.InterpolationMode=k==1?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(playFrame??Img,new RectangleF(PointF.Empty,Img.Size));
                using(var outside=new Region(new Rectangle(Point.Empty,Img.Size))){outside.Exclude(Crop);using(var dim=new SolidBrush(System.Drawing.Color.FromArgb(135,0,0,0)))g.FillRegion(dim,outside);}
                g.SetClip(Crop,CombineMode.Intersect);Painter.DrawMarks(g,shapes,cur!=null&&cur.Kind!=Tool.Crop?cur:null,editing,Img,Crop);PaintEditingAnchor(g);
            }finally{g.Restore(state);}
            PaintAdornments(g);
        }
        void PaintAdornments(Graphics g)
        {
            if(cur!=null&&cur.Kind==Tool.Crop){using(var pen=new Pen(CaptureLook.Blue,1))g.DrawRectangle(pen,ScreenRect(cur.Box));}
            if(hover!=null&&hover!=selected&&!Busy)Outline(g,hover,false);
            if(selected!=null&&!Typing){if(SelectionCount>1)OutlineGroup(g);else Outline(g,selected,true);}
            if(Typing){PlaceBox();BoxLabel(g);}
        }
        RectangleF HandleBounds(Shape shape)
        {
            if(SelectionCount>1)return GroupBounds();
            if(Painter.HasNote(shape.Kind)&&movingNote&&!string.IsNullOrEmpty(shape.Text))return Painter.NoteBounds(shape);
            if(shape.Kind==Tool.Text)return Painter.TextBounds(shape);
            if(shape.Kind==Tool.Counter)return shape.LeaderCounter?Painter.CounterBadge(shape):new RectangleF(shape.A.X-Painter.CounterRadius(shape),shape.A.Y-Painter.CounterRadius(shape),Painter.CounterRadius(shape)*2,Painter.CounterRadius(shape)*2);
            return shape.Kind==Tool.Callout?Painter.Bounds(shape):shape.Box;
        }
        PointF[] Handles(Shape shape){return SelectionCount<=1&&Shape.IsLinear(shape.Kind)?new[]{shape.A,shape.B,shape.Mid}:BoxHandles(HandleBounds(shape));}
        Point[] ScreenHandles(Shape shape){return Handles(shape).Select(ToScreen).ToArray();}
        int HandleAt(Point point){if(selected==null||Typing)return -1;var handles=ScreenHandles(selected);for(int i=0;i<handles.Length;i++)if(Math.Abs(handles[i].X-point.X)<=Pu(6)&&Math.Abs(handles[i].Y-point.Y)<=Pu(6))return i;return -1;}
        void Outline(Graphics g,Shape shape,bool strong)
        {
            Color accent=strong?CaptureLook.Blue:System.Drawing.Color.FromArgb(120,CaptureLook.Blue);
            using(var pen=new Pen(accent,1)){pen.DashStyle=DashStyle.Dash;var bounds=ScreenRect(HandleBounds(shape));if(!Shape.IsLinear(shape.Kind)||SelectionCount>1)g.DrawRectangle(pen,bounds);}
            if(strong)foreach(var point in ScreenHandles(shape))SelectionAppearance.PaintHandle(g,new Rectangle(point.X-Pu(3),point.Y-Pu(3),Pu(6),Pu(6)));
        }
        static float SegmentDistance(PointF point,PointF start,PointF end)
        {
            double dx=end.X-start.X,dy=end.Y-start.Y,den=dx*dx+dy*dy,t=den<1e-12?0:Math.Max(0,Math.Min(1,((point.X-start.X)*dx+(point.Y-start.Y)*dy)/den));
            return (float)Math.Sqrt(Math.Pow(point.X-start.X-t*dx,2)+Math.Pow(point.Y-start.Y-t*dy,2));
        }
        bool Hit(Shape shape,PointF point,float tolerance)
        {
            if(Painter.HasNote(shape.Kind)&&!string.IsNullOrEmpty(shape.Text)&&LeaderHit(point,shape.A,Painter.NoteBounds(shape),tolerance))return true;
            if(shape.Kind==Tool.Text)return Painter.TextBounds(shape).Contains(point);
            if(shape.Kind==Tool.Counter||shape.Kind==Tool.Callout)return Painter.Bounds(shape).Contains(point);
            if(Shape.IsLinear(shape.Kind)||shape.Kind==Tool.Pen){var points=shape.Kind==Tool.Pen?(shape.Points??new List<PointF>()).ToArray():shape.Curve(shape.Curved?48:1);for(int i=1;i<points.Length;i++)if(SegmentDistance(point,points[i-1],points[i])<=tolerance+shape.Width)return true;return false;}
            var b=shape.Box;b.Inflate(tolerance,tolerance);if(!b.Contains(point))return false;
            if(shape.FillEnabled||shape.Kind==Tool.Highlight||shape.Kind==Tool.Redact||shape.Kind==Tool.Pixelate||shape.Kind==Tool.Watermark)return true;
            if(shape.Kind==Tool.Ellipse){double rx=shape.Box.Width/2,ry=shape.Box.Height/2;if(rx<.01||ry<.01)return false;double d=Math.Sqrt(Math.Pow((point.X-shape.Box.Left-rx)/rx,2)+Math.Pow((point.Y-shape.Box.Top-ry)/ry,2));return Math.Abs(d-1)<=tolerance/Math.Min(rx,ry);}
            b=shape.Box;b.Inflate(-tolerance,-tolerance);return !b.Contains(point);
        }
        Shape ShapeAt(Point point){var p=ToImg(point,false);for(int i=shapes.Count-1;i>=0;i--)if(Hit(shapes[i],p,Pu(5)/k))return shapes[i];return null;}
        void UpdateHover(Point point){var next=ShapeAt(point);if(next!=hover){hover=next;Invalidate();}Cursor=HandleAt(point)>=0?Cursors.SizeNWSE:next!=null?Cursors.SizeAll:Tool==Tool.Text?Cursors.IBeam:Cursors.Cross;}

        void BeginEditGesture(PointF point)
        {
            dragFrom=point;orig=selected.Clone();gestureBefore=Take();gestureRedo=redo.ToArray();gestureCommitted=false;BeginSelectionGesture();Capture=true;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);lastPointer=e.Location;if(FullCaptureSurface&&CaptureDown!=null&&CaptureDown(e))return;
            if(e.Button==MouseButtons.Left&&TrySampleColor(e.Location))return;if(Busy)return;Fit();
            if(!InPlaceViewport&&(e.Button==MouseButtons.Middle||e.Button==MouseButtons.Left&&spacePan)){panning=true;panFrom=e.Location;panStart=pan;panButton=e.Button;Capture=true;return;}
            if(Typing){CommitText();return;}if(e.Button==MouseButtons.Right){SelectShape(null);return;}if(e.Button!=MouseButtons.Left)return;Focus();
            var point=ToImg(e.Location,true);int index=HandleAt(e.Location);
            if(index>=0){handle=index;var target=Handles(selected)[index];var raw=ToImg(e.Location,false);grab=new PointF(target.X-raw.X,target.Y-raw.Y);drag=Drag.Resize;BeginEditGesture(raw);return;}
            Shape hit=Tool==Tool.Crop?null:ShapeAt(e.Location);
            if(hit!=null){if((InputModifiers&(Keys.Control|Keys.Shift))!=0){ToggleSelection(hit);return;}if(!SelectedShapes.Contains(hit))SelectShape(hit);
                if(e.Clicks>1&&(hit.Kind==Tool.Text||Painter.HasNote(hit.Kind)||hit.Kind==Tool.Watermark)){StartText(hit.A,hit);return;}
                movingNote=Painter.HasNote(hit.Kind)&&!string.IsNullOrEmpty(hit.Text)&&Painter.NoteBounds(hit).Contains(point);
                movingCounterBadge=hit.Kind==Tool.Counter&&hit.LeaderCounter&&!movingNote&&Painter.CounterBadge(hit).Contains(point);
                drag=Drag.Move;BeginEditGesture(ToImg(e.Location,false));return;
            }
            if((InputModifiers&Keys.Control)!=0)return;SelectShape(null);if(!ScreenRect(Crop).Contains(e.Location))return;
            if(Tool==Tool.Text){StartText(point,null);return;}if(Tool==Tool.Callout){AddCallout(point);return;}
            if(Tool==Tool.Counter){Commit();var counter=NewShape(Tool.Counter);counter.A=counter.B=point;counter.Number=NextNumber();if(counter.LeaderCounter)counter.B=new PointF(point.X+64,point.Y+48);shapes.Add(counter);SelectShape(counter);NotifyEdit();return;}
            cur=NewShape(Tool);cur.A=cur.B=point;dragFrom=point;if(Tool==Tool.Pen)cur.Points=new List<PointF>{point};drag=Drag.Draw;Capture=true;Invalidate();
        }
        Shape NewShape(Tool kind){if(DrawStart!=null)DrawStart();var result=new Shape{Kind=kind,Color=Color,Weight=Weight,Width=StrokeFor(Weight),FontPx=FontFor(Weight)};ApplyDefaultStyle(result);return result;}
        static PointF Constrained(PointF start,PointF end,Tool kind)
        {
            float dx=end.X-start.X,dy=end.Y-start.Y;if(Shape.IsLinear(kind)){double angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4),length=Math.Sqrt(dx*dx+dy*dy);return new PointF(start.X+(float)(length*Math.Cos(angle)),start.Y+(float)(length*Math.Sin(angle)));}
            float edge=Math.Max(Math.Abs(dx),Math.Abs(dy));return new PointF(start.X+(dx<0?-edge:edge),start.Y+(dy<0?-edge:edge));
        }
        void UpdateGesture(Point pixel)
        {
            var point=ToImg(pixel,drag==Drag.Draw);bool square=(InputModifiers&Keys.Shift)!=0;
            if(drag==Drag.Draw){cur.B=square?Constrained(cur.A,point,cur.Kind):point;if(cur.Points!=null){if(square){cur.B=Math.Abs(point.X-cur.A.X)>=Math.Abs(point.Y-cur.A.Y)?new PointF(point.X,cur.A.Y):new PointF(cur.A.X,point.Y);cur.Points=new List<PointF>{cur.A,cur.B};}else if(cur.Points.Count==0||cur.Points[cur.Points.Count-1]!=point)cur.Points.Add(point);}Invalidate();return;}
            if(drag!=Drag.Move&&drag!=Drag.Resize)return;
            if(Math.Abs(point.X-dragFrom.X)+Math.Abs(point.Y-dragFrom.Y)<.001)return;
            if(!gestureCommitted){undo.Add(gestureBefore);redo.Clear();version++;gestureCommitted=true;}
            if(drag==Drag.Move){if(SelectionCount>1)MoveSelection(point.X-dragFrom.X,point.Y-dragFrom.Y);else MoveAnnotation(point.X-dragFrom.X,point.Y-dragFrom.Y);}
            else{point=new PointF(point.X+grab.X,point.Y+grab.Y);
                if(SelectionCount>1)ResizeSelection(point,square);
                else if((movingNote&&Painter.HasNote(selected.Kind))||selected.Kind==Tool.Text)ResizeNote(selected,orig,handle,point);
                else if(Shape.IsLinear(selected.Kind)){if(handle==0)selected.A=point;else if(handle==1)selected.B=point;else selected.SetMid(point,Pu(4)/k);}
                else{var target=ResizeBounds(HandleBounds(orig),handle,point,square);if(selected.Kind==Tool.Counter){if(selected.LeaderCounter)selected.B=new PointF(target.Left+target.Width/2,target.Top+target.Height/2);else selected.A=selected.B=new PointF(target.Left+target.Width/2,target.Top+target.Height/2);selected.MarkerSize=target.Width;}else TransformShape(selected,orig,orig.Box,target);}
            }
            foreach(var shape in SelectedShapes)shape.DropCache();NotifyEdit();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);lastPointer=e.Location;if(FullCaptureSurface&&CaptureMove!=null&&CaptureMove(e))return;
            if(panning){pan=new PointF(panStart.X+e.X-panFrom.X,panStart.Y+e.Y-panFrom.Y);Rescaled();return;}
            if(drag!=Drag.None)UpdateGesture(e.Location);else if(!Typing)UpdateHover(e.Location);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);lastPointer=e.Location;if(FullCaptureSurface&&CaptureUp!=null&&CaptureUp(e))return;
            if(panning){if(e.Button==panButton){panning=false;Capture=false;}return;}if(e.Button!=MouseButtons.Left||drag==Drag.None)return;
            UpdateGesture(e.Location);var completed=drag;drag=Drag.None;Capture=false;gestureShapes=null;gestureBefore=null;gestureRedo=null;orig=null;
            if(completed==Drag.Draw){var mark=cur;cur=null;if(mark.Kind==Tool.Crop){if(mark.Box.Width>=1&&mark.Box.Height>=1){Commit();Crop=RectangleF.Intersect(mark.Box,new RectangleF(PointF.Empty,Img.Size));NotifyEdit();}}
                else if(mark.Kind==Tool.Pen||mark.Box.Width+mark.Box.Height>=1){Commit();shapes.Add(mark);SelectShape(mark);NotifyEdit();if(mark.Kind==Tool.Cloud){movingNote=true;PlaceNoteInside(mark);StartText(mark.A,mark);}}
            }Fire(StateChanged);Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&(drag!=Drag.None||panning))CancelGesture();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(!Busy){hover=null;Invalidate();}}
        public void CancelGesture()
        {
            CancelColorPick();if(gestureBefore!=null&&gestureCommitted){if(undo.Count>0)undo.RemoveAt(undo.Count-1);Restore(gestureBefore);redo.Clear();if(gestureRedo!=null)redo.AddRange(gestureRedo);}cur=null;orig=null;drag=Drag.None;panning=false;gestureShapes=null;gestureBefore=null;gestureRedo=null;gestureCommitted=false;Capture=false;Fire(StateChanged);Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(Typing)return;
            if(e.KeyCode==Keys.Escape){if(Busy)CancelGesture();else SelectShape(null);e.Handled=true;return;}
            if(e.KeyCode==Keys.ShiftKey&&(drag==Drag.Draw||drag==Drag.Resize))UpdateGesture(lastPointer);
            if(Busy)return;
            if(e.KeyCode==Keys.Space){spacePan=true;return;}if(e.Control){switch(e.KeyCode){case Keys.A:SelectAll();break;case Keys.D:DuplicateSelected();break;case Keys.Z:Undo();break;case Keys.Y:Redo();break;default:return;}e.Handled=true;return;}
            if(e.KeyCode==Keys.Delete){DeleteSelected();e.Handled=true;return;}int step=e.Shift?10:1;switch(e.KeyCode){case Keys.Left:Nudge(-step,0);break;case Keys.Right:Nudge(step,0);break;case Keys.Up:Nudge(0,-step);break;case Keys.Down:Nudge(0,step);break;default:return;}e.Handled=true;
        }
        protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Space)spacePan=false;if(e.KeyCode==Keys.ShiftKey&&(drag==Drag.Draw||drag==Drag.Resize))UpdateGesture(lastPointer);base.OnKeyUp(e);}
        protected override void OnLostFocus(EventArgs e){spacePan=false;base.OnLostFocus(e);}

        Snapshot Take(){return new Snapshot{Shapes=shapes.Select(s=>s.Clone()).ToList(),Crop=Crop,Background=Bg==null?null:Bg.Clone(),BackgroundOn=BgOn,OutputCornerRadius=OutputCornerRadius,Extra=SaveExtra==null?null:SaveExtra()};}
        void Commit(){undo.Add(Take());if(undo.Count>128)undo.RemoveAt(0);redo.Clear();version++;nudged=null;}
        public void Checkpoint(){CommitText();Commit();Fire(StateChanged);}
        void Restore(Snapshot state)
        {
            string primary=selected==null?null:selected.Id;var ids=new HashSet<string>(SelectedShapes.Select(s=>s.Id));foreach(var mark in shapes)mark.DropCache();shapes=state.Shapes.Select(s=>s.Clone()).ToList();Crop=state.Crop;Bg=state.Background==null?null:state.Background.Clone();BgOn=state.BackgroundOn;OutputCornerRadius=state.OutputCornerRadius;
            selection.Clear();selection.AddRange(shapes.Where(s=>ids.Contains(s.Id)));selected=shapes.FirstOrDefault(s=>s.Id==primary);if(selected==null)selected=selection.LastOrDefault();hover=null;nudged=null;version++;if(LoadExtra!=null)LoadExtra(state.Extra);NotifyEdit();
        }
        public void Undo(){if(Typing){CancelText();return;}CancelGesture();if(undo.Count==0)return;redo.Add(Take());var last=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);Restore(last);}
        public void Redo(){if(Typing||redo.Count==0)return;CancelGesture();undo.Add(Take());var last=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);Restore(last);}
        public void SelectShape(Shape shape){selected=shape;SetSingleSelection(shape);movingNote=false;movingCounterBadge=false;Fire(StateChanged);Invalidate();}
        public void SetTool(Tool kind){CancelGesture();CommitText();Tool=kind;SelectShape(null);Cursor=kind==Tool.Text?Cursors.IBeam:Cursors.Cross;}
        public void SetColor(Color color)
        {
            Color=System.Drawing.Color.FromArgb(255,color);if(UsesOutlineFill(Tool)||Tool==Tool.Text)DefaultStyle.FillColor=System.Drawing.Color.FromArgb(DefaultStyle.FillColor.A,Color);
            if(selected!=null)EditSelected(delegate(Shape mark){mark.Color=Color;if(mark.Kind==Tool.Text){mark.FillEnabled=true;mark.FillColor=Color;}else if(UsesOutlineFill(mark.Kind))mark.FillColor=System.Drawing.Color.FromArgb(mark.FillColor.A,Color);mark.HasNoteTextColor=true;mark.NoteTextColor=System.Drawing.Color.White;});PlaceBox();Fire(StateChanged);
        }
        public void SetWeight(int weight){Weight=Math.Max(0,Math.Min(2,weight));if(selected!=null)EditSelected(delegate(Shape mark){mark.Weight=Weight;mark.Width=StrokeFor(Weight);mark.FontPx=FontFor(Weight);});PlaceBox();Fire(StateChanged);}
        public void DeleteSelected(){if(selected==null||Typing)return;DeleteSelection();}
        public void DuplicateSelected(){if(selected==null||Typing)return;DuplicateSelection();}
        public void Nudge(int dx,int dy){if(selected==null||Typing)return;if(nudged!=selected||nudgedAt!=version)Commit();nudged=selected;nudgedAt=version;foreach(var mark in SelectedShapes)mark.Offset(mark,dx,dy);NotifyEdit();}
        internal static void Renumber(List<Shape> list){int next=1;foreach(var mark in list.Where(s=>s.Kind==Tool.Counter).OrderBy(s=>s.Number))mark.Number=next++;}
        int NextNumber(){int value=0;foreach(var shape in shapes)if(shape.Kind==Tool.Counter)value=Math.Max(value,shape.Number);return value>=9999?9999:value+1;}
        public string HintText{get{return Typing?"输入文字，Enter 完成，Shift+Enter 换行，Esc 取消":HasSelection?"拖动移动，拖动控制点缩放；Shift/Ctrl 多选；Delete 删除":"拖动绘制，Shift 约束比例；滚轮缩放";}}

        float InputSize{get{return editing==null?DefaultStyle.FontPx:Painter.HasNote(editing.Kind)?Painter.NoteFont(editing):editing.FontPx;}}
        Shape LiveInput(){var live=editing==null?NewInputShape():editing.Clone();live.Text=box==null?live.Text:box.Text;return live;}
        Shape NewInputShape(){var mark=new Shape{Kind=Tool.Text,Color=Color,A=boxAt,B=boxAt};ApplyDefaultStyle(mark);return mark;}
        RectangleF InputBounds(){var mark=LiveInput();return Painter.HasNote(mark.Kind)?Painter.NoteBounds(mark):Painter.TextBounds(mark);}
        void StartText(PointF point,Shape existing)
        {
            if(Typing)CommitText();if(DrawStart!=null)DrawStart();editing=existing;boxAt=point;
            box=new TextBox{Multiline=true,BorderStyle=BorderStyle.None,MaxLength=20000,AcceptsReturn=true,Text=existing==null?"":existing.Text??""};
            box.TextChanged+=delegate{PlaceBox();};box.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter&&!e.Shift){e.SuppressKeyPress=true;CommitText();}else if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;CancelText();}};
            Controls.Add(box);PlaceBox();box.Focus();box.SelectionStart=box.TextLength;Fire(StateChanged);
        }
        void PlaceBox()
        {
            if(box==null)return;Fit();float fontSize=Math.Max(9,InputSize*k);if(inputFont==null||Math.Abs(inputFont.Size-fontSize)>.01){var prior=inputFont;inputFont=new Font("Microsoft YaHei UI",fontSize,GraphicsUnit.Pixel);box.Font=inputFont;if(prior!=null)prior.Dispose();}
            var live=LiveInput();bool note=Painter.HasNote(live.Kind);var bounds=InputBounds();box.WordWrap=note||live.TextWrap;box.ForeColor=System.Drawing.Color.White;box.BackColor=editing==null?Color:Painter.LabelColor(editing);
            var at=ToScreen(new PointF(bounds.Left+8,bounds.Top+8));int width=Math.Max(24,(int)Math.Round((bounds.Width-16)*k));
            var size=TextRenderer.MeasureText((box.Text.Length==0?" ":box.Text)+(box.Text.EndsWith("\n")?" ":""),box.Font,new Size(box.WordWrap?width:32767,32767),TextFormatFlags.NoPadding|TextFormatFlags.TextBoxControl|(box.WordWrap?TextFormatFlags.WordBreak:0));
            box.Bounds=new Rectangle(at,new Size(box.WordWrap?width:Math.Max(24,size.Width+4),Math.Max(box.Font.Height,size.Height)));lastInputBounds=ScreenRect(bounds);
        }
        bool BoxStale(){return false;}
        void BoxLabel(Graphics graphics){if(box==null)return;var bounds=ScreenRect(InputBounds());using(var path=Theme.Round(bounds,6*k))using(var brush=new SolidBrush(box.BackColor))graphics.FillPath(brush,path);}
        void DropBox(TextBox input){Controls.Remove(input);input.Dispose();if(inputFont!=null){inputFont.Dispose();inputFont=null;}lastInputBounds=Rectangle.Empty;}
        public void CommitText()
        {
            if(box==null)return;string text=box.Text.TrimEnd();var previous=editing;var live=LiveInput();var input=box;box=null;editing=null;DropBox(input);
            if(previous==null){if(text.Length>0){Commit();live.Text=text;shapes.Add(live);SelectShape(live);}}
            else if(text!=(previous.Text??"")){Commit();if(text.Length==0&&(previous.Kind==Tool.Text||previous.Kind==Tool.Callout)){shapes.Remove(previous);SelectShape(null);}else previous.Text=text;}
            Fire(Changed);Fire(StateChanged);Invalidate();
        }
        void CancelText(){if(box==null)return;var previous=box;box=null;editing=null;DropBox(previous);Fire(StateChanged);Invalidate();}

        static Bitmap CopyArea(Bitmap source,Rectangle area){return source.Clone(area,PixelFormat.Format32bppArgb);}
        public Bitmap Render(){return RenderContent(false);}
        Bitmap RenderContent(bool preview)
        {
            var area=CropRect;var result=CopyArea(Img,area);
            try{using(var g=Graphics.FromImage(result)){g.TranslateTransform(-area.X,-area.Y);Painter.DrawMarks(g,shapes,preview&&cur!=null&&cur.Kind!=Tool.Crop?cur:null,preview?editing:null,Img,area);if(preview)PaintEditingAnchor(g);}if(BgOn&&Bg!=null){BackgroundStyle.Validate(Bg,result.Size);var composed=Backdrop.Compose(result,Bg,Info());result.Dispose();result=composed;}return RoundOutput(result,OutputCornerRadius);}catch{result.Dispose();throw;}
        }
        public Bitmap RenderMarks(Rectangle area){var result=new Bitmap(Math.Max(1,area.Width),Math.Max(1,area.Height),PixelFormat.Format32bppArgb);try{using(var g=Graphics.FromImage(result)){g.TranslateTransform(-area.X,-area.Y);Painter.DrawMarks(g,shapes,null,null,Img,area);}return result;}catch{result.Dispose();throw;}}
        public void ShowPlayFrame(byte[] pixels,int width,int height)
        {
            var frame=new Bitmap(width,height,PixelFormat.Format32bppArgb);var data=frame.LockBits(new Rectangle(Point.Empty,frame.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int row=0;row<height;row++)System.Runtime.InteropServices.Marshal.Copy(pixels,row*width*4,IntPtr.Add(data.Scan0,row*data.Stride),width*4);}finally{frame.UnlockBits(data);}if(playFrame!=null)playFrame.Dispose();playFrame=frame;if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)Invalidate);
        }
        public void EndPlayFrame(){if(playFrame!=null){playFrame.Dispose();playFrame=null;}Invalidate();}
        public void ReplaceImage(Bitmap image){if(image==null)throw new ArgumentNullException("image");if(image.Size!=Img.Size){image.Dispose();return;}var old=Img;Img=image;old.Dispose();foreach(var mark in shapes)mark.DropCache();if(captureScene!=null){captureScene.Dispose();captureScene=null;}if(backdropInfo!=null){backdropInfo.Dispose();backdropInfo=null;}EndPlayFrame();Invalidate();}
        public void Release(){if(released)return;released=true;foreach(var mark in shapes)mark.DropCache();if(cur!=null)cur.DropCache();if(backdropInfo!=null)backdropInfo.Dispose();if(playFrame!=null)playFrame.Dispose();Img.Dispose();}
    }
}
