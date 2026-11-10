using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ScreenCraft
{
    public partial class Canvas
    {
        readonly List<Shape> selection=new List<Shape>();
        Dictionary<Shape,Shape> gestureShapes;
        RectangleF gestureBounds;
        public int SelectionCount{get{return selected==null?0:SelectedShapes.Length;}}
        public Shape[] SelectedShapes{get{return selected==null?new Shape[0]:selection.Count==0?new[]{selected}:selection.Where(shapes.Contains).ToArray();}}
        void SetSingleSelection(Shape shape){nudged=null;selection.Clear();if(shape!=null)selection.Add(shape);}
        void ToggleSelection(Shape shape)
        {
            nudged=null;if(selected!=null&&selection.Count==0)selection.Add(selected);
            if(selection.Contains(shape))selection.Remove(shape);else selection.Add(shape);
            selected=selection.LastOrDefault();movingNote=false;movingCounterBadge=false;Invalidate();Fire(StateChanged);
        }
        public void SelectAll(){CommitText();nudged=null;selection.Clear();selection.AddRange(shapes);selected=selection.LastOrDefault();movingNote=false;movingCounterBadge=false;Invalidate();Fire(StateChanged);}
        internal RectangleF SelectedBounds{get{return GroupBounds();}}
        RectangleF GroupBounds(){var items=SelectedShapes;var r=Painter.Bounds(items[0]);foreach(var s in items.Skip(1))r=RectangleF.Union(r,Painter.Bounds(s));return r;}
        internal static PointF[] BoxHandles(RectangleF b){return new[]{new PointF(b.Left,b.Top),new PointF(b.Right,b.Top),new PointF(b.Right,b.Bottom),new PointF(b.Left,b.Bottom),new PointF(b.Left+b.Width/2,b.Top),new PointF(b.Right,b.Top+b.Height/2),new PointF(b.Left+b.Width/2,b.Bottom),new PointF(b.Left,b.Top+b.Height/2)};}
        internal static RectangleF ResizeBounds(RectangleF b,int handle,PointF p,bool square)
        {
            float left=b.Left,right=b.Right,top=b.Top,bottom=b.Bottom;
            if(handle==0||handle==3||handle==7)left=Math.Min(p.X,right-1);
            if(handle==1||handle==2||handle==5)right=Math.Max(p.X,left+1);
            if(handle==0||handle==1||handle==4)top=Math.Min(p.Y,bottom-1);
            if(handle==2||handle==3||handle==6)bottom=Math.Max(p.Y,top+1);
            if(square&&handle<4){float edge=Math.Max(right-left,bottom-top);if(handle==0||handle==3)left=right-edge;else right=left+edge;if(handle==0||handle==1)top=bottom-edge;else bottom=top+edge;}
            return RectangleF.FromLTRB(left,top,right,bottom);
        }
        void BeginSelectionGesture(){gestureShapes=SelectedShapes.ToDictionary(s=>s,s=>s.Clone());gestureBounds=GroupBounds();}
        void MoveSelection(float dx,float dy){foreach(var pair in gestureShapes)pair.Key.Offset(pair.Value,dx,dy);Invalidate();}
        void RestoreSelectionGesture()
        {
            if(gestureShapes==null)return;var restored=new List<Shape>();Shape primary=null;
            foreach(var pair in gestureShapes){int index=shapes.IndexOf(pair.Key);if(index<0)continue;pair.Key.DropCache();var copy=pair.Value.Clone();shapes[index]=copy;restored.Add(copy);if(pair.Key==selected)primary=copy;}
            selection.Clear();selection.AddRange(restored);selected=primary??restored.LastOrDefault();
        }
        void ResizeSelection(PointF to,bool square)
        {
            var target=ResizeBounds(gestureBounds,handle,to,square);foreach(var pair in gestureShapes)TransformShape(pair.Key,pair.Value,gestureBounds,target);Invalidate();
        }
        internal static void TransformShape(Shape s,Shape source,RectangleF from,RectangleF to)
        {
            float sx=to.Width/Math.Max(1,from.Width),sy=to.Height/Math.Max(1,from.Height);
            Func<PointF,PointF> map=p=>new PointF(to.X+(p.X-from.X)*sx,to.Y+(p.Y-from.Y)*sy);
            s.A=map(source.A);s.B=map(source.B);s.C=map(source.C);
            if(source.Points!=null)s.Points=source.Points.Select(map).ToList();
            s.NoteOffset=new PointF(source.NoteOffset.X*sx,source.NoteOffset.Y*sy);
            if(s.Kind==Tool.Text||Painter.HasNote(s.Kind)){s.NoteWidth=Math.Max(60,Math.Min(800,source.NoteWidth*sx));s.FontPx=Math.Max(8,Math.Min(300,source.FontPx*sy));if(source.NoteFontPx>0)s.NoteFontPx=Math.Max(8,Math.Min(300,source.NoteFontPx*sy));s.NoteHeight=Math.Min(10000,source.NoteHeight*sy);s.MarkerSize=Math.Min(200,source.MarkerSize*(sx+sy)/2);}
            if(source.Rotation!=0){var expected=map(Painter.RotationCenter(source));var actual=Painter.RotationCenter(s);s.Offset(s,expected.X-actual.X,expected.Y-actual.Y);}
            s.DropCache();
        }
        void OutlineGroup(Graphics g){var b=GroupBounds();var frame=new Shape{Kind=Tool.Rect,A=b.Location,B=new PointF(b.Right,b.Bottom),Width=.25f};Outline(g,frame,true);}
        void DeleteSelection(){Commit();foreach(var s in SelectedShapes){shapes.Remove(s);s.DropCache();}selection.Clear();selected=null;hover=null;Fire(Changed);Fire(StateChanged);Invalidate();}
        void DuplicateSelection()
        {
            Commit();var copies=new List<Shape>();float distance=(float)Math.Round(Math.Max(1f,Pu(14)/k));
            foreach(var s in SelectedShapes){var copy=s.Clone();copy.Id=Guid.NewGuid().ToString("N");copy.Offset(s,distance,distance);if(copy.Kind==Tool.Counter)copy.Number=NextNumber();shapes.Add(copy);copies.Add(copy);}
            selection.Clear();selection.AddRange(copies);selected=copies.LastOrDefault();Fire(Changed);Fire(StateChanged);Invalidate();
        }
    }
}
