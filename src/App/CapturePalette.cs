using System;
using System.Drawing;
using System.Windows.Forms;
namespace Stackshot
{
    public sealed partial class CaptureOverlay
    {
        StyledMenu colorMenu,moreMenu;AnnotationPalette palette;ColorPopup colorPopup;
        Color PaletteColor{get{return canvas.ActiveColor;}}
        internal StyledMenu PrepareColorMenu()
        {
            if(colorPopup==null)colorPopup=new ColorPopup(swatch);
            colorMenu=colorPopup.Prepare(PaletteColor,delegate(Color color){ExecuteCommand(delegate{ApplyAnnotationColor(color);});},delegate{canvas.BeginColorPick(ApplySampledColor);canvas.Focus();});
            palette=colorPopup.Palette;return colorMenu;
        }
        void ShowColors(){PrepareColorMenu();colorPopup.Show();}
        void DisposePopups(){if(colorPopup!=null){colorPopup.Dispose();colorPopup=null;colorMenu=null;}if(moreMenu!=null){moreMenu.Dispose();moreMenu=null;}}
    }
}
