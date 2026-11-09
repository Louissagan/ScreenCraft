using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace ScreenCraft
{
    // Reuse each native popup until its anchor is disposed; never dispose it inside a Closed event.
    internal sealed class ColorPopup:IDisposable
    {
        readonly Control anchor;float scale;Action<Color> choose;Action sample;
        internal StyledMenu Menu{get;private set;}internal AnnotationPalette Palette{get;private set;}
        internal ColorPopup(Control owner){anchor=owner;anchor.Disposed+=OwnerDisposed;}
        void OwnerDisposed(object sender,EventArgs e){Dispose();}
        internal StyledMenu Prepare(Color current,Action<Color> selected,Action pick=null)
        {
            choose=selected;sample=pick;float next=CaptureHost.ScaleFor(Screen.FromControl(anchor));
            if(Menu==null||Math.Abs(next-scale)>.01f){if(Menu!=null)Menu.Dispose();scale=next;
                Menu=new StyledMenu{MinimumContentWidth=0,ShowImageMargin=false};
                Palette=new AnnotationPalette(current,delegate(Color color){if(choose!=null)choose(color);Menu.Close();},null,scale);
                Menu.Items.Add(new ToolStripControlHost(Palette){AutoSize=false,Size=Palette.Size,Margin=Padding.Empty,Padding=Padding.Empty});
                Palette.ExpandedChanged+=delegate{Menu.Items[0].Size=Palette.Size;Menu.PrepareLayout();if(Menu.Visible)Place();};
                Menu.Closed+=delegate{Palette.SetCurrent(Palette.Current);};
            }
            Palette.Sample=sample==null?(Action)null:delegate{Menu.Close();sample();};Palette.SetCurrent(current);Palette.SetExpanded(false);Menu.PrepareLayout();return Menu;
        }
        internal void Show(){Menu.Show(anchor,Point.Empty);Place();}
        void Place(){var bottom=anchor.PointToScreen(new Point(0,anchor.Height));var top=anchor.PointToScreen(Point.Empty);Menu.Location=Placement(top,bottom,Menu.Size,Screen.FromPoint(top).WorkingArea);}
        internal static Point Placement(Point top,Point bottom,Size size,Rectangle work)
        {
            int x=bottom.X,y=bottom.Y+4;if(y+size.Height>work.Bottom)y=top.Y-size.Height-4;
            return new Point(Math.Max(work.Left,Math.Min(work.Right-size.Width,x)),Math.Max(work.Top,Math.Min(work.Bottom-size.Height,y)));
        }
        public void Dispose(){anchor.Disposed-=OwnerDisposed;if(Menu!=null){Menu.Dispose();Menu=null;}}
    }
    internal static class CraftColors
    {
        static readonly ConditionalWeakTable<Control,ColorPopup> popups=new ConditionalWeakTable<Control,ColorPopup>();
        internal static void Show(Control anchor,Color current,Action<Color> choose,Action sample=null)
        {
            var popup=popups.GetValue(anchor,delegate(Control owner){return new ColorPopup(owner);});popup.Prepare(current,choose,sample);popup.Show();
        }
    }
}
