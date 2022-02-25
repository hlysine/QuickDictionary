using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using QuickDictionary.Models.Configs;
using QuickDictionary.UserInterface;

namespace QuickDictionary.Models.WordLists;

public static class WordListStore
{
    public static ObservableCollection<WordListFile> WordListFiles { get; set; } = new();

    public static string WordListFolderPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ConfigStore.Instance.Config.WordListsPath))
            {
                ConfigStore.Instance.Config.WordListsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\Word Lists");
                ConfigStore.Instance.SaveConfig();
            }    
            return ConfigStore.Instance.Config.WordListsPath;
        }
    }

    public static readonly ObservableCollection<string> DeletedPaths = new();

    public static void CommitDeletions()
    {
        foreach (var path in DeletedPaths)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        DeletedPaths.Clear();
    }

    public static WordList LoadWordList(string path)
    {
        using var fs = new FileStream(path, FileMode.Open);
        var serializer = new XmlSerializer(typeof(WordList));
        return (WordList)serializer.Deserialize(fs);
    }

    public static async Task LoadAllWordLists()
    {
        if (!Directory.Exists(WordListFolderPath))
        {
            Directory.CreateDirectory(WordListFolderPath);
        }
        var lists = Directory.GetFiles(WordListFolderPath, "*.xml", SearchOption.TopDirectoryOnly);
        WordListFiles.Clear();
        await Task.Run(() =>
        {
            foreach (var path in lists)
            {
                WordListFiles.Add(new WordListFile(path, LoadWordList(path)));
            }
        });
    }

    public static void SaveWordList(WordListFile wordListFile)
    {
        using var fs = new FileStream(wordListFile.FilePath, FileMode.Create);
        var serializer = new XmlSerializer(typeof(WordList));
        serializer.Serialize(fs, wordListFile.WordList);
    }
}