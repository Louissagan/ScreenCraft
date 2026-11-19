using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using Windows.Storage.Streams;
using Size=System.Drawing.Size;
using Point=System.Drawing.Point;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenCraft
{
    public static partial class Ocr
    {
        public const int MaxSide=10000,Band=1200,Overlap=160;
        const int Padding=20;
        public static string Unavailable {get{return "Windows 本地 OCR 不可用，请在 Windows 语言选项中检查已安装的 OCR 功能。";}}
        public static double Scale(int width){return Math.Min(2,(MaxSide-Padding*2.0)/Math.Max(1,width));}
        public static List<Rectangle> Pieces(Size size)
        {
            if(size.Width<1||size.Height<1)throw new ArgumentException("OCR 图像尺寸无效。","size");
            var result=new List<Rectangle>();int top=0;
            do{int height=Math.Min(Band,size.Height-top);result.Add(new Rectangle(0,top,size.Width,height));if(top+height==size.Height)break;top+=Band-Overlap;}while(true);return result;
        }
        public static Bitmap Prepare(Bitmap image){return Prepare(image,new Rectangle(Point.Empty,image.Size),Scale(image.Width));}
        public static Bitmap Prepare(Bitmap image,Rectangle part,double scale)
        {
            if(scale<=0||double.IsNaN(scale)||double.IsInfinity(scale))throw new ArgumentOutOfRangeException("scale");
            if(part.Width<1||part.Height<1||!new Rectangle(Point.Empty,image.Size).Contains(part))throw new ArgumentException("OCR 区域超出图像。");
            int width=checked((int)Math.Round(part.Width*scale)),height=checked((int)Math.Round(part.Height*scale));
            if(width+Padding*2>MaxSide||height+Padding*2>MaxSide)throw new ArgumentException("OCR 区域超过引擎尺寸限制。");
            var output=new Bitmap(Math.Max(1,width)+Padding*2,Math.Max(1,height)+Padding*2,PixelFormat.Format32bppArgb);
            try{using(var g=Graphics.FromImage(output)){
                g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(image,new Rectangle(Padding,Padding,Math.Max(1,width),Math.Max(1,height)),part.X,part.Y,part.Width,part.Height,GraphicsUnit.Pixel,attributes);}
            }return output;}catch{output.Dispose();throw;}
        }
        sealed class RecognizedLine { public string Text;public RectangleF Bounds; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Recognize(Bitmap image,out string error)
        {
            error=null;CheckCancellation();var engine=ConfiguredEngine();string best=RecognizePass(image,engine);int score=TextScore(best);
            if(score<8)foreach(var turn in new[]{RotateFlipType.Rotate90FlipNone,RotateFlipType.Rotate270FlipNone,RotateFlipType.Rotate180FlipNone}){
                CheckCancellation();using(var rotated=new Bitmap(image)){rotated.RotateFlip(turn);string candidate=RecognizePass(rotated,engine);int candidateScore=TextScore(candidate);if(candidateScore>=12&&candidateScore>score+4){best=candidate;score=candidateScore;}}
            }
            return best;
        }
        static int TextScore(string text){int score=0;foreach(char character in text)if(char.IsLetterOrDigit(character))score+=IsCjk(character)?2:1;return score;}
        static string RecognizePass(Bitmap image,OcrEngine engine)
        {
            var accepted=new List<RecognizedLine>();var bands=Pieces(image.Size);double scale=Scale(image.Width);
            for(int band=0;band<bands.Count;band++){
                CheckCancellation();ReportProgress(band,bands.Count);var part=bands[band];
                using(var prepared=Prepare(image,part,scale)){
                    var result=RecognizePrepared(engine,prepared);
                    foreach(var line in result.Lines){
                        if(line.Words.Count==0)continue;RectangleF extent=RectangleF.Empty;
                        foreach(var word in line.Words){var box=word.BoundingRect;var local=new RectangleF((float)((box.X-Padding)/scale),(float)((box.Y-Padding)/scale+part.Y),(float)(box.Width/scale),(float)(box.Height/scale));extent=extent.IsEmpty?local:RectangleF.Union(extent,local);}
                        var candidate=new RecognizedLine{Text=NormalizeCjkSpacing(line.Text),Bounds=extent};int duplicate=-1;
                        for(int i=accepted.Count-1;i>=0;i--){var prior=accepted[i];if(prior.Bounds.Bottom<part.Top)break;var overlap=RectangleF.Intersect(prior.Bounds,extent);
                            if(overlap.Height>Math.Min(prior.Bounds.Height,extent.Height)*.5f&&overlap.Width>Math.Min(prior.Bounds.Width,extent.Width)*.5f){duplicate=i;break;}}
                        if(duplicate>=0){if(candidate.Text.Length>accepted[duplicate].Text.Length)accepted[duplicate]=candidate;}
                        else accepted.Add(candidate);
                    }
                }
            }
            accepted.Sort(delegate(RecognizedLine a,RecognizedLine b){int y=a.Bounds.Top.CompareTo(b.Bounds.Top);return y!=0?y:a.Bounds.Left.CompareTo(b.Bounds.Left);});
            var text=new StringBuilder();foreach(var line in accepted){if(text.Length>0)text.Append('\n');text.Append(line.Text);}ReportProgress(bands.Count,bands.Count);return text.ToString();
        }
        static OcrResult RecognizePrepared(OcrEngine engine,Bitmap prepared)
        {
            var image=new SoftwareBitmap(BitmapPixelFormat.Bgra8,prepared.Width,prepared.Height,BitmapAlphaMode.Premultiplied);
            IAsyncOperation<OcrResult> operation=null;bool retained=false;
            try{
                byte[] pixels=new byte[checked(prepared.Width*prepared.Height*4)];var data=prepared.LockBits(new Rectangle(Point.Empty,prepared.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                try{for(int y=0;y<prepared.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*prepared.Width*4,prepared.Width*4);}finally{prepared.UnlockBits(data);}
                using(var writer=new DataWriter()){writer.WriteBytes(pixels);image.CopyFromBuffer(writer.DetachBuffer());}
                operation=engine.RecognizeAsync(image);
                try{while(operation.Status==AsyncStatus.Started){CheckCancellation();System.Threading.Thread.Sleep(20);}CheckCancellation();return operation.GetResults();}
                catch{if(operation.Status==AsyncStatus.Started){operation.Cancel();retained=true;var unfinished=operation;var input=image;
                    System.Threading.ThreadPool.QueueUserWorkItem(delegate{try{while(unfinished.Status==AsyncStatus.Started)System.Threading.Thread.Sleep(25);}catch(Exception){}finally{try{input.Dispose();unfinished.Close();}catch(Exception){}}});
                }throw;}
            }
            finally{
                // Native cancellation is asynchronous. Keep its input alive until the native task settles.
                if(!retained){image.Dispose();if(operation!=null)operation.Close();}
            }
        }
    }
}
