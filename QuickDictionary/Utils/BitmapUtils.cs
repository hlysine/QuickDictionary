using System.Drawing;

namespace QuickDictionary.Utils;

public static class BitmapUtils
{
    public static Bitmap CropAtRect(this Bitmap b, Rectangle r)
    {
        var nb = new Bitmap(r.Width, r.Height);
        using var g = Graphics.FromImage(nb);
        g.DrawImage(b, -r.X, -r.Y);
        return nb;
    }
}