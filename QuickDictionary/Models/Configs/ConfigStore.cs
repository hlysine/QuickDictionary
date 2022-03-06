using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using QuickDictionary.Models.Dictionaries;

namespace QuickDictionary.Models.Configs;

public class ConfigStore : NotifyPropertyChanged
{
    private static readonly string config_path = Storage.ToAbsolutePath("config.xml");
    
    private static ConfigStore instance;

    private Config config = new();

    private ConfigStore()
    {
        LoadConfig();
    }

    public Config Config
    {
        get => config;
        private set => SetAndNotify(ref config, value);
    }

    public static ConfigStore Instance => instance ??= new ConfigStore();

    public void SaveConfig()
    {
        using var fs = new FileStream(config_path, FileMode.Create);
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
            if (File.Exists(config_path))
            {
                using var fs = new FileStream(config_path, FileMode.Open);
                var serializer = new XmlSerializer(typeof(Config));
                Config = (Config)serializer.Deserialize(fs);
            }
            else
                overrideConfigWithDefault();
        }
        catch (Exception)
        {
            overrideConfigWithDefault();
        }
    }
}
