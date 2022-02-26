﻿using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using QuickDictionary.Models.Dictionaries;

namespace QuickDictionary.Models.Configs;

public class ConfigStore : NotifyPropertyChanged
{
    public static readonly string PERSISTENT_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary");

    private static ConfigStore instance;

    private Config config = new();

    private ConfigStore()
    {
        if (!Directory.Exists(PERSISTENT_PATH))
            Directory.CreateDirectory(PERSISTENT_PATH);

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
        using var fs = new FileStream(Path.Combine(PERSISTENT_PATH, "config.xml"), FileMode.Create);
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
            string path = Path.Combine(PERSISTENT_PATH, "config.xml");

            if (File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.Open);
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
