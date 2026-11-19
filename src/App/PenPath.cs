using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenCraft
{
    public partial class Shape
    {
        internal List<PointF> PathTangents;
        internal List<float> PathWidths;
        internal PointF PathTangent(int index) { return PathTangents == null ? PointF.Empty : PathTangents[index]; }
        internal float PathWidth(int index) { return PathWidths == null || PathWidths[index] == 0 ? Width : PathWidths[index]; }
        internal float MaximumPathWidth { get { float width=Width; if(PathWidths!=null)foreach(float segment in PathWidths)width=Math.Max(width,segment);return width; } }
        internal void SetPathTangent(int index,PointF value)
        {
            if(PathTangents==null)PathTangents=new List<PointF>(new PointF[Points.Count]);
            PathTangents[index]=value;
        }
        internal void SetPathWidth(int index,float value)
        {
            if(PathWidths==null)PathWidths=new List<float>(new float[Points.Count]);
            PathWidths[index]=value;
        }
    }

    public static partial class Painter
    {
        static void AddPenSegment(GraphicsPath path,Shape shape,int index)
        {
            int next=(index+1)%shape.Points.Count;
            var a=shape.Points[index];var b=shape.Points[next];
            var outgoing=shape.PathTangent(index);var incoming=shape.PathTangent(next);
            if(outgoing.IsEmpty&&incoming.IsEmpty)path.AddLine(a,b);
            else path.AddBezier(a,new PointF(a.X+outgoing.X,a.Y+outgoing.Y),new PointF(b.X-incoming.X,b.Y-incoming.Y),b);
        }
        static GraphicsPath PenPath(Shape shape)
        {
            var path=new GraphicsPath();
            if(shape.Points==null||shape.Points.Count<2)return path;
            int count=shape.Points.Count-(shape.ClosedPen?0:1);
            for(int i=0;i<count;i++)AddPenSegment(path,shape,i);
            if(shape.ClosedPen)path.CloseFigure();
            return path;
        }
        static void DrawPenSegments(Graphics g,Shape shape,Pen pen)
        {
            using(var segmentPen=(Pen)pen.Clone())
            using(var path=new GraphicsPath()){
                segmentPen.StartCap=segmentPen.EndCap=LineCap.Round;
                for(int i=0;i<shape.Points.Count;i++){
                    path.Reset();AddPenSegment(path,shape,i);
                    segmentPen.Width=shape.PathWidth(i);g.DrawPath(segmentPen,path);
                }
            }
        }
    }

    public partial class Canvas
    {
        int pathAnchor=-1,draftTangent=-1,pathPointerPart;
        bool pathDragging,pathChanged;
        Point pathPointerStart;
        Shape pathBefore;
        internal void ActivatePenPath()
        {
            if(Tool==Tool.PenPath){Focus();return;}
            var path=selected!=null&&selected.Polygon?selected:null;
            SetTool(Tool.PenPath);if(path!=null)SelectShape(path);
        }
        internal bool EditingPath { get { return Tool==Tool.PenPath&&SelectionCount==1&&selected!=null&&selected.Polygon&&!Typing; } }
        internal bool HasPathAnchor { get { return EditingPath&&pathAnchor>=0&&pathAnchor<selected.Points.Count; } }
        internal bool CanChangeStrokeWidth { get { return ActiveKind!=Tool.PenPath||Tool==Tool.PenPath; } }
        internal float ActivePathWidth { get { return HasPathAnchor?selected.PathWidth(pathAnchor):selected==null?DefaultStyle.Width:selected.Width; } }
        internal void SetPathWidth(float value)
        {
            if(Tool!=Tool.PenPath)return;
            if(HasPathAnchor){int index=pathAnchor;EditSelected(s=>s.SetPathWidth(index,value));}
            else if(selected!=null)EditSelected(s=>{if(s.Polygon)s.Width=value;});
            else ChangeDefaultStyle(d=>d.Width=value);
        }
        internal void ConvertPathAnchor()
        {
            if(!HasPathAnchor||Busy)return;
            int index=pathAnchor;
            EditSelected(s=>{
                var tangent=s.PathTangent(index);
                if(tangent.IsEmpty){
                    int count=s.Points.Count;
                    var previous=s.Points[(index+count-1)%count];var next=s.Points[(index+1)%count];
                    tangent=new PointF((next.X-previous.X)/4,(next.Y-previous.Y)/4);
                    if(tangent.IsEmpty)tangent=new PointF((next.X-s.Points[index].X)/3,(next.Y-s.Points[index].Y)/3);
                }else tangent=PointF.Empty;
                var before=s.Clone();s.SetPathTangent(index,tangent);PreserveRotationFrame(s,before);
            });
        }
        Point PathHandle(Shape shape,int index,int side,PointF center)
        {
            var anchor=shape.Points[index];var tangent=shape.PathTangent(index);
            var point=new PointF(anchor.X+side*tangent.X,anchor.Y+side*tangent.Y);
            return ToScreen(Painter.RotatePoint(point,center,shape.Rotation));
        }
        bool NearPathPart(Point cursor,Point at){return Math.Abs(cursor.X-at.X)<=Pu(6)&&Math.Abs(cursor.Y-at.Y)<=Pu(6);}
        int PathPartAt(Point point,out int part)
        {
            part=0;
            if(!EditingPath)return -1;
            var center=Painter.RotationCenter(selected);
            for(int i=0;i<selected.Points.Count;i++)if(NearPathPart(point,PathHandle(selected,i,0,center)))return i;
            if(HasPathAnchor&&!selected.PathTangent(pathAnchor).IsEmpty){
                if(NearPathPart(point,PathHandle(selected,pathAnchor,1,center)))part=1;
                else if(NearPathPart(point,PathHandle(selected,pathAnchor,-1,center)))part=-1;
                if(part!=0)return pathAnchor;
            }
            return -1;
        }
        bool PathAnchorDown(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left||Busy||!EditingPath)return false;
            int part;int index=PathPartAt(e.Location,out part);
            if(index<0){
                if(Hit(selected,ToImg(e.Location,false),Pu(5)/k)){pathAnchor=-1;Fire(StateChanged);Invalidate();}
                return false;
            }
            pathAnchor=index;Focus();
            if(part==0&&(InputModifiers&Keys.Alt)!=0){ConvertPathAnchor();return true;}
            pathDragging=true;pathChanged=false;pathPointerPart=part;pathPointerStart=e.Location;
            pathBefore=selected.Clone();gestureBefore=Take();gestureRedo=redo.ToArray();gestureCommitted=false;
            Capture=true;Fire(StateChanged);Invalidate();return true;
        }
        void StartDraftTangent(int index,Point at)
        {
            if(Tool!=Tool.PenPath)return;
            draftTangent=index;pathPointerStart=at;Capture=true;
        }
        bool UpdatePathPointer(Point at)
        {
            if(!pathDragging&&draftTangent<0)return false;
            if(!pathChanged&&Math.Abs(at.X-pathPointerStart.X)<Pu(3)&&Math.Abs(at.Y-pathPointerStart.Y)<Pu(3))return true;
            if(draftTangent>=0){
                var anchor=cur.Points[draftTangent];var point=ToImg(at,false);
                cur.SetPathTangent(draftTangent,new PointF(point.X-anchor.X,point.Y-anchor.Y));Invalidate();return true;
            }
            if(!gestureCommitted){undo.Add(gestureBefore);redo.Clear();version++;gestureCommitted=true;}
            pathChanged=true;
            var before=Painter.LocalPoint(pathBefore,ToImg(pathPointerStart,false));var now=Painter.LocalPoint(pathBefore,ToImg(at,false));
            var start=pathBefore.Points[pathAnchor];
            selected.Points=new List<PointF>(pathBefore.Points);
            selected.PathTangents=pathBefore.PathTangents==null?null:new List<PointF>(pathBefore.PathTangents);
            if(pathPointerPart==0)selected.Points[pathAnchor]=new PointF(start.X+now.X-before.X,start.Y+now.Y-before.Y);
            else{
                var tangent=pathBefore.PathTangent(pathAnchor);
                selected.SetPathTangent(pathAnchor,new PointF(tangent.X+(now.X-before.X)*pathPointerPart,tangent.Y+(now.Y-before.Y)*pathPointerPart));
            }
            selected.A=selected.Points[0];selected.B=selected.Points[selected.Points.Count-1];
            PreserveRotationFrame(selected,pathBefore);NotifyEdit();return true;
        }
        bool FinishPathPointer(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left||!pathDragging&&draftTangent<0)return false;
            UpdatePathPointer(e.Location);CancelPathPointer();
            gestureBefore=null;gestureRedo=null;gestureCommitted=false;Capture=false;Fire(StateChanged);Invalidate();return true;
        }
        void CancelPathPointer(){draftTangent=-1;pathDragging=false;pathChanged=false;pathBefore=null;}
        void PaintPathAnchors(Graphics g,Shape shape,int active)
        {
            var center=Painter.RotationCenter(shape);
            var state=g.Save();try{
                g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var border=new Pen(CaptureLook.Blue,1.5f))
                using(var fill=new SolidBrush(System.Drawing.Color.White))
                using(var accent=new SolidBrush(CaptureLook.Blue)){
                    if(active>=0&&!shape.PathTangent(active).IsEmpty){
                        var a=PathHandle(shape,active,-1,center);var b=PathHandle(shape,active,1,center);
                        g.DrawLine(border,a,b);
                        foreach(var p in new[]{a,b}){int r=Pu(4);g.FillEllipse(fill,p.X-r,p.Y-r,r*2,r*2);g.DrawEllipse(border,p.X-r,p.Y-r,r*2,r*2);}
                    }
                    for(int i=0;i<shape.Points.Count;i++){
                        var p=PathHandle(shape,i,0,center);int r=Pu(i==active||polygonDrawing&&i==0?5:3);
                        g.FillRectangle(i==active?accent:fill,p.X-r,p.Y-r,r*2,r*2);g.DrawRectangle(border,p.X-r,p.Y-r,r*2,r*2);
                    }
                }
            }finally{g.Restore(state);}
        }
    }
}
