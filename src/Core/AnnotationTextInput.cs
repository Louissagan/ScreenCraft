using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScreenCraft
{
    internal sealed class AnnotationTextInput : TextBox
    {
        const int ImeStartComposition=0x010D, ImeEndComposition=0x010E, ImeComposition=0x010F;
        const int CompositionString=0x0008, ResultString=0x0800, EditGetLineCount=0x00BA;
        int compositionStart,compositionSelectionLength;
        bool layoutPending;
        bool useSceneBackdrop;
        IntPtr backdropBrush;
        Rectangle backdropArea;
        int backdropRevision;

        internal event Action CompositionChanged;
        internal string Composition {get;private set;}
        internal bool CompletingComposition {get{return layoutPending&&Composition==null;}}
        internal bool UseSceneBackdrop
        {
            get{return useSceneBackdrop;}
            set{
                if(useSceneBackdrop==value)return;
                useSceneBackdrop=value;
                DiscardBackdrop();Invalidate();
            }
        }
        internal int VisibleLineCount {get{return Math.Max(1,Native.SendMessage(Handle,EditGetLineCount,IntPtr.Zero,IntPtr.Zero).ToInt32());}}
        internal string LayoutText
        {
            get{
                string text=Text;
                if(Composition==null)return text;
                int start=Math.Min(compositionStart,text.Length);
                int length=Math.Min(compositionSelectionLength,text.Length-start);
                return text.Substring(0,start)+Composition+text.Substring(start+length);
            }
        }

        // Native EDIT still owns the caret, selection and IME. Its background is sampled
        // from the same scene painter as the surrounding canvas.
        internal IntPtr PaintOverScene(IntPtr dc,Rectangle area,int revision,Func<Rectangle,Bitmap> paint)
        {
            if(area.Width<1||area.Height<1)return IntPtr.Zero;
            if(backdropBrush==IntPtr.Zero||area!=backdropArea||revision!=backdropRevision){
                DiscardBackdrop();
                using(var image=paint(area)){
                    IntPtr bitmap=image.GetHbitmap();
                    try{backdropBrush=CreatePatternBrush(bitmap);}
                    finally{Native.DeleteObject(bitmap);}
                }
                backdropArea=area;backdropRevision=revision;
            }
            if(backdropBrush!=IntPtr.Zero){
                SetBkMode(dc,1);
                SetTextColor(dc,ColorTranslator.ToWin32(ForeColor));
                SetBrushOrgEx(dc,area.X-Left,area.Y-Top,IntPtr.Zero);
            }
            return backdropBrush;
        }

        void DiscardBackdrop()
        {
            if(backdropBrush==IntPtr.Zero)return;
            Native.DeleteObject(backdropBrush);backdropBrush=IntPtr.Zero;
        }

        internal void RefreshBackdrop()
        {
            if(!useSceneBackdrop)return;
            DiscardBackdrop();Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)DiscardBackdrop();
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message message)
        {
            long flags=message.LParam.ToInt64();
            bool compositionMessage=message.Msg==ImeComposition;
            bool hasComposition=compositionMessage&&(flags&CompositionString)!=0;
            if(message.Msg==ImeStartComposition||hasComposition&&(flags&ResultString)==0)
                UpdateComposition();

            bool ended=Composition!=null&&(message.Msg==ImeEndComposition||compositionMessage&&(flags==0||(flags&ResultString)!=0));
            if(ended)Composition=null;
            if(message.Msg==ImeEndComposition||ended&&!hasComposition)RefreshAfterComposition();

            base.WndProc(ref message);

            if(!IsDisposed&&ended&&hasComposition)UpdateComposition();
        }

        // The native edit control can still report the composition's line count while confirming a candidate.
        void RefreshAfterComposition()
        {
            if(layoutPending||!IsHandleCreated)return;
            layoutPending=true;
            BeginInvoke((Action)delegate{
                layoutPending=false;
                if(!IsDisposed)NotifyCompositionChanged();
            });
        }

        void UpdateComposition()
        {
            if(Composition==null){compositionStart=SelectionStart;compositionSelectionLength=SelectionLength;}
            string current=ReadComposition();
            if(Composition==current)return;
            Composition=current;
            NotifyCompositionChanged();
        }

        void NotifyCompositionChanged()
        {
            var handler=CompositionChanged;
            if(handler!=null)handler();
        }

        string ReadComposition()
        {
            IntPtr context=ImmGetContext(Handle);
            if(context==IntPtr.Zero)return "";
            try{
                int length=ImmGetCompositionString(context,CompositionString,null,0);
                if(length<=0)return "";
                var data=new byte[length];
                int read=ImmGetCompositionString(context,CompositionString,data,data.Length);
                return read>0&&read<=data.Length?Encoding.Unicode.GetString(data,0,read):"";
            }finally{ImmReleaseContext(Handle,context);}
        }

        [DllImport("imm32.dll")] static extern IntPtr ImmGetContext(IntPtr window);
        [DllImport("imm32.dll")] static extern bool ImmReleaseContext(IntPtr window,IntPtr context);
        [DllImport("imm32.dll",EntryPoint="ImmGetCompositionStringW")] static extern int ImmGetCompositionString(IntPtr context,int type,[Out] byte[] buffer,int length);
        [DllImport("gdi32.dll")] static extern IntPtr CreatePatternBrush(IntPtr bitmap);
        [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc,int mode);
        [DllImport("gdi32.dll")] static extern int SetTextColor(IntPtr dc,int color);
        [DllImport("gdi32.dll")] static extern bool SetBrushOrgEx(IntPtr dc,int x,int y,IntPtr oldOrigin);
    }
}
