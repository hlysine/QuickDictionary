using System;
using System.Drawing;

namespace QuickDictionary.Utils;

public static class MathUtils
{
    public static double DistanceToPoint(this RectangleF rect, double x, double y)
    {
        return Math.Sqrt(Math.Pow(Math.Max(0, Math.Abs(rect.X + rect.Width / 2 - x) - rect.Width / 2), 2) + Math.Pow(Math.Max(0, Math.Abs(rect.Y + rect.Height / 2 - y) - rect.Height / 2), 2));
    }
}
