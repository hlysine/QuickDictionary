using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for InstantOcrHighlighter.xaml
    /// </summary>
    public partial class InstantOcrHighlighter : Window, INotifyPropertyChanged
    {

        private string word = null;
        public string Word
        {
            get => word;
            set
            {
                word = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word)));
            }
        }

        private string dictionaryName = null;
        public string DictionaryName
        {
            get => dictionaryName;
            set
            {
                dictionaryName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DictionaryName)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string description = null;

        public string Description
        {
            get => description;
            set
            {
                description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        const int padding = 3;

        public OCRWord OcrWord { get; set; }

        public void SetWord(OCRWord word)
        {
            OcrWord = word;
            Left = word.Rect.Left - padding;
            Top = word.Rect.Top - padding;
            Width = word.Rect.Width + padding * 2;
            Height = word.Rect.Height + padding * 2 + 3;
            this.word = word.Word;
        }

        public InstantOcrHighlighter()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)NativeMethods.GetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            NativeMethods.SetWindowDisplayAffinity(wndHelper.Handle, NativeMethods.DisplayAffinity.ExcludeFromCapture);
        }
    }
}
