using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class PinManagerWindow:Form
    {
        readonly Label emptyHint=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.White,ForeColor=PreferencesLook.Muted,Text="还没有贴图\n截图后按 P，或从托盘粘贴图片。"};
        readonly ListBox list=new ListBox{BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=60,IntegralHeight=false,AccessibleName="独立贴图列表"};readonly PictureBox preview=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};readonly Label note=new Label{ForeColor=PreferencesLook.Muted};readonly FlowLayoutPanel actions=new FlowLayoutPanel{WrapContents=true};readonly ToolTip tips=new ToolTip();readonly Timer refresh=new Timer{Interval=1000};string fingerprint="";
        internal Action<PinWindow> RestorePin=delegate(PinWindow pin){pin.RestoreInteraction();};
        internal PinManagerWindow()
        {
            Text="ScreenCraft · 贴图管理";Icon=CaptureHost.AppIcon;Font=new Font("Microsoft YaHei UI",9);ClientSize=new Size(840,580);MinimumSize=new Size(700,480);BackColor=PreferencesLook.Background;StartPosition=FormStartPosition.CenterParent;
            list.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0||e.Index>=list.Items.Count)return;var pin=(PinWindow)list.Items[e.Index];using(var fill=new SolidBrush((e.State&DrawItemState.Selected)!=0?CaptureLook.Selected:Color.White))e.Graphics.FillRectangle(fill,e.Bounds);PreferencesLook.Text(e.Graphics,"贴图 "+pin.PinId.Substring(0,6),Font,new Rectangle(12,e.Bounds.Top+4,list.Width-24,25),PreferencesLook.Ink,StringAlignment.Near);PreferencesLook.Text(e.Graphics,(pin.State.HiddenByUser?"已隐藏 · ":pin.State.ClickThrough?"鼠标穿透 · ":"")+(int)Math.Round(pin.State.Zoom*100)+"% · 透明度 "+pin.State.OpacityPercent+"%",Font,new Rectangle(12,e.Bounds.Top+29,list.Width-24,24),PreferencesLook.Muted,StringAlignment.Near);};
            list.SelectedIndexChanged+=delegate{var old=preview.Image;preview.Image=null;if(old!=null)old.Dispose();var selected=list.SelectedItem as PinWindow;if(selected!=null&&!selected.IsDisposed)preview.Image=selected.PreviewImage(new Size(600,440));};
            Add("stack","恢复与定位",delegate(PinWindow pin){RestorePin(pin);});Add("hide","隐藏此图",delegate(PinWindow pin){pin.SetPinHidden(true);});Add("edit","重新编辑",delegate(PinWindow pin){pin.EditImage();});Add("rotate","旋转 90°",delegate(PinWindow pin){pin.ChangeView(1,false,false);});Add("flip","水平翻转",delegate(PinWindow pin){pin.ChangeView(0,true,false);});Add("cancel","关闭此图",delegate(PinWindow pin){pin.Close();});
            var all=new PreferencesButton{Text="恢复全部",Width=104};all.Click+=delegate{PinWindow.RestoreAllPins(RestorePin);Reload(true);};actions.Controls.Add(all);
            preview.Controls.Add(emptyHint);Controls.AddRange(new Control[]{list,preview,note,actions});note.Text="每张图都是独立窗口。这里可恢复隐藏或鼠标穿透的图片。";refresh.Tick+=delegate{Reload(false);};Shown+=delegate{refresh.Start();};Reload(true);PerformLayout();
        }
        void Add(string glyph,string label,Action<PinWindow> action){var button=new CaptureIconButton{Glyph=glyph,AccessibleName=label};tips.SetToolTip(button,label);button.Click+=delegate{try{var selected=list.SelectedItem as PinWindow;if(selected!=null&&!selected.IsDisposed)action(selected);Reload(true);}catch(Exception ex){note.Text=ex.Message;}};actions.Controls.Add(button);}
        internal void Reload(bool force){var pins=PinWindow.All;string next="";foreach(var pin in pins)next+=pin.PinId+pin.State.FlipY+pin.State.HiddenByUser+pin.State.ClickThrough+pin.State.Zoom+pin.State.OpacityPercent+pin.State.QuarterTurns+pin.State.FlipX+";";if(!force&&next==fingerprint)return;fingerprint=next;var selected=list.SelectedItem as PinWindow;list.Items.Clear();foreach(var pin in pins)list.Items.Add(pin);if(selected!=null&&list.Items.Contains(selected))list.SelectedItem=selected;else if(pins.Count>0)list.SelectedIndex=0;else{var old=preview.Image;preview.Image=null;if(old!=null)old.Dispose();note.Text="";}emptyHint.Visible=pins.Count==0;if(pins.Count>0)note.Text="可在这里定位贴图，或恢复隐藏、鼠标穿透的图片。";foreach(Control child in actions.Controls)child.Enabled=list.SelectedItem!=null;}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(list==null)return;list.Bounds=new Rectangle(24,24,250,Math.Max(100,ClientSize.Height-116));preview.Bounds=new Rectangle(290,24,Math.Max(100,ClientSize.Width-314),Math.Max(100,ClientSize.Height-116));note.Bounds=new Rectangle(24,ClientSize.Height-82,ClientSize.Width-48,28);actions.Bounds=new Rectangle(24,ClientSize.Height-48,ClientSize.Width-48,40);}
        protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();tips.Dispose();if(preview.Image!=null){preview.Image.Dispose();preview.Image=null;}}base.Dispose(disposing);}
    }
    public partial class PinWindow
    {
        internal Bitmap PreviewImage(Size maximum){float factor=Math.Min(1,Math.Min((float)maximum.Width/img.Width,(float)maximum.Height/img.Height));var image=new Bitmap(Math.Max(1,(int)(img.Width*factor)),Math.Max(1,(int)(img.Height*factor)));using(var g=Graphics.FromImage(image)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(img,new Rectangle(Point.Empty,image.Size));}return image;}
    }
}
