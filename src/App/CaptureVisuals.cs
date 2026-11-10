using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ScreenCraft
{
    // Geometric artwork authored for ScreenCraft. Reference: 1Capture's public toolbar examples.
    internal static class CaptureLook
    {
        internal static readonly Color Surface=Color.White,Ink=Color.FromArgb(37,40,44),Blue=BrandArtwork.Blue,Selected=Color.FromArgb(226,237,255),Line=Color.FromArgb(224,227,231),Danger=Color.FromArgb(245,63,101);
        internal static void SurfaceAt(Graphics g,Rectangle bounds,int radius){PreferencesLook.FloatingCard(g,bounds,radius);}
        internal static void Icon(Graphics g,string name,Rectangle bounds,Color color)
        {
            var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/24f,bounds.Height/24f);
            using(var p=new Pen(color,1.85f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})using(var brush=new SolidBrush(color)){
                switch(name){
                    case "rotate":g.DrawArc(p,4,4,16,16,20,285);g.DrawLines(p,new[]{new Point(18,2),new Point(20,8),new Point(14,8)});g.DrawRectangle(p,8,9,7,7);break;
                    case "flip":g.DrawLine(p,12,2,12,22);g.DrawPolygon(p,new[]{new Point(3,18),new Point(9,5),new Point(9,18)});g.DrawPolygon(p,new[]{new Point(21,18),new Point(15,5),new Point(15,18)});break;
                    case "hide":g.DrawArc(p,3,6,18,12,0,180);g.DrawArc(p,3,6,18,12,180,180);g.DrawEllipse(p,9,9,6,6);g.DrawLine(p,3,3,21,21);break;
                    case "history":g.DrawArc(p,4,4,16,16,-70,290);g.DrawLines(p,new[]{new Point(3,4),new Point(3,10),new Point(8,10)});g.DrawLine(p,12,7,12,12);g.DrawLine(p,12,12,16,14);break;
                    case "board":g.DrawRectangle(p,3,3,18,18);g.DrawRectangle(p,6,6,7,6);g.DrawRectangle(p,11,13,7,5);break;
                    case "front":g.DrawRectangle(p,3,3,10,10);g.FillRectangle(brush,10,10,11,11);break;
                    case "back":g.FillRectangle(brush,3,3,10,10);g.DrawRectangle(p,10,10,11,11);break;
                    case "rect":using(var rect=Theme.Round(new RectangleF(3.5f,3.5f,17,17),1.6f))g.DrawPath(p,rect);break;
                    case "round":using(var path=Theme.Round(new Rectangle(4,4,16,16),4))g.DrawPath(p,path);break;
                    case "ellipse":g.DrawEllipse(p,3.5f,3.5f,17,17);break;
                    case "line":g.DrawLine(p,4,20,20,4);break;
                    case "arrow":g.DrawLine(p,4,20,20,4);g.DrawLines(p,new[]{new PointF(9,4),new PointF(20,4),new PointF(20,15)});break;
                    case "arrow-solid":g.FillPolygon(brush,new[]{new PointF(3,18),new PointF(13,8),new PointF(8,4),new PointF(21,3),new PointF(20,16),new PointF(16,11),new PointF(6,21)});break;
                    case "edit":case "pen":
                        using(var stroke=new GraphicsPath()){
                            stroke.AddBezier(2.8f,15.6f,6,13,13.8f,1,15,4.2f);
                            stroke.AddBezier(15,4.2f,16.2f,7.4f,6.8f,17.4f,9.3f,18);
                            stroke.AddBezier(9.3f,18,11,19.2f,17.7f,10,18.4f,12);
                            stroke.AddBezier(18.4f,12,19,13.8f,15.4f,19.8f,17.5f,20);
                            stroke.AddLine(17.5f,20,21.3f,16.7f);
                            g.DrawPath(p,stroke);
                        }break;
                    case "pen-closed":var vertices=new[]{new PointF(4,17),new PointF(8,4),new PointF(20,8),new PointF(17,20)};g.DrawPolygon(p,vertices);foreach(var vertex in vertices)g.FillRectangle(brush,vertex.X-1.5f,vertex.Y-1.5f,3,3);break;
                    case "pen-smooth":g.DrawBezier(p,new PointF(3,18),new PointF(20,0),new PointF(4,3),new PointF(21,18));break;
                    case "pen-arrow":g.DrawBezier(p,new PointF(3,4),new PointF(20,2),new PointF(0,14),new PointF(20,20));g.DrawLines(p,new[]{new PointF(14,12),new PointF(20,20),new PointF(10,21)});break;
                    case "arrow-outline":using(var shape=Painter.ArrowPath(new PointF(4,20),new PointF(20,4),3.5f))using(var edge=new Pen(color,2))g.DrawPath(edge,shape);break;
                    case "highlight":g.DrawPolygon(p,new[]{new PointF(4,14),new PointF(14,4),new PointF(20,10),new PointF(10,20)});g.DrawLine(p,3,21,10,21);break;
                    case "text":
                        g.DrawLines(p,new[]{new PointF(2,18.5f),new PointF(7.1f,5),new PointF(12.3f,18.5f)});
                        g.DrawLine(p,4.1f,13.4f,10.3f,13.4f);
                        g.DrawArc(p,14.1f,9,6.9f,6,220,142);
                        g.DrawLine(p,21,12,21,18.5f);
                        g.DrawEllipse(p,14.2f,13,6.8f,5.5f);
                        break;
                    case "counter":
                        g.DrawEllipse(p,3,3,18,18);
                        g.DrawLines(p,new[]{new PointF(10,10.2f),new PointF(12.4f,8),new PointF(12.4f,16.3f)});
                        break;
                    case "callout":
                        using(var bubble=new GraphicsPath()){
                            bubble.AddArc(2.8f,4,5,5,180,90);bubble.AddArc(16.2f,4,5,5,270,90);
                            bubble.AddArc(16.2f,12.2f,5,5,0,90);bubble.AddLine(12,17.2f,7.5f,21);
                            bubble.AddLine(7.5f,21,7.5f,17.2f);bubble.AddArc(2.8f,12.2f,5,5,90,90);
                            bubble.CloseFigure();g.DrawPath(p,bubble);
                        }
                        g.DrawLine(p,7,8.8f,17,8.8f);g.DrawLine(p,7,12.5f,13.8f,12.5f);
                        break;
                    case "counter-leader":g.FillEllipse(brush,1,2,4,4);g.DrawLine(p,5,6,11,12);using(var path=Theme.Round(new Rectangle(10,10,12,12),3))g.DrawPath(p,path);using(var font=new Font("Segoe UI",10,FontStyle.Bold,GraphicsUnit.Pixel))g.DrawString("1",font,brush,12,10);break;
                    case "grow":g.DrawRectangle(p,8,5,8,14);g.DrawLine(p,1,12,7,12);g.DrawLine(p,17,12,23,12);g.DrawLines(p,new[]{new Point(4,9),new Point(1,12),new Point(4,15)});g.DrawLines(p,new[]{new Point(20,9),new Point(23,12),new Point(20,15)});break;
                    case "cloud":using(var cloud=Painter.CloudPath(new RectangleF(3,4,18,14)))g.DrawPath(p,cloud);g.DrawLines(p,new[]{new PointF(8,18),new PointF(7,22),new PointF(13,18)});break;
                    case "pixelate":
                        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
                            if((row+col)%2==0)using(var tile=Theme.Round(new RectangleF(3+6*col,3+6*row,6,6),.9f))g.FillPath(brush,tile);
                        break;
                    case "blur":for(int y=4;y<22;y+=5)for(int x=4;x<22;x+=5){using(var dot=new SolidBrush(Color.FromArgb(80+(x+y)%3*65,color)))g.FillEllipse(dot,x-2,y-2,4,4);}break;
                    case "dots":for(int y=4;y<22;y+=6)for(int x=4;x<22;x+=6)g.FillEllipse(brush,x-1,y-1,3,3);break;
                    case "spotlight":using(var path=Theme.Round(new Rectangle(3,4,18,16),4))g.DrawPath(p,path);g.DrawRectangle(p,8,8,8,8);g.DrawLine(p,4,19,8,15);g.DrawLine(p,16,8,20,4);break;
                    case "watermark":g.DrawLines(p,new[]{new Point(3,18),new Point(10,5),new Point(17,18)});g.DrawLine(p,6,13,14,13);g.DrawLine(p,14,6,21,6);g.DrawLine(p,18,6,18,17);break;
                    case "redact":g.FillRectangle(brush,4,5,16,14);break;
                    case "undo":case "redo":
                        if(name=="redo"){g.TranslateTransform(24,0);g.ScaleTransform(-1,1);}
                        g.DrawLines(p,new[]{new PointF(8.4f,3.7f),new PointF(3.3f,8.8f),new PointF(8.4f,13.7f)});
                        using(var turn=new GraphicsPath()){
                            turn.AddLine(4,8.8f,14.5f,8.8f);
                            turn.AddBezier(14.5f,8.8f,22.4f,8.8f,22.4f,19.4f,14.5f,19.4f);
                            turn.AddLine(14.5f,19.4f,11,19.4f);g.DrawPath(p,turn);
                        }break;
                    case "pin":
                        using(var pin=new GraphicsPath()){
                            pin.AddLine(6.4f,3.3f,17.6f,3.3f);pin.AddBezier(17.6f,3.3f,17.6f,5.5f,15.3f,5.4f,15.3f,7);
                            pin.AddLine(15.3f,7,15.3f,10);pin.AddBezier(15.3f,10,15.3f,12,18.5f,12.4f,18.5f,15.7f);
                            pin.AddLine(18.5f,15.7f,5.5f,15.7f);pin.AddBezier(5.5f,15.7f,5.5f,12.4f,8.7f,12,8.7f,10);
                            pin.AddLine(8.7f,10,8.7f,7);pin.AddBezier(8.7f,7,8.7f,5.4f,6.4f,5.5f,6.4f,3.3f);
                            g.DrawPath(p,pin);
                        }
                        g.DrawLine(p,12,16,12,21);break;
                    case "save":g.DrawLine(p,12,3,12,16);g.DrawLines(p,new[]{new PointF(7,11),new PointF(12,16),new PointF(17,11)});g.DrawLines(p,new[]{new PointF(4,15),new PointF(4,21),new PointF(20,21),new PointF(20,15)});break;
                    case "ocr":g.DrawLines(p,new[]{new PointF(3,9),new PointF(3,3),new PointF(9,3)});g.DrawLines(p,new[]{new PointF(15,3),new PointF(21,3),new PointF(21,9)});g.DrawLines(p,new[]{new PointF(3,15),new PointF(3,21),new PointF(9,21)});g.DrawLines(p,new[]{new PointF(15,21),new PointF(21,21),new PointF(21,15)});g.DrawLine(p,8,7,16,7);g.DrawLine(p,12,7,12,17);break;
                    case "settings":
                        g.DrawLine(p,3.4f,7.4f,8,7.4f);g.DrawLine(p,13.5f,7.4f,20.6f,7.4f);g.DrawEllipse(p,8,4.6f,5.5f,5.5f);
                        g.DrawLine(p,3.4f,16.6f,12,16.6f);g.DrawLine(p,17.5f,16.6f,20.6f,16.6f);g.DrawEllipse(p,12,13.8f,5.5f,5.5f);break;
                    case "gear":var teeth=new PointF[32];for(int i=0;i<teeth.Length;i++){double a=i*Math.PI/16;float r=i%4==1||i%4==2?10:7.6f;teeth[i]=new PointF((float)(12+r*Math.Cos(a)),(float)(12+r*Math.Sin(a)));}g.DrawPolygon(p,teeth);g.DrawEllipse(p,8.5f,8.5f,7,7);break;
                    case "open":g.DrawLines(p,new[]{new PointF(3,8),new PointF(3,20),new PointF(20,20),new PointF(22,9),new PointF(10,9),new PointF(8,5),new PointF(3,5),new PointF(3,8)});g.DrawLine(p,3,11,20,11);break;
                    case "image":g.DrawRectangle(p,3,4,18,16);g.DrawLines(p,new[]{new PointF(4,18),new PointF(10,12),new PointF(14,16),new PointF(17,13),new PointF(21,17)});g.DrawEllipse(p,14,7,3,3);break;
                    case "paste":g.DrawRectangle(p,5,5,14,16);g.DrawRectangle(p,9,3,6,4);g.DrawLine(p,9,12,15,12);g.DrawLine(p,9,16,15,16);break;
                    case "stack":g.DrawRectangle(p,3,3,14,14);g.DrawLines(p,new[]{new PointF(20,7),new PointF(21,21),new PointF(7,21)});break;
                    case "copy":g.DrawRectangle(p,3,3,13,13);g.DrawRectangle(p,8,8,13,13);break;
                    case "project":g.DrawLines(p,new[]{new PointF(5,3),new PointF(15,3),new PointF(20,8),new PointF(20,21),new PointF(5,21),new PointF(5,3)});g.DrawLines(p,new[]{new PointF(15,3),new PointF(15,8),new PointF(20,8)});break;
                    case "scroll":g.DrawRectangle(p,5,3,14,18);g.DrawLine(p,12,6,12,17);g.DrawLines(p,new[]{new PointF(8,13),new PointF(12,17),new PointF(16,13)});break;
                    case "exit":g.DrawArc(p,4,4,16,17,-50,280);g.DrawLine(p,12,2,12,12);break;
                    case "keyboard":using(var path=Theme.Round(new Rectangle(2,5,20,14),3))g.DrawPath(p,path);for(int y=9;y<=12;y+=3)for(int x=6;x<=18;x+=4)g.FillRectangle(brush,x,y,1,1);g.DrawLine(p,8,16,16,16);break;
                    case "info":g.DrawEllipse(p,3,3,18,18);g.DrawLine(p,12,10,12,17);g.FillEllipse(brush,11,6,2,2);break;
                    case "minimize":g.DrawLine(p,4,12,20,12);break;
                    case "cancel":g.DrawLine(p,5,5,19,19);g.DrawLine(p,19,5,5,19);break;
                    case "done":g.DrawLines(p,new[]{new PointF(4,13),new PointF(10,19),new PointF(21,4)});break;
                    case "more":for(int x=4;x<=18;x+=7)g.FillEllipse(brush,x-1,10.3f,3.4f,3.4f);break;
                    case "fill":g.FillRectangle(brush,4,4,16,16);break;
                    case "no-fill":g.DrawRectangle(p,4,4,16,16);g.DrawLine(p,5,19,19,5);break;
                    case "dash":p.DashStyle=DashStyle.Dash;g.DrawLine(p,3,12,21,12);break;
                    case "dot":g.FillEllipse(brush,7,7,10,10);break;
                    case "color":g.DrawLine(p,6,18,17,7);g.DrawLines(p,new[]{new PointF(12,6),new PointF(18,12),new PointF(21,9),new PointF(15,3),new PointF(12,6)});g.DrawLine(p,3,21,7,20);break;
                    case "crop":g.DrawLines(p,new[]{new PointF(7,2),new PointF(7,17),new PointF(22,17)});g.DrawLines(p,new[]{new PointF(2,7),new PointF(17,7),new PointF(17,22)});break;
                }
            }g.Restore(state);
        }
    }
    internal sealed class CaptureBar:Panel
    {
        internal Bitmap Material;Rectangle materialBounds,materialRange;
        public CaptureBar(){DoubleBuffered=true;BackColor=Color.Transparent;SetStyle(ControlStyles.SupportsTransparentBackColor,true);Padding=new Padding(6);}
        internal void UpdateMaterial(Canvas canvas){var range=Rectangle.Round(canvas.Crop);if(Material!=null&&materialBounds==Bounds&&materialRange==range)return;var next=FrozenGlass.Create(canvas,Bounds);var old=Material;Material=next;materialBounds=Bounds;materialRange=range;if(old!=null)old.Dispose();Invalidate(true);}
        internal void PaintSurface(Graphics g,Rectangle bounds){CaptureLook.SurfaceAt(g,bounds,13);if(Material!=null){var state=g.Save();using(var path=Theme.Round(Rectangle.Inflate(bounds,-2,-2),13))g.SetClip(path,CombineMode.Intersect);g.DrawImage(Material,bounds);g.Restore(state);PreferencesLook.Round(g,Rectangle.Inflate(bounds,-2,-2),Color.Transparent,13,PreferencesLook.Line);}}
        protected override void OnPaint(PaintEventArgs e){PaintSurface(e.Graphics,ClientRectangle);base.OnPaint(e);}
        protected override void Dispose(bool disposing){if(disposing&&Material!=null){Material.Dispose();Material=null;}base.Dispose(disposing);}
    }
    internal sealed class CaptureIconButton:PreferencesPaintButton
    {
        public string Glyph;public Color? IconColor;public Color? Swatch;internal bool Secondary;bool selected;
        public bool Selected{get{return selected;}set{if(selected==value)return;selected=value;Invalidate();AccessibilityNotifyClients(AccessibleEvents.StateChange,-1);}}
        protected override AccessibleObject CreateAccessibilityInstance(){return new IconAccessibleObject(this);}
        sealed class IconAccessibleObject:ButtonBaseAccessibleObject
        {
            readonly CaptureIconButton button;
            internal IconAccessibleObject(CaptureIconButton button):base(button){this.button=button;}
            public override AccessibleStates State{get{return base.State|(button.Selected?AccessibleStates.Pressed:AccessibleStates.None);}}
        }
        public CaptureIconButton(){Size=new Size(34,34);Margin=new Padding(1);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=CaptureLook.Surface;TabStop=true;}
        internal void PaintButton(Graphics g,Rectangle bounds)
        {
            if(!FrozenGlass.PaintChild(this,g,bounds))PreferencesLook.Fill(g,bounds,CaptureLook.Surface);
            int side=Math.Min(bounds.Width,bounds.Height);
            int inset=Math.Max(2,side/16);
            if(Selected||Hover)PreferencesLook.Round(g,Rectangle.Inflate(bounds,-inset,-inset),Selected?(Secondary?Color.FromArgb(224,230,238):CaptureLook.Selected):Color.FromArgb(231,235,240),side/5,null);
            int size=(int)Math.Round(side*.72);
            var icon=new Rectangle(bounds.X+(bounds.Width-size)/2,bounds.Y+(bounds.Height-size)/2,size,size);
            if(Swatch.HasValue){
                g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var fill=new SolidBrush(Swatch.Value))using(var edge=new Pen(Color.White,side/20f)){
                    g.FillEllipse(fill,icon);g.DrawEllipse(edge,icon);
                }
            }else CaptureLook.Icon(g,Glyph,icon,!Enabled?Color.FromArgb(141,147,154):IconColor??(Selected?CaptureLook.Blue:CaptureLook.Ink));
            if(Focused&&ShowFocusCues)PreferencesLook.Round(g,Rectangle.Inflate(bounds,-inset,-inset),Color.Transparent,side/5,CaptureLook.Blue);
        }
        protected override void OnPaint(PaintEventArgs e){PaintButton(e.Graphics,ClientRectangle);}
    }
    internal sealed class CaptureSlider:Control
    {
        int value=3;bool dragging;public int Minimum=1,Maximum=24;public event EventHandler ValueChanged;
        public int Value{get{return value;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(next==this.value)return;this.value=next;Invalidate();var handler=ValueChanged;if(handler!=null)handler(this,EventArgs.Empty);}}
        public CaptureSlider(){Size=new Size(110,34);DoubleBuffered=true;BackColor=CaptureLook.Surface;TabStop=true;}
        int Gutter{get{return Math.Max(3,(int)Math.Round(Height*.29));}}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;
            if(!FrozenGlass.PaintChild(this,g,ClientRectangle))PreferencesLook.Fill(g,ClientRectangle,BackColor);
            int y=Height/2,pad=Gutter;
            float ratio=(Value-Minimum)/(float)Math.Max(1,Maximum-Minimum);
            float x=pad+ratio*Math.Max(1,Width-2*pad),radius=Math.Max(3,Height*.19f);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var rail=new Pen(Color.FromArgb(185,192,202),Math.Max(2,Height*.1f)){StartCap=LineCap.Round,EndCap=LineCap.Round})
            using(var active=new Pen(CaptureLook.Blue,rail.Width){StartCap=LineCap.Round,EndCap=LineCap.Round}){
                g.DrawLine(rail,pad,y,Math.Max(pad,Width-pad),y);
                if(x>pad)g.DrawLine(active,pad,y,x,y);
            }
            using(var edge=new Pen(Focused?CaptureLook.Blue:Color.FromArgb(155,164,176),Focused?2:1)){
                g.FillEllipse(Brushes.White,x-radius,y-radius,2*radius,2*radius);
                g.DrawEllipse(edge,x-radius,y-radius,2*radius,2*radius);
            }
        }
        internal void PaintPreview(Graphics g,Point at){var state=g.Save();g.TranslateTransform(at.X,at.Y);OnPaint(new PaintEventArgs(g,ClientRectangle));g.Restore(state);}
        internal void SetAt(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(Width-2*Gutter,x-Gutter))*(Maximum-Minimum)/(double)Math.Max(1,Width-2*Gutter));}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();dragging=true;Capture=true;SetAt(e.X);}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)SetAt(e.X);}protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(dragging&&e.Button==MouseButtons.Left){SetAt(e.X);dragging=false;Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override bool IsInputKey(Keys keyData){return keyData==Keys.Left||keyData==Keys.Right||base.IsInputKey(keyData);}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(e.Delta!=0)Value+=Math.Sign(e.Delta);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right){Value+=e.KeyCode==Keys.Right?1:-1;e.Handled=true;}base.OnKeyDown(e);}
    }
}
