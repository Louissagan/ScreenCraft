using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ScreenCraft
{
    public partial class Shape { public bool Polygon; }

    public partial class Canvas
    {
        bool polygonDrawing,polygonJustClosed;
        readonly Stack<PointF> polygonRedo=new Stack<PointF>();
        public bool DrawingPolygon{get{return polygonDrawing;}}
        internal int PolygonPointCount{get{return polygonDrawing&&cur!=null?cur.Points.Count:0;}}
        void BeginPolygon(PointF point)
        {
            cur=NewShape(Tool.Pen);cur.A=cur.B=point;cur.Points=new List<PointF>{point};
            polygonDrawing=true;polygonRedo.Clear();RefreshPolygonStyle();hover=null;Cursor=Cursors.Cross;Fire(StateChanged);Invalidate();
        }
        void RefreshPolygonStyle()
        {
            if(!polygonDrawing||cur==null)return;cur.Color=Color;ApplyDefaultStyle(cur);cur.Polygon=true;cur.ClosedPen=false;cur.SmoothPen=false;cur.FreehandArrow=false;
        }
        bool NearPolygonStart(Point point){return polygonDrawing&&SegmentDistance(ToImg(point,false),cur.Points[0],cur.Points[0])<=Pu(8)/k;}
        bool PolygonPointerDown(MouseEventArgs e)
        {
            if(!polygonDrawing)return false;Focus();Fit();
            if(e.Button==MouseButtons.Right){UndoPolygonPoint();return true;}
            if(e.Button!=MouseButtons.Left)return true;
            if(NearPolygonStart(e.Location)){CompletePolygon();return true;}
            var point=ToImg(e.Location,false);if(!Crop.Contains(point))return true;
            var previous=cur.Points[cur.Points.Count-1];
            if(SegmentDistance(point,previous,previous)>Pu(2)/k&&cur.Points.Count<50000){
                if((InputModifiers&Keys.Shift)!=0)point=Constrained(previous,point,Tool.Line);
                if(!Crop.Contains(point))return true;cur.Points.Add(point);cur.B=point;polygonRedo.Clear();Fire(StateChanged);Invalidate();
            }
            if(e.Clicks>1)CompletePolygon();return true;
        }
        static bool PolygonHasArea(IList<PointF> points)
        {
            if(points.Count<3)return false;var a=points[0];var b=points[1];
            for(int i=2;i<points.Count;i++)if(Math.Abs((double)(b.X-a.X)*(points[i].Y-a.Y)-(double)(b.Y-a.Y)*(points[i].X-a.X))>.001)return true;
            return false;
        }
        public bool CompletePolygon()
        {
            if(!polygonDrawing)return true;if(!PolygonHasArea(cur.Points)){Fire(StateChanged);return false;}
            var mark=cur;cur=null;polygonDrawing=false;polygonRedo.Clear();mark.ClosedPen=true;mark.SmoothPen=false;mark.FreehandArrow=false;
            AnnotationDocument.ValidateShape(mark);Commit();shapes.Add(mark);polygonJustClosed=true;SelectShape(mark);NotifyEdit();return true;
        }
        void UndoPolygonPoint()
        {
            if(!polygonDrawing)return;if(cur.Points.Count<=1){CancelGesture();return;}
            polygonRedo.Push(cur.Points[cur.Points.Count-1]);cur.Points.RemoveAt(cur.Points.Count-1);cur.B=cur.Points[cur.Points.Count-1];Fire(StateChanged);Invalidate();
        }
        void RedoPolygonPoint()
        {
            if(!polygonDrawing||polygonRedo.Count==0)return;cur.Points.Add(polygonRedo.Pop());cur.B=cur.Points[cur.Points.Count-1];Fire(StateChanged);Invalidate();
        }
        internal bool PolygonKey(Keys keys)
        {
            if(!polygonDrawing)return false;
            if(keys==Keys.Escape)CancelGesture();
            else if(keys==Keys.Enter)CompletePolygon();
            else if(keys==Keys.Back||keys==Keys.Delete||keys==(Keys.Control|Keys.Z))UndoPolygonPoint();
            else if(keys==(Keys.Control|Keys.Y))RedoPolygonPoint();
            else if(keys==Keys.Tab||keys==(Keys.Shift|Keys.Tab))return false;
            // Prevent copy, save and range movement from consuming a partial polygon.
            return true;
        }
        void PaintPolygonAnchors(Graphics graphics)
        {
            if(!polygonDrawing||cur==null)return;var state=graphics.Save();try{
                graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var border=new Pen(CaptureLook.Blue,1.5f))using(var fill=new SolidBrush(System.Drawing.Color.White)){
                    for(int i=0;i<cur.Points.Count;i++){var point=ToScreen(cur.Points[i]);int radius=Pu(i==0?5:3);graphics.FillEllipse(fill,point.X-radius,point.Y-radius,radius*2,radius*2);graphics.DrawEllipse(border,point.X-radius,point.Y-radius,radius*2,radius*2);}
                }
            }finally{graphics.Restore(state);}
        }
    }
}
