using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Tesseract;

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for OCROverlay.xaml
    /// </summary>
    public partial class OCROverlay : Window
    {
        public Point Selection { get; set; }
        public bool Selected { get; set; } = false;

        public delegate void WordSelectedHandler(object sender, Point position);
        public event WordSelectedHandler WordSelected;

        public OCROverlay()
        {
            InitializeComponent();
        }

        public void SetBg(BitmapImage img)
        {
            Dispatcher.Invoke(() =>
            {
                imgBg.Source = img;
            });
        }

        private void updateBorder(Point pos)
        {
            Canvas.SetLeft(borderWord, pos.X - MainWindow.Config.CaptureBoxWidth / 2);
            Canvas.SetTop(borderWord, pos.Y - MainWindow.Config.CaptureBoxWidth / MainWindow.Config.CaptureBoxWHRatio / 2);
            borderWord.Width = MainWindow.Config.CaptureBoxWidth;
            borderWord.Height = MainWindow.Config.CaptureBoxWidth / MainWindow.Config.CaptureBoxWHRatio;
        }

        private void Canvas_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            Point pos = e.GetPosition(this);
            updateBorder(pos);
        }

        private void Canvas_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)NativeMethods.GetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Window_Deactivated(this, EventArgs.Empty);
            }
        }

        private void Window_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            MainWindow.Config.CaptureBoxWidth *= Math.Pow(1.1, e.Delta / 120);
            Console.WriteLine(e.Delta);
            MainWindow.Config.CaptureBoxWidth = Math.Min(Screen.PrimaryScreen.Bounds.Width, Math.Max(50, MainWindow.Config.CaptureBoxWidth));
            MainWindow.Config.SaveConfig();
            Point pos = e.GetPosition(this);
            updateBorder(pos);
        }
    }
}
