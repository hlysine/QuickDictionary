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
        public List<OCRWord> OCRWords { get; set; }
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

        private void showBorder(OCRWord word)
        {
            Canvas.SetLeft(borderWord, word.Rect.X);
            Canvas.SetTop(borderWord, word.Rect.Y);
            borderWord.Width = word.Rect.Width;
            borderWord.Height = word.Rect.Height;
            borderWord.Visibility = Visibility.Visible;
            borderWord.ToolTip = word.Word;
        }

        public static OCRWord FindClosest(List<OCRWord> OCRWords, float x, float y)
        {
            var word = OCRWords.FirstOrDefault(w => w.Rect.Contains(x, y));
            if (word != null)
            {
                return word;
            }
            word = OCRWords.MinBy(w => w.Rect.DistanceToPoint(x, y)).FirstOrDefault();
            if (word != null)
            {
                return word;
            }
            return null;
        }

        private void Canvas_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            if (OCRWords != null)
            {
                Point pos = e.GetPosition(this);
                var word = FindClosest(OCRWords, (float)pos.X, (float)pos.Y);
                if (word != null)
                {
                    showBorder(word);
                    return;
                }
            }
            borderWord.Visibility = Visibility.Collapsed;
        }

        private void Canvas_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Window_Deactivated(this, EventArgs.Empty);
            }
        }
    }
}
