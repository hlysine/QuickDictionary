using System;
using System.Windows;
using System.Windows.Controls;
using QuickDictionary.UserInterface.Controls;

namespace QuickDictionary.UserInterface.WordLists;

/// <summary>
/// Interaction logic for WordListItem.xaml
/// </summary>
public partial class WordListItem : UserControl
{
    public static readonly DependencyProperty EditListsProperty =
        DependencyProperty.Register("EditLists", typeof(bool), typeof(WordListItem), new
            PropertyMetadata(false, EditListsChanged));

    public bool EditLists
    {
        get => (bool)GetValue(EditListsProperty);
        set => SetValue(EditListsProperty, value);
    }

    private static void EditListsChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordListItem control)
            control.EditListsChanged(e);
    }

    private void EditListsChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    public WordListItem()
    {
        InitializeComponent();
        ControlUtils.HideBoundingBox(root);
    }

    public event EventHandler DeleteList;
    public event EventHandler RenameList;

    private void btnDeleteList_Click(object sender, RoutedEventArgs e)
    {
        DeleteList?.Invoke(this, EventArgs.Empty);
    }

    private void btnRenameList_Click(object sender, RoutedEventArgs e)
    {
        RenameList?.Invoke(this, EventArgs.Empty);
    }
}