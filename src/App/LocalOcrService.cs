using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.Ocr;
namespace ScreenCraft
{
    public sealed class OcrCapabilities
    {
        public string[] Languages=new string[0];public string Error;public int MaxDimension;
        public bool Available{get{return Error==null&&Languages.Length>0;}}
    }
    public sealed class OcrOutcome
    {
        public string Text,Language,Error;public bool Cancelled,TimedOut;
        public bool Success{get{return Error==null&&!Cancelled&&!TimedOut;}}
    }
    public interface ILocalOcrBackend
    {
        OcrCapabilities Capabilities();
        OcrOutcome Read(Bitmap image,string language,CancellationToken token,TimeSpan timeout,Action<int,int> progress);
    }
    public sealed class WindowsOcrBackend:ILocalOcrBackend
    {
        public OcrCapabilities Capabilities(){try{return Ocr.Discover();}catch(Exception ex){return new OcrCapabilities{Error=Ocr.Unavailable+"\n"+ex.GetType().Name};}}
        public OcrOutcome Read(Bitmap image,string language,CancellationToken token,TimeSpan timeout,Action<int,int> progress){return Ocr.ReadLocal(image,language,token,timeout,progress);}
    }
    public sealed class LocalOcrService
    {
        readonly ILocalOcrBackend backend;
        public LocalOcrService():this(new WindowsOcrBackend()){}
        public LocalOcrService(ILocalOcrBackend implementation){if(implementation==null)throw new ArgumentNullException("implementation");backend=implementation;}
        public Task<OcrCapabilities> CapabilitiesAsync(){return Worker(delegate{return backend.Capabilities();});}
        public Task<OcrOutcome> RecognizeAsync(Bitmap image,string language,CancellationToken token,TimeSpan timeout,Action<int,int> progress)
        {
            if(image==null)throw new ArgumentNullException("image");if((long)image.Width*image.Height>64000000)throw new ArgumentException("OCR 图片超过 6400 万像素。");
            if(timeout<=TimeSpan.Zero||timeout>TimeSpan.FromMinutes(5))throw new ArgumentOutOfRangeException("timeout");
            var owned=new Bitmap(image);
            return Worker(delegate{using(owned){try{token.ThrowIfCancellationRequested();return backend.Read(owned,language,token,timeout,progress);}catch(OperationCanceledException){return new OcrOutcome{Cancelled=true};}catch(Exception ex){return new OcrOutcome{Error="识别失败："+ex.Message};}}});
        }
        static Task<T> Worker<T>(Func<T> action)
        {
            var source=new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread=new Thread(delegate(){try{source.SetResult(action());}catch(Exception ex){source.SetException(ex);}}){IsBackground=true,Priority=ThreadPriority.BelowNormal};
            thread.SetApartmentState(ApartmentState.MTA);thread.Start();return source.Task;
        }
    }
    public static partial class Ocr
    {
        sealed class Execution {public string Requested,Actual;public CancellationToken Token;public long Deadline;public Action<int,int> Progress;}
        [ThreadStatic] static Execution execution;
        [MethodImpl(MethodImplOptions.NoInlining)] internal static OcrCapabilities Discover()
        {
            var languages=new List<string>();foreach(var l in OcrEngine.AvailableRecognizerLanguages)languages.Add(l.LanguageTag);
            return new OcrCapabilities{Languages=languages.ToArray(),MaxDimension=(int)OcrEngine.MaxImageDimension,Error=languages.Count==0?Unavailable:null};
        }
        public static string SelectLanguage(string requested,IList<string> installed)
        {
            if(installed==null||installed.Count==0)return null;
            if(!string.IsNullOrEmpty(requested)&&!string.Equals(requested,"auto",StringComparison.OrdinalIgnoreCase))
            {
                foreach(string tag in installed)if(string.Equals(tag,requested,StringComparison.OrdinalIgnoreCase))return tag;
                string family=requested.StartsWith("zh",StringComparison.OrdinalIgnoreCase)?"zh":requested.Split('-')[0];
                foreach(string tag in installed)if(tag.Equals(family,StringComparison.OrdinalIgnoreCase)||tag.StartsWith(family+"-",StringComparison.OrdinalIgnoreCase)){
                    if(requested.StartsWith("zh",StringComparison.OrdinalIgnoreCase)&&IsSimplified(requested)!=IsSimplified(tag))continue;return tag;
                }
                return null;
            }
            foreach(string tag in installed)if(IsSimplified(tag))return tag;
            foreach(string tag in installed)if(tag.Equals("en",StringComparison.OrdinalIgnoreCase)||tag.StartsWith("en-",StringComparison.OrdinalIgnoreCase))return tag;
            return installed[0];
        }
        static bool IsSimplified(string tag){return tag.Equals("zh",StringComparison.OrdinalIgnoreCase)||tag.StartsWith("zh-Hans",StringComparison.OrdinalIgnoreCase)||tag.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)||tag.Equals("zh-SG",StringComparison.OrdinalIgnoreCase);}
        static OcrEngine ConfiguredEngine()
        {
            CheckCancellation();var caps=Discover();string requested=execution==null?"auto":execution.Requested;string selected=SelectLanguage(requested,caps.Languages);
            if(selected==null){if(!caps.Available)throw new InvalidOperationException(caps.Error);throw new InvalidOperationException("缺少所选 OCR 语言包："+requested+"。已安装："+string.Join(", ",caps.Languages)+"。请选择已安装语言，或由你自行安装 Windows OCR 语言功能。");}
            var engine=OcrEngine.TryCreateFromLanguage(new Language(selected));if(engine==null)throw new InvalidOperationException(Unavailable);
            if(execution!=null)execution.Actual=engine.RecognizerLanguage.LanguageTag;return engine;
        }
        internal static OcrOutcome ReadLocal(Bitmap image,string requested,CancellationToken token,TimeSpan timeout,Action<int,int> progress)
        {
            var prior=execution;execution=new Execution{Requested=requested,Token=token,Deadline=Stopwatch.GetTimestamp()+(long)(timeout.TotalSeconds*Stopwatch.Frequency),Progress=progress};
            try{CheckCancellation();string error;string text=Recognize(image,out error);CheckCancellation();ReportProgress(1,1);return new OcrOutcome{Text=text??"",Error=error,Language=execution.Actual};}
            catch(OperationCanceledException){return new OcrOutcome{Cancelled=true};}
            catch(TimeoutException){return new OcrOutcome{TimedOut=true,Error="识别超时，可缩小图片区域后重试。"};}
            catch(Exception ex){return new OcrOutcome{Error=ex is TypeLoadException||ex is System.IO.FileNotFoundException?Unavailable:"本地识别失败："+ex.Message};}
            finally{execution=prior;}
        }
        static void CheckCancellation(){if(execution==null)return;execution.Token.ThrowIfCancellationRequested();if(Stopwatch.GetTimestamp()>=execution.Deadline)throw new TimeoutException();}
        static void ReportProgress(int completed,int total){if(execution!=null&&execution.Progress!=null)execution.Progress(completed,total);}
        internal static bool IsCjk(char c){return (c>=0x3400&&c<=0x9fff)||(c>=0xf900&&c<=0xfaff);}
        public static string NormalizeCjkSpacing(string text)
        {
            if(string.IsNullOrEmpty(text))return text??"";var result=new System.Text.StringBuilder();
            for(int i=0;i<text.Length;i++){if(text[i]==' '&&result.Length>0&&IsCjk(result[result.Length-1])){int j=i;while(j<text.Length&&text[j]==' ')j++;if(j<text.Length&&IsCjk(text[j])){i=j-1;continue;}}result.Append(text[i]);}return result.ToString();
        }
    }
}
