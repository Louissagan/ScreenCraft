// Evaluation only: calls the unmodified upstream assembly's public APIs.
// This is an integration probe, not proof of the upstream hotkey/toolbar workflow.
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Stackshot;
class StackshotBaselineProbe : Form
{
    readonly string output;
    readonly TextBox log = new TextBox { Multiline=true, Dock=DockStyle.Fill, ReadOnly=true };
    StackshotBaselineProbe(string path)
    {
        output=path; Text="Stackshot M0 integration probe"; Width=650; Height=400;
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Top,Height=50};
        var capture=new Button{Text="Capture area",AutoSize=true};
        var pins=new Button{Text="Two independent pins",AutoSize=true};
        var editor=new Button{Text="Actual editor",AutoSize=true};
        capture.Click+=delegate {
            Bitmap frozen; Rectangle at; IntPtr window;
            var r=RegionPicker.Pick(RegionPicker.Mode.Image,out frozen,out at,out window);
            using(frozen) {
                if(!r.IsEmpty) {
                    frozen.Save(Path.Combine(output,"stackshot-probe-region.png"));
                    Clipboard.SetImage(frozen); Say("Region captured and copied: "+r);
                } else Say("Region cancelled");
            }
        };
        pins.Click+=delegate {
            for(int i=0;i<2;i++) {
                var bmp=Grabber.Grab(new Rectangle(Left+20+i*70,Top+30,240,140));
                PinWindow.Open(bmp);
            }
            int count=0;
            foreach(Form f in Application.OpenForms) if(f is PinWindow) {
                f.Left=30+count*300; f.Top=100+count*220; count++;
                Say("Pin HWND="+f.Handle+" TopMost="+f.TopMost+" Bounds="+f.Bounds);
            }
            Say("Independent pin count="+count);
        };
        editor.Click+=delegate {
            var bmp=Grabber.Grab(new Rectangle(Left,Top,400,250));
            var ed=new Editor(null,null,bmp); ed.ShowInTaskbar=true; ed.Show();
            Say("Upstream Editor created HWND="+ed.Handle);
        };
        buttons.Controls.AddRange(new Control[]{capture,pins,editor});
        Controls.Add(log); Controls.Add(buttons);
    }
    void Say(string s) { log.AppendText(s+Environment.NewLine); File.AppendAllText(Path.Combine(output,"stackshot-probe.log"),DateTime.Now.ToString("O")+" "+s+Environment.NewLine); }
    [STAThread] static void Main(string[] args) {
        Environment.SetEnvironmentVariable("STACKSHOT_TEST_DATA",Path.Combine(args[0],"stackshot-probe-data"));
        try {
            File.AppendAllText(Path.Combine(args[0],"stackshot-probe.log"),"Probe starting\n");
            Application.EnableVisualStyles(); Theme.Init(); I18n.Apply("en");
            Settings.ReadOnly=true; ShotStack.Test=true;
            var form=new StackshotBaselineProbe(args[0]);
            form.Shown+=delegate { form.WindowState=FormWindowState.Normal; form.Activate(); };
            Application.Run(form);
        } catch(Exception ex) { File.AppendAllText(Path.Combine(args[0],"stackshot-probe-error.txt"),ex.ToString()); }
    }
}
