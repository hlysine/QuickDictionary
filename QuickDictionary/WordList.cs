using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QuickDictionary
{
    public static class WordListManager
    {
        // Path-list tuple
        public static ObservableCollection<PathWordListPair> WordLists { get; set; } = new ObservableCollection<PathWordListPair>();

        private static string wordlistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\Word Lists");

        public static WordList LoadList(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(WordList));
                return (WordList)serializer.Deserialize(fs);
            }
        }

        public static async Task LoadAllLists()
        {
            if (!Directory.Exists(wordlistPath))
            {
                Directory.CreateDirectory(wordlistPath);
            }
            string[] lists = Directory.GetFiles(wordlistPath, "*.xml", SearchOption.TopDirectoryOnly);
            WordLists.Clear();
            await Task.Run(() =>
            {
                foreach (string listpath in lists)
                {
                    WordLists.Add(new PathWordListPair(listpath, LoadList(listpath)));
                }
            });
        }

        public static void SaveList(PathWordListPair wordListPair)
        {
            using (FileStream fs = new FileStream(wordListPair.Path, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(WordList));
                serializer.Serialize(fs, wordListPair.WordList);
            }
        }
    }

    public class PathWordListPair
    {
        public string Path { get; set; }
        public WordList WordList { get; set; }

        public PathWordListPair(string path, WordList wordList) => (Path, WordList) = (path, wordList);
    }

    public class WordList
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public ObservableCollection<WordEntry> Entries { get; set; } = new ObservableCollection<WordEntry>();
    }

    public class WordEntry
    {
        public string Word { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
    }
}
