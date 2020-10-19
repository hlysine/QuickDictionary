using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QuickDictionary
{
    public class Config
    {
        public List<string> SelectedDictionaries { get; set; } = new List<string>();
        public bool AlwaysOnTop { get; set; } = false;
        public bool WordListManagerAlwaysOnTop { get; set; } = false;
        public bool PauseClipboard { get; set; } = false;
        public string LastWordListName { get; set; } = null;
        public string WordListsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\Word Lists");

        public void SaveConfig()
        {
            using (FileStream fs = new FileStream(Path.Combine(MainWindow.PersistentPath, "config.xml"), FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                serializer.Serialize(fs, this);
            }
        }

        public void LoadConfig()
        {
            string path = Path.Combine(MainWindow.PersistentPath, "config.xml");
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Config));
                    Config conf = (Config)serializer.Deserialize(fs);
                    SelectedDictionaries = conf.SelectedDictionaries;
                    AlwaysOnTop = conf.AlwaysOnTop;
                    WordListManagerAlwaysOnTop = conf.WordListManagerAlwaysOnTop;
                    PauseClipboard = conf.PauseClipboard;
                    LastWordListName = conf.LastWordListName;
                    WordListsPath = conf.WordListsPath;
                }
            }
            else
            {
                SelectedDictionaries = MainWindow.Dictionaries.Select(x => x.Name).ToList();
            }
        }
    }
}
