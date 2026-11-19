using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class HistoryWindow:Form
    {
        readonly Label emptyHint=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.White,ForeColor=PreferencesLook.Muted};
        readonly bool historyEnabled;readonly CaptureHistory store;readonly ListBox list=new ListBox{BorderStyle=BorderStyle.None,IntegralHeight=false,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=54,AccessibleName="截图历史，按时间倒序"};
        readonly PictureBox preview=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};readonly Label note=new Label{ForeColor=PreferencesLook.Muted};readonly FlowLayoutPanel actions=new FlowLayoutPanel{Height=42};readonly PreferencesButton copy=new PreferencesButton{Text="复制"},pin=new PreferencesButton{Text="贴图"},edit=new PreferencesButton{Text="编辑"},delete=new PreferencesButton{Text="删除"},clear=new PreferencesButton{Text="清理历史"};
        internal Action<Bitmap> CopyImage=CaptureHost.CopyImage;internal Action<Bitmap> PinImage=delegate(Bitmap image){PinWindow.Open(image);};
        internal HistoryWindow(string dataRoot,bool enabled)
        {
            historyEnabled=enabled;store=new CaptureHistory(dataRoot);Text="ScreenCraft · 截图历史";Icon=CaptureHost.AppIcon;Font=new Font("Microsoft YaHei UI",9);ClientSize=new Size(860,620);MinimumSize=new Size(700,480);BackColor=PreferencesLook.Background;StartPosition=FormStartPosition.CenterParent;
            list.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0||e.Index>=list.Items.Count)return;var item=(HistoryItem)list.Items[e.Index];using(var fill=new SolidBrush((e.State&DrawItemState.Selected)!=0?CaptureLook.Selected:Color.White))e.Graphics.FillRectangle(fill,e.Bounds);DateTime time;string label=DateTime.TryParse(item.TimeUtc,out time)?time.ToLocalTime().ToString("yyyy-MM-dd  HH:mm:ss"):item.TimeUtc;PreferencesLook.Text(e.Graphics,label,Font,new Rectangle(e.Bounds.X+12,e.Bounds.Y+5,e.Bounds.Width-24,24),PreferencesLook.Ink,StringAlignment.Near);PreferencesLook.Text(e.Graphics,item.Width+" × "+item.Height,Font,new Rectangle(e.Bounds.X+12,e.Bounds.Y+29,e.Bounds.Width-24,20),PreferencesLook.Muted,StringAlignment.Near);};
            list.SelectedIndexChanged+=delegate{ShowItem();};actions.Controls.AddRange(new Control[]{copy,pin,edit,delete,clear});foreach(Control control in actions.Controls)control.Width=100;
            copy.Click+=delegate{Act(delegate{using(var image=LoadSelected())CopyImage(image);});};pin.Click+=delegate{Act(delegate{var image=LoadSelected();try{PinImage(image);}catch{image.Dispose();throw;}});};edit.Click+=delegate{Act(delegate{new CraftEditor(LoadSelected()).Show();});};delete.Click+=delegate{Act(delegate{var item=list.SelectedItem as HistoryItem;if(item!=null)store.Delete(item.Id);Reload();});};clear.Click+=delegate{if(MessageBox.Show(this,"只删除软件自有历史副本，手动保存的图片保留。继续？","清理历史",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)==DialogResult.OK)Act(delegate{store.Clear();Reload();});};
            preview.Controls.Add(emptyHint);Controls.AddRange(new Control[]{list,preview,note,actions});Reload();PerformLayout();
        }
        void Act(Action action){try{action();}catch(Exception ex){note.Text="操作失败："+ex.Message;}}
        Bitmap LoadSelected(){var item=list.SelectedItem as HistoryItem;if(item==null)throw new InvalidOperationException("请先选择一张历史图片。");return CaptureHost.LoadFull(store.ImagePath(item.Id));}
        void ShowItem(){var old=preview.Image;preview.Image=null;if(old!=null)old.Dispose();bool selected=list.SelectedItem!=null;copy.Enabled=pin.Enabled=edit.Enabled=delete.Enabled=selected;if(selected)Act(delegate{preview.Image=LoadSelected();});}
        internal void Reload(){Act(delegate{list.Items.Clear();foreach(var item in store.List())list.Items.Add(item);clear.Enabled=list.Items.Count>0;emptyHint.Visible=!clear.Enabled;
                if(clear.Enabled){
                    note.Text=historyEnabled?"清理历史只会移除这里的截图副本。":"历史记录已暂停，仍可查看已有截图。";
                    list.SelectedIndex=0;
                }else{
                    ShowItem();
                    note.Text="";emptyHint.Text=historyEnabled?"还没有历史图片\n下次完成截图后会显示在这里。":"历史记录已关闭\n在设置的截图页开启后，新截图会显示在这里。";
                }});}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(list==null)return;list.Bounds=new Rectangle(24,24,245,Math.Max(100,ClientSize.Height-112));preview.Bounds=new Rectangle(285,24,Math.Max(100,ClientSize.Width-309),Math.Max(100,ClientSize.Height-112));note.Bounds=new Rectangle(24,ClientSize.Height-78,ClientSize.Width-48,28);actions.Bounds=new Rectangle(24,ClientSize.Height-44,ClientSize.Width-48,36);}
        protected override void Dispose(bool disposing){if(disposing&&preview.Image!=null){preview.Image.Dispose();preview.Image=null;}base.Dispose(disposing);}
    }
}
