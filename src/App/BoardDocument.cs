using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Script.Serialization;
namespace ScreenCraft
{
    public sealed class BoardItem
    {
        public string Id=Guid.NewGuid().ToString("N"),Resource;
        public float X,Y,Scale=1;public int Turns;
        public BoardItem Clone(){return (BoardItem)MemberwiseClone();}
    }
    public sealed class BoardDocument:IDisposable
    {
        readonly Dictionary<string,Bitmap> resources=new Dictionary<string,Bitmap>();readonly List<BoardSnapshot> undo=new List<BoardSnapshot>(),redo=new List<BoardSnapshot>();List<BoardItem> items=new List<BoardItem>();string clean="";long pixels;bool disposed;
        public Color Background{get;private set;}
        public event EventHandler Changed;
        public BoardItem[] Items{get{return items.Select(delegate(BoardItem item){return item.Clone();}).ToArray();}}
        public bool CanUndo{get{return undo.Count>0;}}public bool CanRedo{get{return redo.Count>0;}}
        public bool Dirty{get{return Signature()!=clean;}}
        public BoardDocument(){Background=Color.Transparent;MarkClean();}
        string Signature(){return new JavaScriptSerializer().Serialize(new BoardDto{Version=1,Background=Background.ToArgb(),Items=Items});}
        public void MarkClean(){clean=Signature();}
        void Fire(){var handler=Changed;if(handler!=null)handler(this,EventArgs.Empty);}
        BoardSnapshot Take(){return new BoardSnapshot{Items=Items,Background=Background};}
        void Restore(BoardSnapshot state){items=state.Items.Select(delegate(BoardItem item){return item.Clone();}).ToList();Background=state.Background;Fire();}
        internal void Checkpoint(){undo.Add(Take());if(undo.Count>100)undo.RemoveAt(0);redo.Clear();}
        public string AddImage(Bitmap ownedImage,PointF at)
        {
            if(ownedImage==null)throw new ArgumentNullException("ownedImage");
            try{if(items.Count>=100||(long)ownedImage.Width*ownedImage.Height+pixels>64000000)throw new InvalidDataException("画板最多 100 张图，含撤销资源合计不超过 6400 万像素。请新建画板或缩小图片。");if(!Finite(at.X)||!Finite(at.Y))throw new ArgumentException("画板坐标无效。");string resource=Guid.NewGuid().ToString("N");var item=new BoardItem{Resource=resource,X=at.X,Y=at.Y};Checkpoint();resources.Add(resource,ownedImage);pixels+=(long)ownedImage.Width*ownedImage.Height;items.Add(item);ownedImage=null;Fire();return item.Id;}finally{if(ownedImage!=null)ownedImage.Dispose();}
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value)&&Math.Abs(value)<=100000;}
        internal BoardItem Find(string id){return items.Find(delegate(BoardItem item){return item.Id==id;});}
        internal Bitmap Source(string resource){return resources[resource];}
        void Validate(BoardItem item){if(item==null||!Finite(item.X)||!Finite(item.Y)||!Finite(item.Scale)||item.Scale<.01f||item.Scale>10||item.Turns<0||item.Turns>3||!resources.ContainsKey(item.Resource))throw new InvalidDataException("画板图像参数无效。");}
        public RectangleF Bounds(BoardItem item){var source=Source(item.Resource);return new RectangleF(item.X,item.Y,(item.Turns%2==0?source.Width:source.Height)*item.Scale,(item.Turns%2==0?source.Height:source.Width)*item.Scale);}
        public RectangleF ContentBounds{get{if(items.Count==0)return new RectangleF(0,0,1,1);var bounds=Bounds(items[0]);foreach(var item in items.Skip(1))bounds=RectangleF.Union(bounds,Bounds(item));return bounds;}}
        public void Change(string id,Action<BoardItem> edit){var item=Find(id);if(item==null)return;var candidate=item.Clone();edit(candidate);Validate(candidate);Checkpoint();items[items.IndexOf(item)]=candidate;Fire();}
        internal void PreviewChange(string id,Action<BoardItem> edit){var item=Find(id);if(item==null)return;var candidate=item.Clone();edit(candidate);Validate(candidate);items[items.IndexOf(item)]=candidate;Fire();}
        public void Delete(string id){var item=Find(id);if(item==null)return;Checkpoint();items.Remove(item);Fire();}
        public string Duplicate(string id){var item=Find(id);if(item==null)return null;if(items.Count>=100)throw new InvalidOperationException("画板已达到 100 张上限。");Checkpoint();var copy=item.Clone();copy.Id=Guid.NewGuid().ToString("N");copy.X+=24;copy.Y+=24;items.Add(copy);Fire();return copy.Id;}
        public void Reorder(string id,int step){var item=Find(id);if(item==null)return;int from=items.IndexOf(item),to=Math.Max(0,Math.Min(items.Count-1,from+step));if(to==from)return;Checkpoint();items.RemoveAt(from);items.Insert(to,item);Fire();}
        public void SetBackground(Color color){if(Background==color)return;Checkpoint();Background=color;Fire();}
        public void Undo(){if(!CanUndo)return;redo.Add(Take());var state=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);Restore(state);}
        public void Redo(){if(!CanRedo)return;undo.Add(Take());var state=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);Restore(state);}
        internal void Draw(Graphics g)
        {
            foreach(var item in items){var bounds=Bounds(item);if(!g.IsVisible(bounds))continue;var source=Source(item.Resource);var tl=new PointF(bounds.Left,bounds.Top);var tr=new PointF(bounds.Right,bounds.Top);var bl=new PointF(bounds.Left,bounds.Bottom);var br=new PointF(bounds.Right,bounds.Bottom);var corners=item.Turns==0?new[]{tl,tr,bl}:item.Turns==1?new[]{tr,br,tl}:item.Turns==2?new[]{br,bl,tr}:new[]{bl,tl,br};using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(source,corners,new RectangleF(0,0,source.Width,source.Height),GraphicsUnit.Pixel,attributes);}}
        }
        public Bitmap Render()
        {
            var area=Rectangle.FromLTRB((int)Math.Floor(ContentBounds.Left),(int)Math.Floor(ContentBounds.Top),(int)Math.Ceiling(ContentBounds.Right),(int)Math.Ceiling(ContentBounds.Bottom));
            if(area.Width<1||area.Height<1||area.Width>32767||area.Height>32767||(long)area.Width*area.Height>64000000)throw new InvalidOperationException("画板内容边界过大，请靠拢图片或缩小比例后导出（上限 6400 万像素）。");
            var image=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image)){g.Clear(Background);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.TranslateTransform(-area.Left,-area.Top);Draw(g);}return image;
        }
        public Bitmap RenderItem(string id){var item=Find(id);if(item==null)throw new InvalidOperationException("请先选择一张图片。");var bounds=Bounds(item);int width=Math.Max(1,(int)Math.Round(bounds.Width)),height=Math.Max(1,(int)Math.Round(bounds.Height));if(width>32767||height>32767||(long)width*height>64000000)throw new InvalidOperationException("选中图像过大，请缩小后贴图。");using(var view=PinViews.Render(Source(item.Resource),item.Turns,false,false)){var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(view,new Rectangle(Point.Empty,image.Size));}return image;}}
        public void Save(string path)
        {
            string full=Path.GetFullPath(path),temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var file=FilePaths.Create(temp))using(var archive=new ZipArchive(file,ZipArchiveMode.Create)){using(var writer=new StreamWriter(archive.CreateEntry("board.json").Open()))writer.Write(Signature());foreach(var resource in items.Select(delegate(BoardItem item){return item.Resource;}).Distinct())using(var stream=archive.CreateEntry(resource+".png",CompressionLevel.NoCompression).Open())Source(resource).Save(stream,ImageFormat.Png);}FilePaths.Commit(temp,full,true);MarkClean();}finally{if(FilePaths.Exists(temp))FilePaths.Delete(temp);}}
        public static BoardDocument Open(string path)
        {
            var doc=new BoardDocument();try{using(var file=FilePaths.Read(path))using(var archive=new ZipArchive(file,ZipArchiveMode.Read)){
                if(archive.Entries.Count<1||archive.Entries.Count>101||archive.Entries.Sum(delegate(ZipArchiveEntry entry){return entry.Length;})>256L*1024*1024)throw new InvalidDataException("画板资源过多或过大。");var json=archive.GetEntry("board.json");if(json==null||json.Length>1024*1024)throw new InvalidDataException("画板索引缺失或过大。");BoardDto dto;using(var reader=new StreamReader(json.Open()))dto=new JavaScriptSerializer().Deserialize<BoardDto>(reader.ReadToEnd());if(dto==null||dto.Version!=1||dto.Items==null||dto.Items.Length>100)throw new InvalidDataException("画板版本或结构无效。");
                var ids=new HashSet<string>();foreach(var item in dto.Items){Guid id;if(item==null||!Guid.TryParseExact(item.Id,"N",out id)||!Guid.TryParseExact(item.Resource,"N",out id)||!ids.Add(item.Id))throw new InvalidDataException("画板 ID 无效。");if(!doc.resources.ContainsKey(item.Resource)){var imageEntry=archive.GetEntry(item.Resource+".png");if(imageEntry==null||imageEntry.Length>200L*1024*1024)throw new InvalidDataException("画板图片缺失或过大。");using(var stream=imageEntry.Open())using(var raw=Image.FromStream(stream,true,true)){if((long)raw.Width*raw.Height+doc.pixels>64000000)throw new InvalidDataException("画板图片像素超过上限。");doc.resources.Add(item.Resource,new Bitmap(raw));doc.pixels+=(long)raw.Width*raw.Height;}}doc.Validate(item);doc.items.Add(item.Clone());}
                if(archive.Entries.Count!=doc.resources.Count+1)throw new InvalidDataException("画板含多余或重复资源。");doc.Background=Color.FromArgb(dto.Background);doc.MarkClean();return doc;
            }}catch{doc.Dispose();throw;}
        }
        public void Dispose(){if(disposed)return;disposed=true;foreach(var source in resources.Values)source.Dispose();resources.Clear();}
        public sealed class BoardDto{public int Version,Background;public BoardItem[] Items;}
        sealed class BoardSnapshot{internal BoardItem[] Items;internal Color Background;}
    }
    internal static class BoardSnap
    {
        internal static PointF Move(RectangleF candidate,RectangleF[] others,float tolerance,out float? guideX,out float? guideY)
        {
            float bestX=tolerance+1,bestY=tolerance+1,dx=0,dy=0;guideX=guideY=null;
            foreach(var other in others){foreach(float from in new[]{candidate.Left,candidate.Right,candidate.Left+candidate.Width/2})foreach(float to in new[]{other.Left,other.Right,other.Left+other.Width/2}){float difference=to-from;if(Math.Abs(difference)<bestX){bestX=Math.Abs(difference);dx=difference;if(bestX<=tolerance)guideX=to;}}foreach(float from in new[]{candidate.Top,candidate.Bottom,candidate.Top+candidate.Height/2})foreach(float to in new[]{other.Top,other.Bottom,other.Top+other.Height/2}){float difference=to-from;if(Math.Abs(difference)<bestY){bestY=Math.Abs(difference);dy=difference;if(bestY<=tolerance)guideY=to;}}}
            return new PointF(candidate.X+(bestX<=tolerance?dx:0),candidate.Y+(bestY<=tolerance?dy:0));
        }
    }
}
