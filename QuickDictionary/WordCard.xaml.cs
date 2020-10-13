using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for WordCard.xaml
    /// </summary>
    public partial class WordCard : UserControl
    {
        public static readonly DependencyProperty EditModeProperty =
         DependencyProperty.Register("EditMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, new PropertyChangedCallback(EditModeChanged)));

        public bool EditMode
        {
            get { return (bool)GetValue(EditModeProperty); }
            set { SetValue(EditModeProperty, value); }
        }

        private static void EditModeChanged(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            WordCard control = d as WordCard;
            control.EditModeChanged(e);
        }

        private void EditModeChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public static readonly DependencyProperty FlashcardModeProperty =
         DependencyProperty.Register("FlashcardMode", typeof(bool), typeof(WordCard), new
            PropertyMetadata(true, new PropertyChangedCallback(FlashcardModeChanged)));

        public bool FlashcardMode
        {
            get { return (bool)GetValue(FlashcardModeProperty); }
            set { SetValue(FlashcardModeProperty, value); }
        }

        private static void FlashcardModeChanged(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            WordCard control = d as WordCard;
            control.FlashcardModeChanged(e);
        }

        private void FlashcardModeChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public static readonly DependencyProperty FlashcardFlippedProperty =
         DependencyProperty.Register("FlashcardFlipped", typeof(bool), typeof(WordCard), new
            PropertyMetadata(false, new PropertyChangedCallback(FlashcardFlippedChanged)));

        public bool FlashcardFlipped
        {
            get { return (bool)GetValue(FlashcardFlippedProperty); }
            set { SetValue(FlashcardFlippedProperty, value); }
        }

        private static void FlashcardFlippedChanged(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            WordCard control = d as WordCard;
            control.FlashcardFlippedChanged(e);
        }

        private void FlashcardFlippedChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public event EventHandler WordEdited;
        public event EventHandler DeleteWord;

        public WordCard()
        {
            InitializeComponent();
            Helper.HideBoundingBox(root);
        }

        private void Flipper_Click(object sender, RoutedEventArgs e)
        {
            FlashcardFlipped = !FlashcardFlipped;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FlashcardMode = !FlashcardMode;
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
    }

    public class WordValidationRule : ValidationRule
    {
        public static ValidationResult ValidateWord(object value, CultureInfo cultureInfo)
        {
            string val = value as string;
            val = val.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(val))
                return new ValidationResult(false, "Word cannot be empty");
            return ValidationResult.ValidResult;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidateWord(value, cultureInfo);
        }
    }
}
