using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace QuickDictionary.Native;

using static NativeMethods;
public static class ScreenCapture
{
    public static Bitmap GetScreenshot()
    {
        //Create a new bitmap.
        var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
            Screen.PrimaryScreen.Bounds.Height,
            PixelFormat.Format32bppArgb);

        // Create a graphics object from the bitmap.
        var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

        // Take the screenshot from the upper left corner to the right bottom corner.
        gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
            Screen.PrimaryScreen.Bounds.Y,
            0,
            0,
            Screen.PrimaryScreen.Bounds.Size,
            CopyPixelOperation.SourceCopy);

        return bmpScreenshot;
    }

    public static Bitmap GetScreenshot(IntPtr ihandle)
    {
        var hwnd = ihandle;//handle here

        NativeRect rc;
        GetWindowRect(hwnd, out rc);

        var bmp = new Bitmap(rc.Right - rc.Left, rc.Bottom - rc.Top, PixelFormat.Format32bppArgb);
        var gfxBmp = Graphics.FromImage(bmp);
        IntPtr hdcBitmap;
        try
        {
            hdcBitmap = gfxBmp.GetHdc();
        }
        catch
        {
            return null;
        }
        var succeeded = PrintWindow(hwnd, hdcBitmap, 0);
        gfxBmp.ReleaseHdc(hdcBitmap);
        if (!succeeded)
        {
            gfxBmp.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(Point.Empty, bmp.Size));
        }
        var hRgn = CreateRectRgn(0, 0, 0, 0);
        GetWindowRgn(hwnd, hRgn);
        var region = Region.FromHrgn(hRgn);//err here once
        if (!region.IsEmpty(gfxBmp))
        {
            gfxBmp.ExcludeClip(region);
            gfxBmp.Clear(Color.Transparent);
        }
        gfxBmp.Dispose();
        return bmp;
    }
}