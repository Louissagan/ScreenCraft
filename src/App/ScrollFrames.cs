using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace ScreenCraft
{
    public interface IScrollFrameSource:IDisposable {void Read(int[] destination);}
    // Allocated only by an explicit user capture. Tests supply synthetic frames through the same interface.
    internal sealed class GdiScrollFrameSource:IScrollFrameSource
    {
        readonly Rectangle area;IntPtr screen,mem,dib,old,bits;bool disposed;
        public GdiScrollFrameSource(Rectangle bounds)
        {
            area=bounds;
            try{
                if(bounds.Width<1||bounds.Height<1||(long)bounds.Width*bounds.Height>40000000)throw new ArgumentException("滚动区域过大或无效。");
                screen=Native.GetDC(IntPtr.Zero);if(screen==IntPtr.Zero)throw new InvalidOperationException("无法访问屏幕。");
                mem=Native.CreateCompatibleDC(screen);if(mem==IntPtr.Zero)throw new InvalidOperationException("无法创建采集上下文。");
                var bi=new Native.BITMAPINFOHEADER{biSize=Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER)),biWidth=bounds.Width,biHeight=-bounds.Height,biPlanes=1,biBitCount=32};
                dib=Native.CreateDIBSection(screen,ref bi,0,out bits,IntPtr.Zero,0);if(dib==IntPtr.Zero||bits==IntPtr.Zero)throw new InvalidOperationException("无法分配采集缓冲区。");
                old=Native.SelectObject(mem,dib);if(old==IntPtr.Zero||old==new IntPtr(-1))throw new InvalidOperationException("无法选择采集位图。");
            }catch{Dispose();throw;}
        }
        public void Read(int[] destination)
        {
            if(disposed)throw new ObjectDisposedException("GdiScrollFrameSource");if(destination==null||destination.Length!=area.Width*area.Height)throw new ArgumentException("帧尺寸不匹配。");
            if(!Native.BitBlt(mem,0,0,area.Width,area.Height,screen,area.X,area.Y,Native.SRCCOPY))throw new InvalidOperationException("屏幕采集失败，已保留此前内容。");
            Native.GdiFlush();Marshal.Copy(bits,destination,0,destination.Length);
        }
        public void Dispose(){if(disposed)return;disposed=true;if(mem!=IntPtr.Zero&&old!=IntPtr.Zero&&old!=new IntPtr(-1))Native.SelectObject(mem,old);if(dib!=IntPtr.Zero)Native.DeleteObject(dib);if(mem!=IntPtr.Zero)Native.DeleteDC(mem);if(screen!=IntPtr.Zero)Native.ReleaseDC(IntPtr.Zero,screen);}
    }
    // Shared by the production collector and synthetic tests; contains no screen access or input polling.
    public sealed class ScrollFrameProcessor:IDisposable
    {
        public readonly Stitcher Stitcher;bool finished;
        public const string LimitMessage="已达到 30000 行或 4000 万像素上限。本图仅包含已采集部分；可分段导出 PNG，剩余页面请另起一次截图。";
        public ScrollFrameProcessor(int width,int height,int ignoreRight,int maxRows){Stitcher=new Stitcher(width,height,ignoreRight,maxRows);}
        public void Push(ref int[] buffer,out Stitcher.Result state,out int added){if(finished)throw new InvalidOperationException("采集已结束。");int[] free=Stitcher.Add(buffer,out state,out added);buffer=free??new int[buffer.Length];}
        public void Read(IScrollFrameSource source,int[] buffer){if(finished)throw new InvalidOperationException("采集已结束。");source.Read(buffer);}
        public Bitmap Complete(bool cancelled,string error,out string message)
        {
            if(finished)throw new InvalidOperationException("采集已结束。");finished=true;message=null;
            if(cancelled)return null;
            if(Stitcher.Height==0){message=error==null?"没有采集到画面。":"采集失败："+error;return null;}
            Bitmap image=Stitcher.Compose();
            if(error!=null)message="采集中途失败，仅保留已采集部分："+error;
            if(Stitcher.IsFull)message=(message==null?"":message+"\n")+LimitMessage;
            return image;
        }
        public void Dispose(){finished=true;Stitcher.Dispose();}
    }
    public static class SegmentedPng
    {
        public static string[] Export(Bitmap image,string parent,int rowsPerPart)
        {
            if(image==null)throw new ArgumentNullException("image");if(rowsPerPart<1||rowsPerPart>30000)throw new ArgumentOutOfRangeException("rowsPerPart");
            string directory=Path.Combine(Path.GetFullPath(parent),"ScreenCraft-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
            Directory.CreateDirectory(directory);var paths=new List<string>();
            try{for(int y=0;y<image.Height;y+=rowsPerPart){string path=Path.Combine(directory,"part-"+(paths.Count+1).ToString("D3")+".png");using(var part=image.Clone(new Rectangle(0,y,image.Width,Math.Min(rowsPerPart,image.Height-y)),PixelFormat.Format32bppArgb))ImageFiles.SavePng(part,path);paths.Add(path);}return paths.ToArray();}
            catch{foreach(string path in Directory.GetFiles(directory))File.Delete(path);Directory.Delete(directory);throw;}
        }
    }
}
