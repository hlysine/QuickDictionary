using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using QuickDictionary.Native;

namespace QuickDictionary.UserInterface.Ocr;

/// <summary>
/// Interaction logic for AutoOcrHighlighter.xaml
/// </summary>
public partial class AutoOcrHighlighter : Window, INotifyPropertyChanged
{

    private string word;
    public string Word
    {
        get => word;
        set
        {
            word = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word)));
        }
    }

    private string dictionaryName;
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

    private string description;

    public string Description
    {
        get => description;
        set
        {
            description = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
        }
    }

    private const int padding = 3;

    public OcrEntry OcrEntry { get; set; }

    public void SetWord(OcrEntry entry)
    {
        OcrEntry = entry;
        Left = entry.Rect.Left - padding;
        Top = entry.Rect.Top - padding;
        Width = entry.Rect.Width + padding * 2;
        Height = entry.Rect.Height + padding * 2 + 3;
        this.word = entry.Word;
    }

    public AutoOcrHighlighter()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var wndHelper = new WindowInteropHelper(this);

        var exStyle = (int)NativeMethods.GetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GwlExStyle);

        exStyle |= (int)NativeMethods.ExtendedWindowStyles.WsExToolWindow;
        NativeMethods.SetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GwlExStyle, (IntPtr)exStyle);

        NativeMethods.SetWindowDisplayAffinity(wndHelper.Handle, NativeMethods.DisplayAffinity.ExcludeFromCapture);
    }
}