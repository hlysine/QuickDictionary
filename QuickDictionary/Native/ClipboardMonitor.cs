using System;
using System.Windows;
using System.Windows.Interop;

namespace QuickDictionary.Native;

public class ClipboardMonitor
{
    public event EventHandler ClipboardChanged;

    public ClipboardMonitor(Window windowSource)
    {
        var source = PresentationSource.FromVisual(windowSource) as HwndSource;
        if (source == null)
        {
            throw new ArgumentException(
                "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                , nameof(windowSource));
        }

        source.AddHook(WndProc);

        // get window handle for interop
        var windowHandle = new WindowInteropHelper(windowSource).Handle;

        // register for clipboard events
        NativeMethods.AddClipboardFormatListener(windowHandle);
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly IntPtr wndProcSuccess = IntPtr.Zero;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARD_UPDATE)
        {
            OnClipboardChanged();
            handled = true;
        }

        return wndProcSuccess;
    }
}