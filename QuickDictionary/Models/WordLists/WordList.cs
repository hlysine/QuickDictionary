using System;
using System.Collections.ObjectModel;

namespace QuickDictionary.Models.WordLists;

public class WordList : NotifyPropertyChanged
{
    private DateTime created;
    private ObservableCollection<WordEntry> entries = new();
    private bool flashcardMode;
    private string name;

    public string Name
    {
        get => name;
        set => SetAndNotify(ref name, value);
    }

    public DateTime Created
    {
        get => created;
        set => SetAndNotify(ref created, value, new[] { nameof(CreatedString) });
    }

    public ObservableCollection<WordEntry> Entries
    {
        get => entries;
        set => SetAndNotify(ref entries, value);
    }

    public string CreatedString => created.ToString("d MMM, yyyy");

    public bool FlashcardMode
    {
        get => flashcardMode;
        set => SetAndNotify(ref flashcardMode, value);
    }
}
