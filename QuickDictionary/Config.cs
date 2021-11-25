using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
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
        public double CaptureBoxWidth { get; set; } = 200;
        public double CaptureBoxWHRatio { get; set; } = 4;

        DispatcherTimer timer;
        bool needSave = false;

        public Config()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += Timer_Tick;
        }

        ~Config()
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            save();
        }

        private void save()
        {
            using (FileStream fs = new FileStream(Path.Combine(MainWindow.PersistentPath, "config.xml"), FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                serializer.Serialize(fs, this);
            }
        }

        public void SaveConfig()
        {
            needSave = true;
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
                    CaptureBoxWidth = conf.CaptureBoxWidth;
                    CaptureBoxWHRatio = conf.CaptureBoxWHRatio;
                }
            }
            else
            {
                SelectedDictionaries = MainWindow.Dictionaries.Select(x => x.Name).ToList();
                save();
            }
            timer.Start();
        }
    }
}
