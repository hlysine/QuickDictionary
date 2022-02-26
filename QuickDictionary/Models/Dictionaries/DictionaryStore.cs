using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;

namespace QuickDictionary.Models.Dictionaries;

public class DictionaryStore
{
    private static DictionaryStore instance;
    
    public readonly ObservableCollection<Dictionary> Dictionaries = new();

    private DictionaryStore()
    {
        Dictionaries.Add(new CambridgeEnglishChineseDictionary());
        Dictionaries.Add(new MerriamWebsterMedicalDictionary());
        Dictionaries.Add(new OxfordAdvancedLearnersDictionary());
        Dictionaries.Add(new DictionaryDotCom());
        Dictionaries.Add(new WikipediaDictionary());
        Dictionaries.Add(new GoogleTranslateDictionary());
        Dictionaries.Add(new GoogleSearchDictionary());
    }

    public static DictionaryStore Instance => instance ??= new DictionaryStore();

    [CanBeNull]
    public Dictionary GetDictionaryByUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
            return uri.IsAbsoluteUri ? Dictionaries.FirstOrDefault(x => x.ValidateUrl(url)) : null;

        return null;
    }
}
