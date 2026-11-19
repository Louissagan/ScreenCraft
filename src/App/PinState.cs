using System;
using System.Collections.Generic;
using System.Drawing;
namespace ScreenCraft
{
    public sealed class PinState
    {
        public string Id=Guid.NewGuid().ToString("N");
        public Rectangle Bounds;
        public float Zoom=1;
        public int OpacityPercent=100;
        public bool TopMost=true,ClickThrough,Thumbnail;public int QuarterTurns;public bool FlipX,FlipY,HiddenByUser;
        public PinState Clone(){return (PinState)MemberwiseClone();}
        public string ToJson(){return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new StateDto{Id=Id,Bounds=new[]{Bounds.X,Bounds.Y,Bounds.Width,Bounds.Height},Zoom=Zoom,Opacity=OpacityPercent,TopMost=TopMost,ClickThrough=ClickThrough,Thumbnail=Thumbnail,QuarterTurns=QuarterTurns,FlipX=FlipX,FlipY=FlipY,HiddenByUser=HiddenByUser});}
        public static PinState FromJson(string json){if(json==null||json.Length>4096)throw new ArgumentException("贴图状态无效。");var dto=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<StateDto>(json);if(dto==null||string.IsNullOrEmpty(dto.Id)||dto.Id.Length>64||dto.Bounds==null||dto.Bounds.Length!=4||dto.Bounds[2]<1||dto.Bounds[2]>100000||dto.Bounds[3]<1||dto.Bounds[3]>100000||float.IsNaN(dto.Zoom)||float.IsInfinity(dto.Zoom)||dto.Zoom<=0||dto.Zoom>100||dto.Opacity<20||dto.Opacity>100||dto.QuarterTurns<0||dto.QuarterTurns>3)throw new ArgumentException("贴图状态无效。");return new PinState{Id=dto.Id,Bounds=new Rectangle(dto.Bounds[0],dto.Bounds[1],dto.Bounds[2],dto.Bounds[3]),Zoom=dto.Zoom,OpacityPercent=dto.Opacity,TopMost=dto.TopMost,ClickThrough=dto.ClickThrough,Thumbnail=dto.Thumbnail,QuarterTurns=dto.QuarterTurns,FlipX=dto.FlipX,FlipY=dto.FlipY,HiddenByUser=dto.HiddenByUser};}
        public sealed class StateDto{public string Id;public int[] Bounds;public float Zoom;public int Opacity;public bool TopMost,ClickThrough,Thumbnail;public int QuarterTurns;public bool FlipX,FlipY,HiddenByUser;}
    }
    public sealed class PinRegistry<T> where T:class
    {
        readonly Dictionary<string,T> items=new Dictionary<string,T>();
        public int Count{get{return items.Count;}}
        public List<T> Values{get{return new List<T>(items.Values);}}
        public void Add(string id,T pin){if(string.IsNullOrEmpty(id)||pin==null)throw new ArgumentException("贴图 ID 或对象无效。");if(items.ContainsKey(id))throw new InvalidOperationException("贴图 ID 已存在，不能覆盖旧图。");items.Add(id,pin);}
        public bool Remove(string id){return items.Remove(id);}
        public T Find(string id){T value;return items.TryGetValue(id,out value)?value:null;}
    }
    public static class PinGeometry
    {
        public static float ResizeFactor(Size original,Point delta){double w=Math.Max(1,original.Width),h=Math.Max(1,original.Height);return (float)Math.Max(.001,((w+delta.X)*w+(h+delta.Y)*h)/(w*w+h*h));}
        public static Rectangle KeepReachable(Rectangle bounds,Rectangle[] areas)
        {
            if(areas==null||areas.Length==0)return bounds;
            foreach(var a in areas){var visible=Rectangle.Intersect(bounds,a);if(visible.Width>=Math.Min(40,bounds.Width)&&visible.Height>=Math.Min(40,bounds.Height))return bounds;}
            Rectangle closest=areas[0];double distance=double.MaxValue;
            foreach(var a in areas){double dx=Math.Max(0,Math.Max(a.Left-bounds.Right,bounds.Left-a.Right)),dy=Math.Max(0,Math.Max(a.Top-bounds.Bottom,bounds.Top-a.Bottom));double d=dx*dx+dy*dy;if(d<distance){distance=d;closest=a;}}
            return new Rectangle(Math.Max(closest.Left,Math.Min(closest.Right-Math.Min(80,bounds.Width),bounds.X)),Math.Max(closest.Top,Math.Min(closest.Bottom-Math.Min(80,bounds.Height),bounds.Y)),bounds.Width,bounds.Height);
        }
        public static Rectangle Place(Size size,Rectangle work,Point cursor,int ordinal)
        {
            int offset=(ordinal%8)*24;
            return new Rectangle(work.Left+Math.Max(0,Math.Min(work.Width-size.Width,cursor.X-work.Left-size.Width/2+offset)),
                work.Top+Math.Max(0,Math.Min(work.Height-size.Height,cursor.Y-work.Top-size.Height/2+offset)),size.Width,size.Height);
        }
        public static int StepOpacity(int current,int wheelDelta){return Math.Max(20,Math.Min(100,current+(wheelDelta>0?5:-5)));}
    }
}
