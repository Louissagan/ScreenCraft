using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed class OcrResultWindow:Form
    {
        readonly Bitmap source;
        readonly LocalOcrService service;
        readonly TextBox result=new TextBox{Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Font=new Font("Microsoft YaHei UI",12),AccessibleName="OCR 识别结果"};
        readonly ComboBox language=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=160,AccessibleName="OCR 语言"};
        readonly Label status=new Label{Dock=DockStyle.Bottom,Height=55,Padding=new Padding(8),AccessibleName="OCR 状态"};
        readonly ProgressBar progress=new ProgressBar{Dock=DockStyle.Bottom,Height=6};
        readonly PreferencesButton copy=new PreferencesButton{Text="复制文字",Primary=true,Enabled=false},retry=new PreferencesButton{Text="识别／重试"},cancel=new PreferencesButton{Text="取消识别",Enabled=false};
        CancellationTokenSource job;int generation;bool closed;
        public OcrResultWindow(Bitmap ownedSource,string preferredLanguage):this(ownedSource,preferredLanguage,new LocalOcrService()){}
        internal OcrResultWindow(Bitmap ownedSource,string preferredLanguage,LocalOcrService backend)
        {
            source=ownedSource;service=backend;Icon=ShotStack.AppIcon;Text="ScreenCraft · 本地文字识别";ClientSize=new Size(760,520);MinimumSize=new Size(500,360);StartPosition=FormStartPosition.CenterParent;Font=new Font("Microsoft YaHei UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            AutoScaleDimensions=new SizeF(96,96);BackColor=PreferencesLook.Background;Padding=new Padding(12);
            status.ForeColor=PreferencesLook.Muted;result.BorderStyle=BorderStyle.None;
            language.Format+=delegate(object sender,ListControlConvertEventArgs e){if(Equals(e.ListItem,"auto"))e.Value="自动选择";};
            language.FormattingEnabled=true;
            var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(0,0,0,8)};
            tools.Controls.Add(new Label{Text="识别语言",AutoSize=true,Padding=new Padding(0,6,0,0)});tools.Controls.Add(language);tools.Controls.Add(retry);tools.Controls.Add(cancel);tools.Controls.Add(copy);
            language.Items.Add("auto");language.SelectedIndex=0;status.Text="仅使用 Windows 本地 OCR；不会上传图片或文字。";
            var paper=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14)};paper.Controls.Add(result);Controls.Add(paper);Controls.Add(status);Controls.Add(progress);Controls.Add(tools);
            retry.Click+=async delegate{await RunAsync();};cancel.Click+=delegate{if(job!=null)job.Cancel();};copy.Click+=delegate{try{Clipboard.SetText(result.Text);}catch(System.Runtime.InteropServices.ExternalException){status.Text="剪贴板正被其他程序使用，请稍后再复制。";}};
            result.TextChanged+=delegate{copy.Enabled=!string.IsNullOrEmpty(result.Text);};
            Shown+=async delegate{
                try{var caps=await service.CapabilitiesAsync();if(closed)return;if(!caps.Available){language.Enabled=false;retry.Enabled=false;status.Text=caps.Error;return;}foreach(string tag in caps.Languages)language.Items.Add(tag);if(!string.IsNullOrEmpty(preferredLanguage)&&!preferredLanguage.Equals("auto",StringComparison.OrdinalIgnoreCase)){string selected=Ocr.SelectLanguage(preferredLanguage,caps.Languages);if(selected==null){status.Text="先前的 OCR 语言在本机不可用，请选择已安装语言后点击识别。";return;}language.SelectedItem=selected;}await RunAsync();}
                catch(Exception ex){if(!closed)status.Text="无法读取本地 OCR："+ex.Message;}
            };
        }
        async Task RunAsync()
        {
            if(closed)return;if(job!=null)job.Cancel();var current=new CancellationTokenSource();job=current;int version=++generation;
            cancel.Enabled=true;retry.Enabled=false;language.Enabled=false;status.Text="正在本机识别，可随时取消……";progress.Style=ProgressBarStyle.Marquee;
            try{
                var outcome=await service.RecognizeAsync(source,Convert.ToString(language.SelectedItem),current.Token,TimeSpan.FromSeconds(30),delegate(int done,int total){
                    if(closed||!IsHandleCreated)return;try{BeginInvoke((Action)delegate{if(closed||version!=generation)return;progress.Style=ProgressBarStyle.Continuous;progress.Value=Math.Max(0,Math.Min(100,done*100/Math.Max(1,total)));});}catch(InvalidOperationException){}
                });
                if(closed||version!=generation)return;
                if(outcome.Success){result.Text=outcome.Text;status.Text=string.IsNullOrWhiteSpace(outcome.Text)?"未检测到文字。可更换语言或缩小区域后重试。":"本地识别完成 · "+outcome.Language+" · 可选择、编辑或复制文字。";progress.Style=ProgressBarStyle.Continuous;progress.Value=100;}
                else status.Text=outcome.Cancelled?"已取消识别。可点击重试。":outcome.Error;
            }
            catch(Exception ex){if(!closed&&version==generation)status.Text="识别失败，请缩小图片后重试："+ex.Message;}
            finally{current.Dispose();if(!closed&&version==generation){job=null;cancel.Enabled=false;retry.Enabled=true;language.Enabled=true;progress.Style=ProgressBarStyle.Continuous;}}
        }
        protected override void Dispose(bool disposing){if(disposing&&!closed){closed=true;generation++;if(job!=null)job.Cancel();source.Dispose();}base.Dispose(disposing);}
    }
}
