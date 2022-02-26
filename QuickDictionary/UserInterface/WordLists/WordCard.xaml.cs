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
    public static readonly DependencyProperty EDIT_MODE_PROPERTY =
        DependencyProperty.Register("EditMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, editModeChanged));

    public static readonly DependencyProperty FLASHCARD_MODE_PROPERTY =
        DependencyProperty.Register("FlashcardMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(true, flashcardModeChanged));

    public static readonly DependencyProperty FLASHCARD_FLIPPED_PROPERTY =
        DependencyProperty.Register("FlashcardFlipped", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, flashcardFlippedChanged));

    public WordCard()
    {
        InitializeComponent();
        ControlUtils.HideBoundingBox(root);
    }

    public bool EditMode
    {
        get => (bool)GetValue(EDIT_MODE_PROPERTY);
        set => SetValue(EDIT_MODE_PROPERTY, value);
    }

    public bool FlashcardMode
    {
        get => (bool)GetValue(FLASHCARD_MODE_PROPERTY);
        set => SetValue(FLASHCARD_MODE_PROPERTY, value);
    }

    public bool FlashcardFlipped
    {
        get => (bool)GetValue(FLASHCARD_FLIPPED_PROPERTY);
        set => SetValue(FLASHCARD_FLIPPED_PROPERTY, value);
    }

    private static void editModeChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordCard control)
            control.EditModeChanged(e);
    }

    private void EditModeChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    private static void flashcardModeChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is WordCard control)
            control.FlashcardModeChanged(e);
    }

    private void FlashcardModeChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    private static void flashcardFlippedChanged(DependencyObject d,
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
