using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenCraft
{
    public enum FillPatternKind { Diagonal, Crosshatch, Dots, Horizontal, Vertical }

    public struct FillPatternSettings
    {
        public FillPatternKind Kind;
        public int Spacing, StrokePercent;
        internal int EffectiveSpacing { get { return Spacing==0?11:Spacing; } }
        internal int EffectiveStrokePercent { get { return StrokePercent==0?50:StrokePercent; } }
        internal bool IsValid
        {
            get { return Enum.IsDefined(typeof(FillPatternKind),Kind)
                &&(Spacing==0||Spacing>=5&&Spacing<=64)&&(StrokePercent==0||StrokePercent>=25&&StrokePercent<=75); }
        }
        internal void Apply(Shape shape)
        {
            shape.FillPattern=Kind;shape.FillSpacing=Spacing;shape.FillStrokePercent=StrokePercent;
        }
    }

    public partial class Shape
    {
        internal FillPatternKind FillPattern;
        internal int FillSpacing,FillStrokePercent;
        internal FillPatternSettings PatternSettings
        {
            get { return new FillPatternSettings{Kind=FillPattern,Spacing=FillSpacing,StrokePercent=FillStrokePercent}; }
        }
    }

    public static partial class Painter
    {
        static void DrawPattern(Graphics g,Shape shape,GraphicsPath clip,Color color)
        {
            if(color.A==0||clip.PointCount==0)return;
            var bounds=clip.GetBounds();
            var visible=RectangleF.Intersect(g.VisibleClipBounds,bounds);
            if(visible.Width<=0||visible.Height<=0)return;
            var settings=shape.PatternSettings;
            float spacing=settings.EffectiveSpacing;
            float width=settings.StrokePercent==0?Math.Max(1.25f,Math.Min(3,shape.Width*.5f)):Math.Max(.5f,shape.Width*settings.EffectiveStrokePercent/100f);
            var state=g.Save();
            try{
                g.SetClip(clip,CombineMode.Intersect);
                using(var ink=new SolidBrush(Color.FromArgb(color.A,shape.Color)))
                using(var pen=new Pen(ink,width))
                using(var strokes=new GraphicsPath()){
                    float x0=bounds.Left,y0=bounds.Top;
                    switch(settings.Kind){
                        case FillPatternKind.Dots:
                            for(float y=FirstPatternLine(visible.Top-width,y0,spacing);y<=visible.Bottom+width;y+=spacing)
                                for(float x=FirstPatternLine(visible.Left-width,x0,spacing);x<=visible.Right+width;x+=spacing)
                                    g.FillEllipse(ink,x-width/2,y-width/2,width,width);
                            break;
                        case FillPatternKind.Horizontal:
                            for(float y=FirstPatternLine(visible.Top-width,y0,spacing);y<=visible.Bottom+width;y+=spacing)
                                PatternLine(strokes,bounds.Left-width,y,bounds.Right+width,y);
                            break;
                        case FillPatternKind.Vertical:
                            for(float x=FirstPatternLine(visible.Left-width,x0,spacing);x<=visible.Right+width;x+=spacing)
                                PatternLine(strokes,x,bounds.Top-width,x,bounds.Bottom+width);
                            break;
                        default:
                            float left=bounds.Left-width,right=bounds.Right+width;
                            for(float sum=FirstPatternLine(visible.Left+visible.Top-width*2,x0+y0,spacing);sum<=visible.Right+visible.Bottom+width*2;sum+=spacing)
                                PatternLine(strokes,left,sum-left,right,sum-right);
                            if(settings.Kind==FillPatternKind.Crosshatch)
                                for(float difference=FirstPatternLine(visible.Top-visible.Right-width*2,y0-x0,spacing);difference<=visible.Bottom-visible.Left+width*2;difference+=spacing)
                                    PatternLine(strokes,left,difference+left,right,difference+right);
                            break;
                    }
                    if(strokes.PointCount>0)g.DrawPath(pen,strokes);
                }
            }finally{g.Restore(state);}
        }
        static float FirstPatternLine(float value,float origin,float spacing){return origin+(float)Math.Floor((value-origin)/spacing)*spacing;}
        static void PatternLine(GraphicsPath path,float x1,float y1,float x2,float y2){path.StartFigure();path.AddLine(x1,y1,x2,y2);}
    }

    internal static class FillPatternMenu
    {
        static readonly string[] Names={"斜线","交叉斜线","波点","横线","竖线"};
        static readonly string[] Icons={"hatch-fill","pattern-cross","dots","pattern-horizontal","pattern-vertical"};
        internal static CraftMenuItem Defaults(Func<FillPatternSettings> read,Action<FillPatternSettings> set)
        {
            var root=new CraftMenuItem("默认填充图案","hatch-fill",delegate{});
            AddOptions(root.DropDownItems,read,set);
            root.DropDownOpening+=delegate{Refresh(root.DropDownItems,read());};
            return root;
        }
        internal static void AddOptions(ToolStripItemCollection items,Func<FillPatternSettings> read,Action<FillPatternSettings> apply)
        {
            var pattern=new CraftMenuItem("图案","hatch-fill",delegate{});
            var density=new CraftMenuItem("疏密","pattern-horizontal",delegate{});
            var ratio=new CraftMenuItem("图案线宽比例","line",delegate{});
            for(int i=0;i<Names.Length;i++){
                var kind=(FillPatternKind)i;
                pattern.DropDownItems.Add(new CraftMenuItem(Names[i],Icons[i],delegate{var choice=read();choice.Kind=kind;apply(choice);}){Tag=i});
            }
            foreach(int spacing in new[]{7,11,18,28})
                density.DropDownItems.Add(new CraftMenuItem(spacing==7?"密 · 7 px":spacing==11?"标准 · 11 px":spacing==18?"疏 · 18 px":"很疏 · 28 px","pattern-horizontal",delegate{var choice=read();choice.Spacing=spacing;apply(choice);}){Tag=spacing});
            foreach(int percent in new[]{25,50,75})
                ratio.DropDownItems.Add(new CraftMenuItem(percent+"%","line",delegate{var choice=read();choice.StrokePercent=percent;apply(choice);}){Tag=percent});
            items.Add(pattern);items.Add(density);items.Add(ratio);
            Refresh(items,read());
        }
        internal static void Refresh(ToolStripItemCollection items,FillPatternSettings current)
        {
            var values=new[]{(int)current.Kind,current.EffectiveSpacing,current.EffectiveStrokePercent};
            for(int i=0;i<values.Length;i++)
                foreach(ToolStripMenuItem item in ((ToolStripMenuItem)items[i]).DropDownItems)item.Checked=(int)item.Tag==values[i];
        }
    }

    public partial class Canvas
    {
        internal FillPatternSettings FillDefaults;
        StyledMenu fillMenu;
        internal bool CanChangeSelectedPattern(Point location)
        {
            if(SelectionCount!=1||!Crop.Contains(ToImg(location,false))||!SupportsShapeFill(ActiveKind,selected.ClosedPen))return false;
            var point=Painter.LocalPoint(selected,ToImg(location,false));
            using(var path=selected.Kind==Tool.Pen?Painter.FreehandPath(selected):
                selected.Kind==Tool.Cloud?Painter.CloudPath(selected.Box):new GraphicsPath()){
                if(selected.Kind==Tool.Ellipse&&selected.Box.Width>0&&selected.Box.Height>0)path.AddEllipse(selected.Box);
                else if(selected.Kind==Tool.Rect&&selected.Box.Width>0&&selected.Box.Height>0)
                    using(var rectangle=Theme.Round(selected.Box,selected.CornerRadius))path.AddPath(rectangle,false);
                return path.PointCount>0&&path.IsVisible(point);
            }
        }
        bool ShowShapeFillMenu(Point location)
        {
            if(!CanChangeSelectedPattern(location))return false;
            if(fillMenu!=null)fillMenu.Dispose();
            var shape=selected;
            var menu=new StyledMenu{MinimumContentWidth=210};
            FillPatternMenu.AddOptions(menu.Items,()=>shape.PatternSettings,value=>{
                if(selected==shape&&SelectionCount==1)EditSelected(s=>{value.Apply(s);s.FillEnabled=true;s.HatchedFill=true;});
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new CraftMenuItem("无填充","no-fill",()=>SetShapeFill(ShapeFill.None)){Checked=ActiveShapeFill==ShapeFill.None});
            menu.Items.Add(new CraftMenuItem("半透明填充","fill",()=>SetShapeFill(ShapeFill.Translucent)){Checked=ActiveShapeFill==ShapeFill.Translucent});
            fillMenu=menu;menu.Show(this,location);return true;
        }
    }
}
