using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoreLinq;
using QuickDictionary.Models.Configs;
using QuickDictionary.Models.WordLists;
using QuickDictionary.UserInterface.Controls;
using QuickDictionary.UserInterface.Validation;

namespace QuickDictionary.UserInterface.WordLists;

/// <summary>
/// Interaction logic for WordLists.xaml
/// </summary>
public partial class WordListBrowser : Window, INotifyPropertyChanged
{
    private readonly MainWindow mainWindow;

    private WordListSortOption selectedSortOption;
    
    private bool editLists;

    private string renameListName;

    private WordListFile renamingList;

    private WordListFile selectedWordList;

    public WordListBrowser(MainWindow mainWindow)
    {
        InitializeComponent();
        this.mainWindow = mainWindow;
        ControlUtils.HideBoundingBox(root);
        listWordLists.ItemsSource = WordListStore.WordListFiles;
        checkTopmost.IsSelected = ConfigStore.Instance.Config.WordListManagerAlwaysOnTop;
        drawerHost.IsLeftDrawerOpen = true;
        SortOptions = new ObservableCollection<WordListSortOption>
        {
            new(null, "No sorting"),
            new(nameof(WordEntry.Word), "Word"),
            new(nameof(WordEntry.Created), "Time created"),
            new(nameof(WordEntry.LastModified), "Last modified"),
            new(nameof(WordEntry.DictionaryName), "Dictionary")
        };
    }
    
    public ObservableCollection<WordListSortOption> SortOptions { get; }

    public WordListSortOption SelectedSortOption
    {
        get => selectedSortOption;
        set
        {
            selectedSortOption = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSortOption)));
        }
    }

    public bool EditLists
    {
        get => editLists;
        set
        {
            editLists = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditLists)));
        }
    }

    public string RenameListName
    {
        get => renameListName;
        set
        {
            renameListName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RenameListName)));
        }
    }

    public WordListFile SelectedWordList
    {
        get => selectedWordList;
        set
        {
            selectedWordList = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedWordList)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void btnFlipToBack_Click(object sender, RoutedEventArgs e)
    {
        SelectedWordList?.WordList?.Entries?.ForEach(x => x.FlashcardFlipped = true);
    }

    private void btnFlipToFront_Click(object sender, RoutedEventArgs e)
    {
        SelectedWordList?.WordList?.Entries?.ForEach(x => x.FlashcardFlipped = false);
    }

    private void WordCard_WordEdited(object sender, EventArgs e)
    {
        if (SelectedWordList != null)
            WordListStore.SaveWordList(SelectedWordList);
    }

    private void WordCard_DeleteWord(object sender, EventArgs e)
    {
        if (SelectedWordList == null) return;

        if ((sender as WordCard)?.DataContext is WordEntry entry)
        {
            if (SelectedWordList.WordList == null || SelectedWordList.WordList.Entries == null) return;
            snackbarMain.MessageQueue.Enqueue(
                entry.Word + " deleted",
                "UNDO",
                obj =>
                {
                    obj.SelectedWordList.WordList.Entries.Insert(obj.Item2, obj.entry);
                    WordListStore.SaveWordList(obj.SelectedWordList);
                },
                (SelectedWordList, SelectedWordList.WordList.Entries.IndexOf(entry), entry),
                false,
                true,
                TimeSpan.FromSeconds(5));
            SelectedWordList.WordList.Entries.Remove(entry);
            WordListStore.SaveWordList(SelectedWordList);
        }
    }

    private void WordListsWindow_Closing(object sender, CancelEventArgs e)
    {
        if (SelectedWordList != null)
            WordListStore.SaveWordList(SelectedWordList);
    }

    private void WordListItem_DeleteList(object sender, EventArgs e)
    {
        if ((sender as WordListItem)?.DataContext is WordListFile entry)
        {
            snackbar.MessageQueue.Enqueue(
                entry.WordList.Name + " deleted",
                "UNDO",
                pair =>
                {
                    WordListStore.WordListFiles.Insert(pair.Item1, pair.entry);
                    WordListStore.DeletedPaths.Remove(pair.entry.FilePath);
                },
                (WordListStore.WordListFiles.IndexOf(entry), entry),
                false,
                true,
                TimeSpan.FromSeconds(5));
            WordListStore.WordListFiles.Remove(entry);
            WordListStore.DeletedPaths.Add(entry.FilePath);
        }
    }

    private void WordListItem_RenameList(object sender, EventArgs e)
    {
        renamingList = (sender as WordListItem)?.DataContext as WordListFile;

        if (renamingList != null)
        {
            RenameListName = renamingList.WordList.Name;
            dialogHost.IsOpen = true;
        }
    }

    private void btnRenameListSave_Click(object sender, RoutedEventArgs e)
    {
        if (renamingList == null) return;

        if (!WordListNameValidationRule.ValidateWordlistName(txtRenameListName.Text, CultureInfo.InvariantCulture).IsValid)
        {
            txtRenameListName.Focus();
            Keyboard.Focus(txtRenameListName);
            txtRenameListName.SelectAll();
            return;
        }

        string newPath = Path.Combine(Path.GetDirectoryName(renamingList.FilePath) ?? string.Empty, txtRenameListName.Text + ".xml");
        File.Move(renamingList.FilePath, newPath);
        renamingList.WordList.Name = txtRenameListName.Text;
        renamingList.FilePath = newPath;
        WordListStore.SaveWordList(renamingList);
        renamingList = null;
        snackbar.MessageQueue.Enqueue($"{txtRenameListName.Text} renamed");
        dialogHost.IsOpen = false;
    }

    private void btnRenameListCancel_Click(object sender, RoutedEventArgs e)
    {
        dialogHost.IsOpen = false;
        renamingList = null;
    }

    private void check_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfigStore.Instance.Config.WordListManagerAlwaysOnTop = checkTopmost.IsSelected;
        ConfigStore.Instance.SaveConfig();
    }

    private void WordCard_NavigateWord(object sender, EventArgs e)
    {
        if ((sender as WordCard)?.DataContext is WordEntry entry)
            mainWindow.NavigateWord(entry);
    }

    public class WordListSortOption
    {
        public string PropertyName { get; }
        public string DisplayName { get; }

        public WordListSortOption(string propertyName, string displayName)
        {
            PropertyName = propertyName;
            DisplayName = displayName;
        }
    }
}
