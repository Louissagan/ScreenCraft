using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    internal sealed class PreferencesChoice:PreferencesPaintButton
    {
        string[] choices;int selected;StyledMenu popup;bool disposingPopup;
        public event EventHandler SelectionChanged;
        internal PreferencesChoice(params string[] values){if(values==null||values.Length==0)throw new ArgumentException("需要选项");choices=(string[])values.Clone();Height=34;Width=270;BackColor=Color.White;AccessibleRole=AccessibleRole.ComboBox;AccessibleName="取色格式";}
        internal string SelectedItem{get{return choices[selected];}set{int index=Array.IndexOf(choices,value);SelectIndex(index<0?0:index);}}
        internal int SelectedIndex{get{return selected;}set{SelectIndex(Math.Max(0,Math.Min(choices.Length-1,value)));}}
        internal string[] Choices{get{return (string[])choices.Clone();}}
        internal void SetChoices(string[] values){if(values==null||values.Length==0)throw new ArgumentException("需要选项");if(popup!=null){disposingPopup=true;popup.Dispose();popup=null;disposingPopup=false;}choices=(string[])values.Clone();selected=0;Invalidate();}
        void SelectIndex(int value){if(selected==value){Invalidate();return;}selected=value;Invalidate();var handler=SelectionChanged;if(handler!=null)handler(this,EventArgs.Empty);}
        internal StyledMenu CreateOptions(){var menu=new StyledMenu{MinimumContentWidth=Math.Max(120,Width-12)};for(int i=0;i<choices.Length;i++){int index=i;var item=new ToolStripMenuItem(choices[i]){Checked=i==selected,AccessibleName=choices[i]};item.Click+=delegate{SelectIndex(index);};menu.Items.Add(item);}menu.PrepareLayout();return menu;}
        // Closed runs inside native menu click/focus cleanup. Keep the popup alive until its owner is disposed.
        internal StyledMenu PreparePopup(){if(popup==null||popup.IsDisposed){popup=CreateOptions();popup.Closed+=delegate{if(!disposingPopup&&!IsDisposed&&Visible)Focus();};}for(int i=0;i<popup.Items.Count;i++)((ToolStripMenuItem)popup.Items[i]).Checked=i==selected;return popup;}
        protected override void OnClick(EventArgs e){base.OnClick(e);if(popup!=null&&!popup.IsDisposed&&popup.Visible){popup.Close();return;}PreparePopup().Show(this,new Point(0,Height+4));}
        internal bool ApplyKey(Keys key){switch(key){case Keys.Down:SelectIndex((selected+1)%choices.Length);return true;case Keys.Up:SelectIndex((selected+choices.Length-1)%choices.Length);return true;case Keys.Home:SelectIndex(0);return true;case Keys.End:SelectIndex(choices.Length-1);return true;default:return false;}}
        protected override bool ProcessCmdKey(ref Message message,Keys keys){if(ApplyKey(keys))return true;if(keys==(Keys.Alt|Keys.Down)||keys==Keys.F4){OnClick(EventArgs.Empty);return true;}return base.ProcessCmdKey(ref message,keys);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;PreferencesLook.Fill(g,ClientRectangle,BackColor);PreferencesLook.Round(g,ClientRectangle,Hover?PreferencesLook.Input:Color.White,8,Focused?CaptureLook.Blue:PreferencesLook.Line);PreferencesLook.Text(g,SelectedItem,Font,new Rectangle(12,0,Math.Max(1,Width-44),Height),Enabled?PreferencesLook.Ink:PreferencesLook.Muted,StringAlignment.Near);using(var pen=new Pen(PreferencesLook.Muted,1.5f)){int x=Width-19,y=Height/2;g.DrawLines(pen,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}}
        protected override void Dispose(bool disposing){if(disposing){disposingPopup=true;if(popup!=null){var old=popup;popup=null;old.Dispose();}}base.Dispose(disposing);}
    }
}
