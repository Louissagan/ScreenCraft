using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenCraft
{
    // Correlates consecutive frames in both directions. Only confidently matched
    // downward motion is appended; a rejected frame never becomes the reference.
    public sealed class Stitcher : IDisposable
    {
        public enum Result { First,Same,Added,Lost,Full,Up }
        sealed class Strip {internal int[] Pixels;internal int Rows;}
        readonly object gate=new object();
        readonly List<Strip> strips=new List<Strip>();
        readonly int width,frameHeight,matchWidth,limit;
        int[] reference;
        int height;
        bool disposed,completed;
        public Stitcher(int width,int height,int ignoreRight,int maxRows)
        {
            if(width<1||height<1||height>30000||(long)width*height>40000000)throw new ArgumentOutOfRangeException("width");
            this.width=width;frameHeight=height;matchWidth=Math.Max(1,width-Math.Max(0,ignoreRight));limit=Math.Max(height,Math.Min(Math.Min(30000,maxRows),40000000/width));
        }
        public int Width{get{return width;}}
        public int Height{get{lock(gate)return height;}}
        public bool IsFull{get{lock(gate)return height>=limit;}}
        static int Difference(int a,int b){return Math.Abs(((a>>16)&255)-((b>>16)&255))+Math.Abs(((a>>8)&255)-((b>>8)&255))+Math.Abs((a&255)-(b&255));}
        double RowError(int[] current,int row)
        {
            double total=0;int samples=0;for(int x=0;x<matchWidth;x+=Math.Max(1,matchWidth/48)){total+=Difference(reference[row*width+x],current[row*width+x]);samples++;}return total/(samples*3);
        }
        double Score(int[] current,int shift,int top,int bottom)
        {
            int start=top+Math.Max(0,-shift),end=frameHeight-bottom-Math.Max(0,shift),overlap=end-start;
            if(overlap<Math.Min(24,(frameHeight-top-bottom)/3))return double.MaxValue;
            double sum=0;int count=0;int rows=Math.Min(48,overlap),columns=Math.Min(32,matchWidth);
            for(int r=0;r<rows;r++){int y=start+(int)((long)r*overlap/rows);for(int c=0;c<columns;c++){int x=(int)((long)(c*2+1)*matchWidth/(columns*2));sum+=Difference(current[y*width+x],reference[(y+shift)*width+x]);count++;}}
            return sum/Math.Max(1,count*3);
        }
        void Append(int[] source,int row,int rows)
        {
            if(rows<=0)return;var data=new int[checked(rows*width)];Array.Copy(source,row*width,data,0,data.Length);strips.Add(new Strip{Pixels=data,Rows=rows});height+=rows;
        }
        void Trim(int rows)
        {
            while(rows>0){var strip=strips[strips.Count-1];int removed=Math.Min(rows,strip.Rows);strip.Rows-=removed;height-=removed;rows-=removed;if(strip.Rows==0)strips.RemoveAt(strips.Count-1);}
        }
        public int[] Add(int[] frame,out Result result,out int added)
        {
            lock(gate){
                if(disposed||completed)throw new InvalidOperationException("拼接已结束。");if(frame==null||frame.Length!=checked(width*frameHeight))throw new ArgumentException("采集帧尺寸不一致。","frame");added=0;
                if(reference==null){Append(frame,0,frameHeight);reference=(int[])frame.Clone();added=frameHeight;result=Result.First;return frame;}
                if(height>=limit){result=Result.Full;return frame;}
                int top=0,bottom=0;while(top<frameHeight&&RowError(frame,top)<.25)top++;
                if(top==frameHeight){result=Result.Same;return frame;}
                while(bottom<frameHeight-top&&RowError(frame,frameHeight-bottom-1)<.25)bottom++;
                int range=frameHeight-top-bottom-Math.Min(24,Math.Max(4,(frameHeight-top-bottom)/3));
                double best=double.MaxValue,runnerUp=double.MaxValue;int displacement=0;
                for(int shift=-range;shift<=range;shift++){double score=Score(frame,shift,top,bottom);if(score<best){runnerUp=best;best=score;displacement=shift;}else if(score<runnerUp)runnerUp=score;}
                // Independent frames and animation usually have no distinct correlation peak.
                if(best>42 || runnerUp-best<.5 || (best>3&&runnerUp-best<2)){result=Result.Lost;return frame;}
                if(displacement<0){result=Result.Up;return frame;}if(displacement==0){result=Result.Same;return frame;}
                added=Math.Min(displacement,limit-height);Trim(bottom);Append(frame,frameHeight-bottom-displacement,added);Append(frame,frameHeight-bottom,bottom);
                Array.Copy(frame,reference,frame.Length);result=height>=limit?Result.Full:Result.Added;return frame;
            }
        }
        public int[] Thumb(int maxWidth,int maxHeight,out int targetWidth,out int targetHeight)
        {
            lock(gate){if(disposed)throw new ObjectDisposedException("Stitcher");double ratio=Math.Min(1,Math.Min(Math.Max(1,maxWidth)/(double)width,Math.Max(1,maxHeight)/(double)Math.Max(1,height)));targetWidth=Math.Max(1,(int)Math.Round(width*ratio));targetHeight=Math.Max(1,(int)Math.Round(height*ratio));var pixels=new int[targetWidth*targetHeight];int offset=0,index=0;
                for(int y=0;y<targetHeight;y++){int sourceY=Math.Min(height-1,(int)(y/ratio));while(index<strips.Count&&sourceY>=offset+strips[index].Rows){offset+=strips[index].Rows;index++;}if(index==strips.Count)break;for(int x=0;x<targetWidth;x++)pixels[y*targetWidth+x]=strips[index].Pixels[(sourceY-offset)*width+Math.Min(width-1,(int)(x/ratio))];}return pixels;}
        }
        public Bitmap Compose()
        {
            lock(gate){if(disposed||height==0)throw new InvalidOperationException("没有可导出的拼接图像。");var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);
                try{var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{int y=0;foreach(var strip in strips)for(int row=0;row<strip.Rows;row++,y++)Marshal.Copy(strip.Pixels,row*width,IntPtr.Add(data.Scan0,y*data.Stride),width);}finally{image.UnlockBits(data);}completed=true;return image;}catch{image.Dispose();throw;}}
        }
        public void Dispose(){lock(gate){if(disposed)return;disposed=true;strips.Clear();reference=null;}}
    }
}
