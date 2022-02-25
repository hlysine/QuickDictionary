namespace QuickDictionary.Models.Dictionaries;

public class DictionaryLookupResult : NotifyPropertyChanged
{
    private Dictionary dictionary;
    public Dictionary Dictionary
    {
        get => dictionary;
        set => SetAndNotify(ref dictionary, value);
    }


    private bool hasEntry;
    public bool HasEntry
    {
        get => hasEntry;
        set => SetAndNotify(ref hasEntry, value);
    }


    private string tooltipText;
    public string TooltipText
    {
        get => tooltipText;
        set => SetAndNotify(ref tooltipText, value);
    }


    private string url;
    public string Url
    {
        get => url;
        set => SetAndNotify(ref url, value);
    }
}