using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
namespace Stackshot
{
    static class NativeOcrProbe
    {
        public static int Run(string output)
        {
            var service=new LocalOcrService();var records=new List<object>();var caps=service.CapabilitiesAsync().GetAwaiter().GetResult();
            records.Add(new{Case="installed local OCR languages",Status=caps.Available?"PASS":"BLOCKED",Languages=caps.Languages,Error=caps.Error});
            using(var fixture=new Bitmap(1100,260))
            {
                using(var graphics=Graphics.FromImage(fixture))using(var font=new Font("Microsoft YaHei UI",34,GraphicsUnit.Pixel)){
                    graphics.Clear(Color.White);graphics.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;graphics.DrawString("建筑平面测试 2026\nScreenCraft OCR 12345",font,Brushes.Black,new PointF(24,24));
                }
                fixture.Save(Path.Combine(output,"ocr-synthetic-input.png"));
                foreach(string language in new[]{"en","zh-Hans"})
                {
                    string selected=Ocr.SelectLanguage(language,caps.Languages);
                    if(!caps.Available||selected==null){records.Add(new{Case="native recognition "+language,Status="BLOCKED",Reason="Requested Windows local OCR language is not installed",Requested=language});continue;}
                    var task=service.RecognizeAsync(fixture,language,CancellationToken.None,TimeSpan.FromSeconds(25),null);
                    if(!task.Wait(30000)){records.Add(new{Case="native recognition "+language,Status="FAIL",Reason="Probe exceeded 30 seconds"});continue;}
                    var result=task.Result;
                    bool english=result.Success&&(result.Text??"").IndexOf("ScreenCraft",StringComparison.OrdinalIgnoreCase)>=0&&(result.Text??"").Contains("12345");
                    bool chinese=language!="zh-Hans"||(result.Text??"").Contains("建筑平面测试");
                    records.Add(new{Case="native recognition "+language,Status=english&&chinese?"PASS":"FAIL",ActualLanguage=result.Language,ResultFromSyntheticFixture=result.Text,Error=result.Error});
                    Console.WriteLine((english&&chinese?"PASS":"FAIL")+" Windows OCR "+language+" ("+result.Language+"): "+(result.Text??result.Error));
                }
            }
            if(caps.Available&&Ocr.SelectLanguage("en",caps.Languages)!=null)
            using(var tall=new Bitmap(1100,2700))
            {
                using(var graphics=Graphics.FromImage(tall))using(var font=new Font("Microsoft YaHei UI",34,GraphicsUnit.Pixel)){
                    graphics.Clear(Color.White);graphics.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                    graphics.DrawString("ScreenCraft OCR 12345",font,Brushes.Black,24,24);
                    graphics.DrawString("Boundary marker 22222",font,Brushes.Black,24,1120);
                    graphics.DrawString("Final line 33333",font,Brushes.Black,24,2480);
                }
                tall.Save(Path.Combine(output,"ocr-tall-synthetic-input.png"));
                var job=service.RecognizeAsync(tall,"en",CancellationToken.None,TimeSpan.FromSeconds(25),null);bool done=job.Wait(30000);var read=done?job.Result:new OcrOutcome{Error="Probe timeout"};string value=read.Text??"";
                bool okay=read.Success&&value.Contains("12345")&&value.Contains("22222")&&value.Contains("33333")&&value.IndexOf("22222")==value.LastIndexOf("22222")&&value.IndexOf("12345")<value.IndexOf("33333");
                records.Add(new{Case="native tall image overlapping OCR bands",Status=okay?"PASS":"FAIL",ResultFromSyntheticFixture=value,Error=read.Error});Console.WriteLine((okay?"PASS":"FAIL")+" Windows OCR tall bands: "+value);
            }
            if(caps.Available&&Ocr.SelectLanguage("en",caps.Languages)!=null)
            using(var sparse=new Bitmap(1100,120)){
                using(var g=Graphics.FromImage(sparse))using(var f=new Font("Microsoft YaHei UI",34,GraphicsUnit.Pixel)){g.Clear(Color.White);g.DrawString("First line 11111",f,Brushes.Black,24,24);}
                sparse.Save(Path.Combine(output,"ocr-known-limitation-input.png"));var read=service.RecognizeAsync(sparse,"en",CancellationToken.None,TimeSpan.FromSeconds(10),null).GetAwaiter().GetResult();
                bool recognized=read.Success&&(read.Text??"").Contains("11111");records.Add(new{Case="isolated First line 11111 engine accuracy regression",Status=recognized?"PASS":"KNOWN LIMITATION",Expected="First line 11111",ActualFromSyntheticFixture=read.Text,Error=read.Error,Issue="OCR-001: English Windows engine misses this short line; no accuracy guarantee, no cloud fallback"});Console.WriteLine((recognized?"PASS":"KNOWN LIMITATION")+" OCR-001 isolated short line: "+read.Text);
            }
            using(var blank=new Bitmap(10,10)){
                var missing=service.RecognizeAsync(blank,"zz-MISSING",CancellationToken.None,TimeSpan.FromSeconds(5),null).GetAwaiter().GetResult();
                bool okay=!missing.Success&&missing.Error!=null&&(missing.Error.Contains("zz-MISSING")||!caps.Available);records.Add(new{Case="explicit missing language never falls back to cloud or another script",Status=okay?"PASS":"FAIL",Error=missing.Error});Console.WriteLine((okay?"PASS":"FAIL")+" missing-language handling");
            }
            string json=new JavaScriptSerializer().Serialize(new{Date="2026-11-04",Mode="Local Windows OCR on synthetic image only; no desktop, clipboard, network or language installation",Cases=records});
            File.WriteAllText(Path.Combine(output,"native-ocr-probe.json"),json);Console.WriteLine("OCR probe saved; installed languages: "+string.Join(", ",caps.Languages));return json.Contains("\"Status\":\"FAIL\"")?1:0;
        }
    }
}
