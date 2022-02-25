using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickDictionary.UserInterface.Controls;
using QuickDictionary.UserInterface.Validation;

namespace QuickDictionary.UserInterface.WordLists;

/// <summary>
/// Interaction logic for WordCard.xaml
/// </summary>
public partial class WordCard : UserControl
{
    public static readonly DependencyProperty EditModeProperty =
        DependencyProperty.Register("EditMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, EditModeChanged));

    public bool EditMode
    {
        get => (bool)GetValue(EditModeProperty);
        set => SetValue(EditModeProperty, value);
    }

    private static void EditModeChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordCard control)
            control.EditModeChanged(e);
    }

    private void EditModeChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    public static readonly DependencyProperty FlashcardModeProperty =
        DependencyProperty.Register("FlashcardMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(true, FlashcardModeChanged));

    public bool FlashcardMode
    {
        get => (bool)GetValue(FlashcardModeProperty);
        set => SetValue(FlashcardModeProperty, value);
    }

    private static void FlashcardModeChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordCard control)
            control.FlashcardModeChanged(e);
    }

    private void FlashcardModeChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    public static readonly DependencyProperty FlashcardFlippedProperty =
        DependencyProperty.Register("FlashcardFlipped", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, FlashcardFlippedChanged));

    public bool FlashcardFlipped
    {
        get => (bool)GetValue(FlashcardFlippedProperty);
        set => SetValue(FlashcardFlippedProperty, value);
    }

    private static void FlashcardFlippedChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordCard control)
            control.FlashcardFlippedChanged(e);
    }

    private void FlashcardFlippedChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    public event EventHandler WordEdited;
    public event EventHandler DeleteWord;
    public event EventHandler NavigateWord;

    public WordCard()
    {
        InitializeComponent();
        ControlUtils.HideBoundingBox(root);
    }

    private void Flipper_Click(object sender, RoutedEventArgs e)
    {
        FlashcardFlipped = !FlashcardFlipped;
    }

    private void btnEditSave_Click(object sender, RoutedEventArgs e)
    {
        if (!WordValidationRule.ValidateWord(txtEditWord.Text, CultureInfo.InvariantCulture).IsValid)
        {
            txtEditWord.Focus();
            Keyboard.Focus(txtEditWord);
            txtEditWord.SelectAll();
        }
        EditMode = false;
        WordEdited?.Invoke(this, EventArgs.Empty);
    }

    private void btnOpenEdit_Click(object sender, RoutedEventArgs e)
    {
        EditMode = true;
    }

    private void btnEditDelete_Click(object sender, RoutedEventArgs e)
    {
        EditMode = false;
        DeleteWord?.Invoke(this, EventArgs.Empty);
    }

    private void btnWordLink_Click(object sender, RoutedEventArgs e)
    {
        NavigateWord?.Invoke(this, EventArgs.Empty);
    }
}