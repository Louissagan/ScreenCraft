// Stackshot - Editor toolbar (also the window's title bar) and hint strip.
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using M = System.Windows.Media;

namespace Stackshot
{
    // Shared look of the editor chrome, read from the design palette at paint time so theme switches just repaint.
    public static class EdLook
    {
        static string text, semibold;

        public static Color C(M.Color c) { return Ds.Gdi(c); }

        // Area around the capture, so it floats on a calm, slightly darker surface.
        public static Color Surround { get { return Ds.Brushes.High ? C(Ds.Brushes.Window) : Ds.Dark ? Color.FromArgb(22, 22, 24) : Color.FromArgb(233, 233, 235); } }

        public static string Text
        {
            get
            {
                if (text == null) text = Fonts.Has("Segoe UI Variable Text") ? "Segoe UI Variable Text" : "Segoe UI";
                return text;
            }
        }

        public static string Semibold
        {
            get
            {
                if (semibold == null) semibold = Fonts.Has("Segoe UI Variable Text Semibold") ? "Segoe UI Variable Text Semibold" : "Segoe UI Semibold";
                return semibold;
            }
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb((int)Math.Round(a.A + (b.A - a.A) * t), (int)Math.Round(a.R + (b.R - a.R) * t),
                                  (int)Math.Round(a.G + (b.G - a.G) * t), (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static void Fill(Graphics g, RectangleF r, float radius, Color c)
        {
            using (GraphicsPath p = Theme.Round(r, radius))
            using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
        }

        // 1 px border just inside the shape.
        public static void Hairline(Graphics g, RectangleF r, float radius, Color c)
        {
            r.Inflate(-0.5f, -0.5f);
            using (GraphicsPath p = Theme.Round(r, Math.Max(0, radius - 0.5f)))
            using (Pen pen = new Pen(c, 1f)) g.DrawPath(pen, p);
        }

        // Line icon centered in r.
        public static void Icon(Graphics g, string name, RectangleF r, int px, M.Color color, float stroke)
        {
            if (name == null || px <= 0) return;
            if (!Glyph.Has(name)) { EditorGlyph(g, name, r, px, C(color), stroke); return; }
            Bitmap b = Ink.GlyphBitmap(name, px, color, stroke);
            g.DrawImage(b, new Rectangle((int)Math.Round(r.X + (r.Width - px) / 2f), (int)Math.Round(r.Y + (r.Height - px) / 2f), px, px));
        }

        // Icons only the editor uses, drawn on the same 24x24 grid as the shared line icons.
        static void EditorGlyph(Graphics g, string name, RectangleF r, int px, Color color, float stroke)
        {
            float k = px / 24f, x0 = (float)Math.Round(r.X + (r.Width - px) / 2f), y0 = (float)Math.Round(r.Y + (r.Height - px) / 2f);
            GraphicsState st = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(x0, y0);
            g.ScaleTransform(k, k);
            using (Pen pen = new Pen(color, stroke / k))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                if (name == "line") g.DrawLine(pen, 5.5f, 18.5f, 18.5f, 5.5f);
                else if (name == "spotlight")
                {
                    // A frame dimmed around a lit circle.
                    using (GraphicsPath p = Theme.Round(new RectangleF(3.5f, 5f, 17f, 14f), 2.5f)) g.DrawPath(pen, p);
                    using (SolidBrush dim = new SolidBrush(Color.FromArgb(color.A * 2 / 5, color)))
                    using (GraphicsPath outside = new GraphicsPath(FillMode.Alternate))
                    {
                        // A path, not a Region: GDI+ never antialiases FillRegion.
                        outside.AddRectangle(new RectangleF(3.5f, 5f, 17f, 14f));
                        outside.AddEllipse(8f, 7.5f, 8f, 9f);
                        g.FillPath(dim, outside);
                    }
                    g.DrawEllipse(pen, 8f, 7.5f, 8f, 9f);
                }
            }
            g.Restore(st);
        }

        public static M.Color White { get { return M.Color.FromRgb(255, 255, 255); } }
    }

}

