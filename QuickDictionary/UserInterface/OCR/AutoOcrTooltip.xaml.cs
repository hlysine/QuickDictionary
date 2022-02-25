using System.ComponentModel;
using System.Windows.Controls;

namespace QuickDictionary.UserInterface.OCR;

/// <summary>
/// Interaction logic for AutoOcrTooltip.xaml
/// </summary>
public partial class AutoOcrTooltip : UserControl, INotifyPropertyChanged
{
    public AutoOcrTooltip()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler PropertyChanged;
}