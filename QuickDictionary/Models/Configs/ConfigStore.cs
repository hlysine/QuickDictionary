using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using QuickDictionary.Models.Dictionaries;

namespace QuickDictionary.Models.Configs;

public class ConfigStore : NotifyPropertyChanged
{
    public static readonly string PersistentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary");

    private Config config = new();

    public Config Config
    {
        get => config;
        private set => SetAndNotify(ref config, value);
    }

    private static ConfigStore instance;

    public static ConfigStore Instance => instance ??= new ConfigStore();

    private ConfigStore()
    {
        if (!Directory.Exists(PersistentPath))
        {
            Directory.CreateDirectory(PersistentPath);
        }

        LoadConfig();
    }

    public void SaveConfig()
    {
        using var fs = new FileStream(Path.Combine(PersistentPath, "config.xml"), FileMode.Create);
        var serializer = new XmlSerializer(typeof(Config));
        serializer.Serialize(fs, Config);
    }

    private void overrideConfigWithDefault()
    {
        Config = new Config
        {
            SelectedDictionaries = DictionaryStore.Instance.Dictionaries.Select(x => x.Name).ToList()
        };
        SaveConfig();
    }

    public void LoadConfig()
    {
        try
        {
            var path = Path.Combine(PersistentPath, "config.xml");
            if (File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.Open);
                var serializer = new XmlSerializer(typeof(Config));
                Config = (Config)serializer.Deserialize(fs);
            }
            else
            {
                overrideConfigWithDefault();
            }
        }
        catch (Exception _)
        {
            overrideConfigWithDefault();
        }
    }
}