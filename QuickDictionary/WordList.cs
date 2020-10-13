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

        public static string WordListPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\Word Lists");

        public static ObservableCollection<string> DeletedPaths = new ObservableCollection<string>();

        public static void CommitDeletions()
        {
            foreach (string path in DeletedPaths)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            DeletedPaths.Clear();
        }

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
            if (!Directory.Exists(WordListPath))
            {
                Directory.CreateDirectory(WordListPath);
            }
            string[] lists = Directory.GetFiles(WordListPath, "*.xml", SearchOption.TopDirectoryOnly);
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

    public class PathWordListPair : NotifyPropertyChanged
    {
        private string path;
        private WordList wordList;

        public string Path { get => path; set => SetAndNotify(ref path, value); }
        public WordList WordList { get => wordList; set => SetAndNotify(ref wordList, value); }

        public PathWordListPair(string path, WordList wordList) => (Path, WordList) = (path, wordList);
    }

    public class WordList : NotifyPropertyChanged
    {
        private string name;
        private DateTime created;
        private ObservableCollection<WordEntry> entries = new ObservableCollection<WordEntry>();
        private bool flashcardMode;

        public string Name { get => name; set => SetAndNotify(ref name, value); }
        public DateTime Created { get => created; set => SetAndNotify(ref created, value, calculatedProperties: new[] { nameof(CreatedString) }); }
        public ObservableCollection<WordEntry> Entries { get => entries; set => SetAndNotify(ref entries, value); }
        public string CreatedString
        {
            get
            {
                return created.ToString("d MMM, yyyy");
            }
        }
        public bool FlashcardMode { get => flashcardMode; set => SetAndNotify(ref flashcardMode, value); }
    }

    public class WordEntry : NotifyPropertyChanged
    {
        private string word;
        private string url;
        private string description;
        private DateTime created;
        private DateTime lastModified;
        private bool flashcardFlipped;

        public string Word { get => word; set => SetAndNotify(ref word, value); }
        public string Url { get => url; set => SetAndNotify(ref url, value, calculatedProperties: new[] { nameof(DictionaryName) }); }
        public string Description { get => description; set => SetAndNotify(ref description, value); }
        public DateTime Created { get => created; set => SetAndNotify(ref created, value, calculatedProperties: new[] { nameof(CreatedString) }); }
        public DateTime LastModified { get => lastModified; set => SetAndNotify(ref lastModified, value); }
        public string CreatedString 
        { 
            get
            {
                return created.ToString("d MMM, yyyy");
            } 
        }
        public string DictionaryName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Url)) return null;
                return MainWindow.Dictionaries.FirstOrDefault(x => new Uri(x.Url).Host.Trim().ToLower() == new Uri(Url).Host.Trim().ToLower())?.Name;
            }
        }
        public bool FlashcardFlipped { get => flashcardFlipped; set => SetAndNotify(ref flashcardFlipped, value); }
    }
}
