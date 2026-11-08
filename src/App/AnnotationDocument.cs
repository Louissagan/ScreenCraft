using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Web.Script.Serialization;
namespace Stackshot
{
    // Explicit editable files retain the original image. PNG export contains only flattened pixels.
    public sealed class AnnotationDocument : IDisposable
    {
        public readonly Bitmap Source;
        public readonly Shape[] Shapes;
        public readonly RectangleF Crop;
        public readonly bool BackgroundOn;public float OutputCornerRadius;
        public readonly Settings Background;public int PinQuarterTurns;public bool PinFlipX,PinFlipY;
        const long MaxPixels=64000000;
        const long MaxPngBytes=200L*1024*1024;
        const int MaxJsonBytes=16*1024*1024;
        public AnnotationDocument(Bitmap ownedSource,Shape[] shapes,RectangleF crop,bool backgroundOn,Settings background)
        {
            try
            {
                if(ownedSource==null || (long)ownedSource.Width*ownedSource.Height>MaxPixels)throw new InvalidDataException("图片超过 6400 万像素上限。");
                if(shapes==null || shapes.Length>5000)throw new InvalidDataException("标注数量超过上限。");
                if(!Finite(crop.X)||!Finite(crop.Y)||!Finite(crop.Width)||!Finite(crop.Height)||crop.Width<1||crop.Height<1||crop.X<0||crop.Y<0||crop.Right>ownedSource.Width+.01||crop.Bottom>ownedSource.Height+.01)throw new InvalidDataException("裁剪范围超出原图。");
                var ids=new HashSet<string>();
                foreach(var s in shapes){ValidateShape(s);if(!ids.Add(s.Id))throw new InvalidDataException("标注 ID 重复。");}
                Source=ownedSource; Shapes=new Shape[shapes.Length];for(int i=0;i<shapes.Length;i++)Shapes[i]=shapes[i].Clone();
                Crop=crop;BackgroundOn=backgroundOn;Background=(background??new Settings()).Clone();
            }
            catch {if(ownedSource!=null)ownedSource.Dispose();throw;}
        }
        public static AnnotationDocument FromImage(Bitmap ownedImage) {return new AnnotationDocument(ownedImage,new Shape[0],new RectangleF(0,0,ownedImage.Width,ownedImage.Height),false,new Settings());}
        public AnnotationDocument Clone() {return new AnnotationDocument(new Bitmap(Source),Shapes,Crop,BackgroundOn,Background){PinQuarterTurns=PinQuarterTurns,PinFlipX=PinFlipX,PinFlipY=PinFlipY,OutputCornerRadius=OutputCornerRadius};}
        public Bitmap Render() {using(var c=new Canvas(new Bitmap(Source))){c.LoadDocument(this);return c.Render();}}
        public void Dispose(){Source.Dispose();foreach(var s in Shapes)s.DropCache();}
        static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<=1000000;}
        static bool PointOk(PointF p){return Finite(p.X)&&Finite(p.Y);}
        public static void ValidateShape(Shape s)
        {
            if(s==null || string.IsNullOrEmpty(s.Id) || s.Id.Length>64 || !Enum.IsDefined(typeof(Tool),s.Kind) || s.Kind==Tool.Crop)throw new InvalidDataException("标注类型或 ID 无效。");
            if(!PointOk(s.A)||!PointOk(s.B)||!PointOk(s.C)||!PointOk(s.NoteOffset))throw new InvalidDataException("标注坐标无效。");
            if(!Finite(s.Width)||s.Width<.25||s.Width>128||!Finite(s.FontPx)||s.FontPx<8||s.FontPx>300||!Finite(s.CornerRadius)||s.CornerRadius<0||s.CornerRadius>1000)throw new InvalidDataException("线宽、圆角或字号超出范围。");
            if(s.Dash<DashStyle.Solid || s.Dash>DashStyle.DashDotDot || !Finite(s.NoteWidth)||s.NoteWidth<60||s.NoteWidth>800 || s.Number<0||s.Number>99999)throw new InvalidDataException("线型、序号或说明宽度无效。");
            if(!Finite(s.NoteHeight)||s.NoteHeight<0||s.NoteHeight>10000||!Finite(s.MarkerSize)||s.MarkerSize<0||s.MarkerSize>200||!Finite(s.EffectOpacity)||s.EffectOpacity<0||s.EffectOpacity>95)throw new InvalidDataException("文字框或标记大小无效。");
            if(s.Text!=null && s.Text.Length>20000)throw new InvalidDataException("文字过长。");
            if(s.Points!=null){if(s.Points.Count>50000)throw new InvalidDataException("画笔节点过多。");foreach(var p in s.Points)if(!PointOk(p))throw new InvalidDataException("画笔节点无效。");}
            if(s.RedactionMode<0||s.RedactionMode>2||!Finite(s.WatermarkAngle)||s.WatermarkAngle< -90||s.WatermarkAngle>90||!Finite(s.WatermarkSpacing)||s.WatermarkSpacing<20||s.WatermarkSpacing>600)throw new InvalidDataException("模糊或水印参数无效。");
            if(s.MosaicBlock<0||s.MosaicBlock>128)throw new InvalidDataException("马赛克块大小无效。");
            if(!Finite(s.NoteFontPx)||(s.NoteFontPx!=0&&(s.NoteFontPx<8||s.NoteFontPx>300)))throw new InvalidDataException("说明字号无效。");
        }
        public void Save(string path)
        {
            string full=Path.GetFullPath(path),temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                var dto=new ProjectDto{Version=1,OutputCornerRadius=OutputCornerRadius,Crop=new[]{Crop.X,Crop.Y,Crop.Width,Crop.Height},BackgroundOn=BackgroundOn,Background=Background,PinQuarterTurns=PinQuarterTurns,PinFlipX=PinFlipX,PinFlipY=PinFlipY};
                dto.Shapes=new ShapeDto[Shapes.Length];for(int i=0;i<Shapes.Length;i++)dto.Shapes[i]=ShapeDto.From(Shapes[i]);
                var serializer=new JavaScriptSerializer{MaxJsonLength=MaxJsonBytes};
                using(var file=FilePaths.Create(temp))
                using(var zip=new ZipArchive(file,ZipArchiveMode.Create))
                {
                    using(var image=zip.CreateEntry("source.png",CompressionLevel.NoCompression).Open())Source.Save(image,ImageFormat.Png);
                    using(var writer=new StreamWriter(zip.CreateEntry("document.json").Open()))writer.Write(serializer.Serialize(dto));
                }
                FilePaths.Commit(temp,full,true);
            }
            finally {if(FilePaths.Exists(temp))FilePaths.Delete(temp);}
        }
        public static AnnotationDocument Open(string path)
        {
            using(var file=FilePaths.Read(path))
            using(var zip=new ZipArchive(file,ZipArchiveMode.Read))
            {
                if(zip.Entries.Count!=2)throw new InvalidDataException("工程文件结构无效。");
                var png=zip.GetEntry("source.png");var json=zip.GetEntry("document.json");
                if(png==null||json==null||png.Length>MaxPngBytes||json.Length>MaxJsonBytes)throw new InvalidDataException("工程资源缺失或过大。");
                ProjectDto dto;
                using(var reader=new StreamReader(json.Open()))dto=new JavaScriptSerializer{MaxJsonLength=MaxJsonBytes}.Deserialize<ProjectDto>(reader.ReadToEnd());
                if(dto==null || !Finite(dto.OutputCornerRadius)||dto.OutputCornerRadius<0||dto.OutputCornerRadius>1000||dto.Version!=1 || dto.Crop==null || dto.Crop.Length!=4 || dto.Shapes==null || dto.Shapes.Length>5000||dto.PinQuarterTurns<0||dto.PinQuarterTurns>3)throw new InvalidDataException("不支持的工程版本或损坏的工程。");
                var shapes=new Shape[dto.Shapes.Length];for(int i=0;i<shapes.Length;i++)shapes[i]=dto.Shapes[i].ToShape();
                using(var stream=png.Open())using(var raw=Image.FromStream(stream,true,true))
                {
                    if((long)raw.Width*raw.Height>MaxPixels)throw new InvalidDataException("工程图片过大。");
                    return new AnnotationDocument(new Bitmap(raw),shapes,new RectangleF(dto.Crop[0],dto.Crop[1],dto.Crop[2],dto.Crop[3]),dto.BackgroundOn,dto.Background){PinQuarterTurns=dto.PinQuarterTurns,PinFlipX=dto.PinFlipX,PinFlipY=dto.PinFlipY,OutputCornerRadius=dto.OutputCornerRadius};
                }
            }
        }
        public sealed class ProjectDto {public float OutputCornerRadius;public int Version;public float[] Crop;public ShapeDto[] Shapes;public bool BackgroundOn;public Settings Background;public int PinQuarterTurns;public bool PinFlipX,PinFlipY;}
        public sealed class ShapeDto
        {
            public string Id,Text; public int Kind,Color,FillColor,Dash,Number,Weight,MosaicBlock;public float Width,FontPx,Radius,NoteWidth,NoteHeight,MarkerSize,EffectOpacity;
            public bool FillEnabled,SimpleArrow,Curved;public float[] A,B,C,NoteOffset,Points;
            public bool HasNoteTextColor;public int NoteTextColor;public float NoteFontPx;public bool NoteAutoWidth,TextWrap,LeaderCounter,SmoothPen,FreehandArrow,ArrowWhiteOutline;public int RedactionMode;public float WatermarkAngle,WatermarkSpacing;
            static float[] Pair(PointF p){return new[]{p.X,p.Y};}
            static PointF ReadPair(float[] p){if(p==null||p.Length!=2)throw new InvalidDataException("坐标损坏。");return new PointF(p[0],p[1]);}
            public static ShapeDto From(Shape s)
            {
                var dto=new ShapeDto{Id=s.Id,Text=s.Text,Kind=(int)s.Kind,Color=s.Color.ToArgb(),FillColor=s.FillColor.ToArgb(),Dash=(int)s.Dash,Number=s.Number,Weight=s.Weight,Width=s.Width,FontPx=s.FontPx,Radius=s.CornerRadius,NoteWidth=s.NoteWidth,FillEnabled=s.FillEnabled,SimpleArrow=s.SimpleArrow,Curved=s.Curved,A=Pair(s.A),B=Pair(s.B),C=Pair(s.C),NoteOffset=Pair(s.NoteOffset)};
                dto.MosaicBlock=s.MosaicBlock;dto.NoteHeight=s.NoteHeight;dto.MarkerSize=s.MarkerSize;dto.EffectOpacity=s.EffectOpacity;
                dto.HasNoteTextColor=s.HasNoteTextColor;dto.NoteTextColor=s.NoteTextColor.ToArgb();dto.NoteFontPx=s.NoteFontPx;dto.NoteAutoWidth=s.NoteAutoWidth;dto.TextWrap=s.TextWrap;dto.LeaderCounter=s.LeaderCounter;dto.SmoothPen=s.SmoothPen;dto.FreehandArrow=s.FreehandArrow;dto.ArrowWhiteOutline=s.ArrowWhiteOutline;dto.RedactionMode=s.RedactionMode;dto.WatermarkAngle=s.WatermarkAngle;dto.WatermarkSpacing=s.WatermarkSpacing;
                if(s.Points!=null){dto.Points=new float[s.Points.Count*2];for(int i=0;i<s.Points.Count;i++){dto.Points[i*2]=s.Points[i].X;dto.Points[i*2+1]=s.Points[i].Y;}}
                return dto;
            }
            public Shape ToShape()
            {
                var s=new Shape{Id=Id,Text=Text,Kind=(Tool)Kind,Color=System.Drawing.Color.FromArgb(Color),FillColor=System.Drawing.Color.FromArgb(FillColor),Dash=(DashStyle)Dash,Number=Number,Weight=Math.Max(0,Math.Min(2,Weight)),Width=Width,FontPx=FontPx,CornerRadius=Radius,NoteWidth=NoteWidth,FillEnabled=FillEnabled,SimpleArrow=SimpleArrow,Curved=Curved,A=ReadPair(A),B=ReadPair(B),C=ReadPair(C),NoteOffset=ReadPair(NoteOffset)};
                s.MosaicBlock=MosaicBlock;s.NoteHeight=NoteHeight;s.MarkerSize=MarkerSize;s.EffectOpacity=EffectOpacity;
                s.HasNoteTextColor=HasNoteTextColor;s.NoteTextColor=System.Drawing.Color.FromArgb(NoteTextColor);s.NoteFontPx=NoteFontPx;s.NoteAutoWidth=NoteAutoWidth;s.TextWrap=TextWrap;s.LeaderCounter=LeaderCounter;s.SmoothPen=SmoothPen;s.FreehandArrow=FreehandArrow;s.ArrowWhiteOutline=ArrowWhiteOutline;s.RedactionMode=RedactionMode;s.WatermarkAngle=WatermarkAngle;s.WatermarkSpacing=WatermarkSpacing==0?100:WatermarkSpacing;
                if(Points!=null){if(Points.Length%2!=0||Points.Length>100000)throw new InvalidDataException("画笔节点损坏。");s.Points=new List<PointF>();for(int i=0;i<Points.Length;i+=2)s.Points.Add(new PointF(Points[i],Points[i+1]));}
                ValidateShape(s);return s;
            }
        }
    }
}
