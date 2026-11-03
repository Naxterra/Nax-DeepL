using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconMaker;

/// <summary>
/// Draws the Nax-Translate icon: two speech bubbles on the app's blue tile, "A" in the white one and
/// "Ä" in the dark one. The drawing uses a 256-unit grid and is rendered separately for every size,
/// with a simpler shape without letters at 24 pixels and below, so small sizes stay sharp.
/// Usage: IconMaker &lt;icon.ico&gt; &lt;logo.png&gt; [preview.png]
/// </summary>
static class Program
{
    static readonly Color Light = Color.FromRgb(0x60, 0xCD, 0xFF);
    static readonly Color Deep  = Color.FromRgb(0x3A, 0x7B, 0xFF);
    static readonly Color Navy  = Color.FromRgb(0x0B, 0x2A, 0x6F);

    // Every size Windows asks for at the usual display scales, from the tray to the Start menu.
    static readonly int[] IcoSizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: IconMaker <icon.ico> <logo.png> [preview.png]");
            return 1;
        }
        WriteIco(args[0], IcoSizes.Select(s => (s, Png(Render(s)))).ToList());
        // The logo in the window's header is shown at 30 pixels, so 128 stays sharp up to 400 % scaling.
        File.WriteAllBytes(args[1], Png(Render(128)));
        if (args.Length > 2) File.WriteAllBytes(args[2], Png(Preview()));
        return 0;
    }

    static BitmapSource Render(int px)
    {
        var v = new DrawingVisual();
        using (var dc = v.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(px / 256.0, px / 256.0));
            var small = px <= 24;
            Tile(dc, small);
            Bubbles(dc, small);
            dc.Pop();
        }
        var rt = new RenderTargetBitmap(px, px, 96, 96, PixelFormats.Pbgra32);
        rt.Render(v);
        return rt;
    }

    // All sizes next to each other on a light and a dark strip, for checking the result by eye.
    static BitmapSource Preview()
    {
        const int pad = 16;
        var width = IcoSizes.Sum() + pad * (IcoSizes.Length + 1);
        var v = new DrawingVisual();
        using (var dc = v.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3)), null, new Rect(0, 0, width, 288));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), null, new Rect(0, 288, width, 288));
            var x = pad;
            foreach (var s in IcoSizes)
            {
                var bmp = Render(s);
                dc.DrawImage(bmp, new Rect(x, 16 + (256 - s) / 2.0, s, s));
                dc.DrawImage(bmp, new Rect(x, 304 + (256 - s) / 2.0, s, s));
                x += s + pad;
            }
        }
        var rt = new RenderTargetBitmap(width, 576, 96, 96, PixelFormats.Pbgra32);
        rt.Render(v);
        return rt;
    }

    // The rounded blue tile, the same gradient as the window's accent colors.
    static void Tile(DrawingContext dc, bool small)
    {
        var inset = small ? 8.0 : 16.0;
        const double radius = 56;
        var area = new Rect(inset, inset, 256 - 2 * inset, 256 - 2 * inset);
        dc.DrawRoundedRectangle(new LinearGradientBrush(Light, Deep, new Point(0, 0), new Point(1, 1)), null, area, radius, radius);
        if (small) return;
        // A soft highlight on the upper half gives the tile some depth.
        var highlight = new LinearGradientBrush(Color.FromArgb(70, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(0, 1));
        dc.PushClip(new RectangleGeometry(area, radius, radius));
        dc.DrawRectangle(highlight, null, new Rect(inset, inset, area.Width, 120));
        dc.Pop();
    }

    static void Bubbles(DrawingContext dc, bool small)
    {
        var navy = new SolidColorBrush(Navy);
        if (small)
        {
            // Two plain shapes that still read as two bubbles at 16 pixels.
            dc.DrawGeometry(Brushes.White, null, Bubble(new Rect(36, 40, 128, 96), 26, tailLeft: true, 0.3));
            dc.DrawGeometry(navy, Gap(18), Bubble(new Rect(92, 108, 128, 96), 26, tailLeft: false, 0.3));
            return;
        }
        var back  = new Rect(42, 46, 116, 88);
        var front = new Rect(98, 106, 116, 88);
        dc.DrawGeometry(Brushes.White, null, Bubble(back, 24, tailLeft: true));
        dc.DrawGeometry(navy, Gap(10), Bubble(front, 24, tailLeft: false));
        Letter(dc, "A", new Point(back.Left + back.Width * 0.42, back.Top + back.Height / 2), 64, new SolidColorBrush(Deep));
        Letter(dc, "Ä", new Point(front.Left + front.Width / 2, front.Top + front.Height / 2 + 2), 66, Brushes.White);
    }

    // The outline around the dark bubble, in the tile's own blue, so it reads as a gap between the bubbles.
    static Pen Gap(double width) =>
        new(new LinearGradientBrush(Light, Deep, new Point(16, 16), new Point(240, 240)) { MappingMode = BrushMappingMode.Absolute }, width)
        { LineJoin = PenLineJoin.Round };

    // A speech bubble as one outline (body and tail combined), so the gap runs around both.
    static Geometry Bubble(Rect body, double r, bool tailLeft, double tail = 0.26)
    {
        var tri = new StreamGeometry();
        using (var c = tri.Open())
        {
            var h = body.Height * tail;
            if (tailLeft)
            {
                c.BeginFigure(new Point(body.Left + body.Width * 0.16, body.Bottom - r), true, true);
                c.LineTo(new Point(body.Left + body.Width * 0.12, body.Bottom + h), true, false);
                c.LineTo(new Point(body.Left + body.Width * 0.42, body.Bottom - 1), true, false);
            }
            else
            {
                c.BeginFigure(new Point(body.Right - body.Width * 0.16, body.Bottom - r), true, true);
                c.LineTo(new Point(body.Right - body.Width * 0.12, body.Bottom + h), true, false);
                c.LineTo(new Point(body.Right - body.Width * 0.42, body.Bottom - 1), true, false);
            }
        }
        return Geometry.Combine(new RectangleGeometry(body, r, r), tri, GeometryCombineMode.Union, null);
    }

    static void Letter(DrawingContext dc, string text, Point center, double size, Brush brush)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            size, brush, 1.0);
        // Centered on the ink rather than the line box, so the letter with umlaut sits visually centered.
        var geo = ft.BuildGeometry(new Point(0, 0));
        var b = geo.Bounds;
        dc.PushTransform(new TranslateTransform(center.X - b.X - b.Width / 2, center.Y - b.Y - b.Height / 2));
        dc.DrawGeometry(brush, null, geo);
        dc.Pop();
    }

    static byte[] Png(BitmapSource src)
    {
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(src));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }

    // An .ico with one PNG-compressed image per size, which Windows Vista and later read.
    static void WriteIco(string path, List<(int Size, byte[] Data)> images)
    {
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (s, data) in images)
        {
            w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
            w.Write((byte)0); w.Write((byte)0);
            w.Write((ushort)1); w.Write((ushort)32);
            w.Write(data.Length); w.Write(offset);
            offset += data.Length;
        }
        foreach (var (_, data) in images) w.Write(data);
    }
}
