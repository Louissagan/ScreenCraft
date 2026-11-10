using System;
using System.Drawing;
using System.Windows.Forms;
namespace ScreenCraft
{
    public sealed partial class CaptureOverlay
    {
        internal void CopyAndContinue(){if(!canvas.CompletePolygon())return;canvas.CommitText();using(var image=canvas.Render())CaptureCompletion.Deliver(image,completionPreferences,OutputRoot,CopyOutput,false);tips.SetToolTip(swatch,"已复制，继续标注；Enter 完成并退出。");}
        internal StyledMenu PrepareMoreMenu()
        {
            if(moreMenu!=null)return moreMenu;var menu=new StyledMenu();foreach(string id in toolbarSettings.Order){if(Array.IndexOf(toolbarSettings.Hidden,id)<0)continue;string tool=id;AddMenu(menu,ToolbarSettings.Name(id)+" "+toolbarSettings.Keys[id],delegate{SelectToolbarTool(tool);});}
            AddMenu(menu,"工具栏显示与排序…",ConfigureToolbar);
            AddMenu(menu,"前移一层  Ctrl+]",delegate{canvas.ReorderSelected(1);});AddMenu(menu,"后移一层  Ctrl+[",delegate{canvas.ReorderSelected(-1);});
            AddMenu(menu,"置于顶层  Ctrl+Shift+]",delegate{canvas.ReorderSelected(1,true);});AddMenu(menu,"置于底层  Ctrl+Shift+[",delegate{canvas.ReorderSelected(-1,true);});
            AddMenu(menu,"复制并继续标注 Ctrl+Shift+Enter",CopyAndContinue);AddMenu(menu,"加入新画板",delegate{if(!canvas.CompletePolygon())return;canvas.CommitText();pendingBoard=canvas.Render();Completed=true;Close();});
            AddMenu(menu,"保存可编辑工程…",delegate{using(var d=new SaveFileDialog{Filter="ScreenCraft 可编辑工程|*.sccraft",FileName="标注工程.sccraft"})if(d.ShowDialog(this)==DialogResult.OK)using(var doc=canvas.ExportDocument())doc.Save(d.FileName);});AddMenu(menu,"分段导出 PNG…",delegate{using(var d=new FolderBrowserDialog{Description="分段导出当前截图"})if(d.ShowDialog(this)==DialogResult.OK)using(var image=canvas.Render())SegmentedPng.Export(image,d.SelectedPath,4000);});AddMenu(menu,"滚动长截图",delegate{ScrollRequested=true;Close();});moreMenu=menu;return menu;
        }
        static string MenuGlyph(string text){if(text.StartsWith("前移")||text.StartsWith("置于顶"))return "front";if(text.StartsWith("后移")||text.StartsWith("置于底"))return "back";if(text.StartsWith("加入新画板"))return "board";if(text.StartsWith("图片美化"))return "image";if(text.StartsWith("文字水印"))return "watermark";if(text.StartsWith("聚光灯"))return "spotlight";if(text.StartsWith("复制"))return "copy";if(text.StartsWith("裁剪"))return "crop";if(text.StartsWith("保存可编辑"))return "project";if(text.StartsWith("分段"))return "save";if(text.StartsWith("滚动"))return "scroll";foreach(var id in ToolbarSettings.Known)if(text.StartsWith(ToolbarSettings.Name(id)))return ToolbarSettings.Glyph(id);return "more";}
    }
}
