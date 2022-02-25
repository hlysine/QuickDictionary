using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QuickDictionary.Models.Configs;
using QuickDictionary.Native;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickDictionary.UserInterface.Ocr;

/// <summary>
/// Interaction logic for OCROverlay.xaml
/// </summary>
public partial class ManualOcrOverlay : Window
{
    public Point Selection { get; set; }
    public bool Selected { get; set; }

    public delegate void WordSelectedHandler(object sender, Point position);
    public event WordSelectedHandler WordSelected;

    public ManualOcrOverlay()
    {
        InitializeComponent();
    }

    public void SetBackground(BitmapImage img)
    {
        Dispatcher.Invoke(() =>
        {
            imgBg.Source = img;
        });
    }

    private void updateBorder(Point pos)
    {
        Canvas.SetLeft(borderWord, pos.X - ConfigStore.Instance.Config.CaptureBoxWidth / 2);
        Canvas.SetTop(borderWord, pos.Y - ConfigStore.Instance.Config.CaptureBoxWidth / ConfigStore.Instance.Config.CaptureBoxAspectRatio / 2);
        borderWord.Width = ConfigStore.Instance.Config.CaptureBoxWidth;
        borderWord.Height = ConfigStore.Instance.Config.CaptureBoxWidth / ConfigStore.Instance.Config.CaptureBoxAspectRatio;
    }

    private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        e.Handled = true;
        var pos = e.GetPosition(this);
        updateBorder(pos);
    }

    private void Canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Selected) return;
        e.Handled = true;
        Selection = e.GetPosition(this);
        WordSelected?.Invoke(this, Selection);
        Selected = true;
        Close();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (Selected) return;
        Selection = new Point(-1, -1);
        WordSelected?.Invoke(this, Selection);
        Selected = true;
        Close();
    }
            
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var wndHelper = new WindowInteropHelper(this);

        var exStyle = (int)NativeMethods.GetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GwlExStyle);

        exStyle |= (int)NativeMethods.ExtendedWindowStyles.WsExToolWindow;
        NativeMethods.SetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GwlExStyle, (IntPtr)exStyle);
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Window_Deactivated(this, EventArgs.Empty);
        }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        ConfigStore.Instance.Config.CaptureBoxWidth *= Math.Pow(1.1, e.Delta / 120d);
        Console.WriteLine(e.Delta);
        ConfigStore.Instance.Config.CaptureBoxWidth = Math.Min(Screen.PrimaryScreen.Bounds.Width, Math.Max(50, ConfigStore.Instance.Config.CaptureBoxWidth));
        ConfigStore.Instance.SaveConfig();
        var pos = e.GetPosition(this);
        updateBorder(pos);
    }
}