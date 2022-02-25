namespace QuickDictionary.Models.WordLists;

public class WordListFile : NotifyPropertyChanged
{
    private string filePath;
    private WordList wordList;

    public string FilePath
    {
        get => filePath;
        set => SetAndNotify(ref filePath, value);
    }

    public WordList WordList
    {
        get => wordList;
        set => SetAndNotify(ref wordList, value);
    }

    public WordListFile(string path, WordList wordList) => (FilePath, WordList) = (path, wordList);
}