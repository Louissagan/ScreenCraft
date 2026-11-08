using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed partial class CaptureOverlay
    {
        readonly List<Control> configurableTools=new List<Control>();
        void BuildConfigurableTools()
        {
            int index=0;foreach(string id in toolbarSettings.Order){if(Array.IndexOf(toolbarSettings.Hidden,id)>=0)continue;string command=id;
                var button=Add(tools,ToolbarSettings.Glyph(id),ToolbarSettings.Name(id)+(toolbarSettings.Keys[id]==""?"":" "+toolbarSettings.Keys[id]),delegate{SelectToolbarTool(command);});
                configurableTools.Add(button);tools.Controls.SetChildIndex(button,index++);Tool kind;if(Enum.TryParse<Tool>(id,out kind))toolButtons[kind]=button;
            }
        }
        void SelectToolbarTool(string id)
        {
            if(id=="Background"){using(var dialog=new BackgroundDialog(canvas))dialog.ShowDialog(this);return;}
            if(id=="Watermark"){using(var dialog=new WatermarkDialog(canvas))dialog.ShowDialog(this);return;}
            canvas.SetTool(id=="Callout"?annotationTool:(Tool)Enum.Parse(typeof(Tool),id));canvas.Focus();
        }
        void ConfigureToolbar()
        {
            using(var dialog=new ToolbarSettingsDialog(toolbarSettings))if(dialog.ShowDialog(this)==DialogResult.OK){
                if(ShotStack.Running!=null&&ShotStack.Running.ToolbarChanged!=null)ShotStack.Running.ToolbarChanged(dialog.Result.Clone());
                ApplyToolbarConfiguration(dialog.Result);
            }
        }
        internal void ApplyToolbarConfiguration(ToolbarSettings configuration)
        {
            var next=ToolbarSettings.Safe(configuration);toolbarSettings.Order=next.Order;toolbarSettings.Hidden=next.Hidden;toolbarSettings.Keys=next.Keys;completionPreferences.Toolbar=next.Clone();
            tools.SuspendLayout();foreach(var control in configurableTools){stripItems.RemoveAll(item=>item.Control==control);tools.Controls.Remove(control);control.Dispose();}configurableTools.Clear();toolButtons.Clear();
            BuildConfigurableTools();foreach(var control in configurableTools)stripItems.Add(new StripItem{Control=control,Size=control.Size,Margin=control.Margin});tools.ResumeLayout();
            // A menu being clicked must finish its native callback before it is released.
            var stale=moreMenu;moreMenu=null;if(stale!=null){stale.Close();if(IsHandleCreated)BeginInvoke((Action)delegate{stale.Dispose();});else stale.Dispose();}
            SetChromeScale(chromeScale);RefreshState();
        }
    }
}
