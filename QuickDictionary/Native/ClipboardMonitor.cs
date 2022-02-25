using System;
using System.Windows;
using System.Windows.Interop;

namespace QuickDictionary.Native;

public class ClipboardMonitor
{
    public event EventHandler ClipboardChanged;

    public ClipboardMonitor(Window windowSource)
    {
        if (PresentationSource.FromVisual(windowSource) is not HwndSource source)
        {
            throw new ArgumentException(
                @"Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                , nameof(windowSource));
        }

        source.AddHook(wndProc);

        // get window handle for interop
        var windowHandle = new WindowInteropHelper(windowSource).Handle;

        // register for clipboard events
        NativeMethods.AddClipboardFormatListener(windowHandle);
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly IntPtr wnd_proc_success = IntPtr.Zero;

    private IntPtr wndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARD_UPDATE)
        {
            OnClipboardChanged();
            handled = true;
        }

        return wnd_proc_success;
    }
}