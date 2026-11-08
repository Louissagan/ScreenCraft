using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Stackshot
{
    internal static class PinViews
    {
        internal static Bitmap Render(Bitmap source,int quarterTurns,bool flipX,bool flipY)
        {
            var result=new Bitmap(source);int turns=((quarterTurns%4)+4)%4;
            if(flipX)result.RotateFlip(RotateFlipType.RotateNoneFlipX);if(flipY)result.RotateFlip(RotateFlipType.RotateNoneFlipY);
            if(turns!=0)result.RotateFlip((RotateFlipType)turns);return result;
        }
        internal static Rectangle Snap(Rectangle candidate,Rectangle[] others,Rectangle area,int margin,int distance)
        {
            var body=Rectangle.Inflate(candidate,-margin,-margin);int bestX=distance+1,bestY=distance+1,dx=0,dy=0;
            Action<int> takeX=delegate(int value){if(Math.Abs(value)<bestX){bestX=Math.Abs(value);dx=value;}};
            Action<int> takeY=delegate(int value){if(Math.Abs(value)<bestY){bestY=Math.Abs(value);dy=value;}};
            takeX(area.Left-body.Left);takeX(area.Right-body.Right);takeY(area.Top-body.Top);takeY(area.Bottom-body.Bottom);
            foreach(var outer in others){var other=Rectangle.Inflate(outer,-margin,-margin);if(body.Bottom+distance>=other.Top&&body.Top-distance<=other.Bottom){takeX(other.Left-body.Left);takeX(other.Right-body.Right);takeX(other.Right+8-body.Left);takeX(other.Left-8-body.Right);}if(body.Right+distance>=other.Left&&body.Left-distance<=other.Right){takeY(other.Top-body.Top);takeY(other.Bottom-body.Bottom);takeY(other.Bottom+8-body.Top);takeY(other.Top-8-body.Bottom);}}
            candidate.Offset(bestX<=distance?dx:0,bestY<=distance?dy:0);return candidate;
        }
    }
    public partial class PinWindow
    {
        readonly Preferences pinPreferences=new Preferences();
        internal Bitmap ViewImage(){return new Bitmap(img);}
        internal void ChangeView(int turns,bool flipHorizontal,bool flipVertical,bool reset=false)
        {
            int oldTurns=State.QuarterTurns;bool oldX=State.FlipX,oldY=State.FlipY;
            State.QuarterTurns=reset?0:(State.QuarterTurns+turns+4)%4;State.FlipX=reset?false:State.FlipX^flipHorizontal;State.FlipY=reset?false:State.FlipY^flipVertical;
            try{ReplaceDocument(document.Clone());KeepInArea();}catch{State.QuarterTurns=oldTurns;State.FlipX=oldX;State.FlipY=oldY;throw;}
        }
        void KeepInArea(){if(testArea.HasValue){Bounds=PinGeometry.KeepReachable(Bounds,new[]{testArea.Value});State.Bounds=Bounds;}else KeepReachable();}
        internal void SetPinHidden(bool hidden,Action<PinWindow> show=null)
        {
            State.HiddenByUser=hidden;if(hidden)Hide();else{KeepInArea();if(show==null)Show();else show(this);}
        }
        internal static void HideAllPins(){foreach(var pin in All)if(pin.Visible)pin.SetPinHidden(true);}
        internal static void RestoreAllPins(Action<PinWindow> show=null){foreach(var pin in All)if(pin.State.HiddenByUser)pin.SetPinHidden(false,show);}
        internal static void ToggleAllPins(){if(All.Exists(delegate(PinWindow p){return p.Visible;}))HideAllPins();else RestoreAllPins();}
        internal void ApplyPinAppearance(Preferences preferences)
        {
            pinPreferences.PinReverseWheel=preferences.PinReverseWheel;pinPreferences.PinWheelOpacity=preferences.PinWheelOpacity;pinPreferences.PinSnap=preferences.PinSnap;
            pinPreferences.PinShadow=preferences.PinShadow;pinPreferences.PinRadius=preferences.PinRadius;pinPreferences.PinBorderWidth=preferences.PinBorderWidth;pinPreferences.PinBorderColor=preferences.PinBorderColor;
            drawnFor=null;if(IsHandleCreated)Redraw();
        }
        void AddDailyMenu(ContextMenuStrip menu)
        {
            var transforms=new CraftMenuItem("旋转与翻转","rotate",delegate{});
            transforms.DropDownItems.Add(new CraftMenuItem("顺时针旋转 90°  Ctrl+R","rotate",delegate{ChangeView(1,false,false);}));
            transforms.DropDownItems.Add(new CraftMenuItem("逆时针旋转 90°","rotate",delegate{ChangeView(3,false,false);}));
            transforms.DropDownItems.Add(new CraftMenuItem("水平翻转  Ctrl+H","flip",delegate{ChangeView(0,true,false);}));
            transforms.DropDownItems.Add(new CraftMenuItem("垂直翻转  Ctrl+J","flip",delegate{ChangeView(0,false,true);}));
            transforms.DropDownItems.Add(new CraftMenuItem("恢复原方向","undo",delegate{ChangeView(0,false,false,true);}));menu.Items.Add(transforms);
            menu.Items.Add(new CraftMenuItem("隐藏此图（托盘可恢复）","hide",delegate{SetPinHidden(true);}));
            menu.Items.Add(new CraftMenuItem("隐藏全部贴图","hide",HideAllPins));menu.Items.Add(new CraftMenuItem("恢复隐藏贴图","stack",delegate{RestoreAllPins();}));
        }
        protected override void WndProc(ref Message message)
        {
            // WM_MOVING belongs to this window; no global hooks or synthetic system input.
            if(message.Msg==0x216&&pinPreferences.PinSnap&&(ModifierKeys&Keys.Alt)==0){var r=(Native.RECT)Marshal.PtrToStructure(message.LParam,typeof(Native.RECT));var candidate=Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom);var others=new List<Rectangle>();foreach(var pin in All)if(pin!=this&&pin.Visible)others.Add(pin.Bounds);var area=testArea.HasValue?testArea.Value:Screen.FromRectangle(candidate).WorkingArea;candidate=PinViews.Snap(candidate,others.ToArray(),area,Pad,P(10));r.Left=candidate.Left;r.Top=candidate.Top;r.Right=candidate.Right;r.Bottom=candidate.Bottom;Marshal.StructureToPtr(r,message.LParam,false);}
            base.WndProc(ref message);
        }
    }
}
