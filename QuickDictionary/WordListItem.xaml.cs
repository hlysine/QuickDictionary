using System;
using System.Collections.Generic;
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
    /// Interaction logic for WordListItem.xaml
    /// </summary>
    public partial class WordListItem : UserControl
    {
        public static readonly DependencyProperty EditListsProperty =
         DependencyProperty.Register("EditLists", typeof(bool), typeof(WordListItem), new
            PropertyMetadata(false, new PropertyChangedCallback(EditListsChanged)));

        public bool EditLists
        {
            get { return (bool)GetValue(EditListsProperty); }
            set { SetValue(EditListsProperty, value); }
        }

        private static void EditListsChanged(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            WordListItem control = d as WordListItem;
            control.EditListsChanged(e);
        }

        private void EditListsChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public WordListItem()
        {
            InitializeComponent();
            Helper.HideBoundingBox(root);
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
}
