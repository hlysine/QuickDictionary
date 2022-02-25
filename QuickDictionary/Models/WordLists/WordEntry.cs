using System;
using QuickDictionary.Models.Dictionaries;

namespace QuickDictionary.Models.WordLists;

public class WordEntry : NotifyPropertyChanged
{
    private string word;
    private string url;
    private string description;
    private DateTime created;
    private DateTime lastModified;
    private bool flashcardFlipped;

    public string Word
    {
        get => word; 
        set => SetAndNotify(ref word, value);
    }

    public string Url
    {
        get => url; 
        set => SetAndNotify(ref url, value, new[] { nameof(DictionaryName) });
    }

    public string Description
    {
        get => description; 
        set => SetAndNotify(ref description, value);
    }

    public DateTime Created
    {
        get => created; 
        set => SetAndNotify(ref created, value, new[] { nameof(CreatedString) });
    }

    public DateTime LastModified
    {
        get => lastModified; 
        set => SetAndNotify(ref lastModified, value);
    }
    public string CreatedString => created.ToString("d MMM, yyyy");

    public string DictionaryName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Url)) return null;
            return DictionaryStore.Instance.GetDictionaryByUrl(Url)?.Name;
        }
    }

    public bool FlashcardFlipped
    {
        get => flashcardFlipped;
        set => SetAndNotify(ref flashcardFlipped, value);
    }
}