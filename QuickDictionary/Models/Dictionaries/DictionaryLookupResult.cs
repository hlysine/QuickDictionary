namespace QuickDictionary.Models.Dictionaries;

public class DictionaryLookupResult : NotifyPropertyChanged
{
    private Dictionary dictionary;

    private bool hasEntry;

    private string tooltipText;

    private string url;

    public Dictionary Dictionary
    {
        get => dictionary;
        set => SetAndNotify(ref dictionary, value);
    }

    public bool HasEntry
    {
        get => hasEntry;
        set => SetAndNotify(ref hasEntry, value);
    }

    public string TooltipText
    {
        get => tooltipText;
        set => SetAndNotify(ref tooltipText, value);
    }

    public string Url
    {
        get => url;
        set => SetAndNotify(ref url, value);
    }
}
