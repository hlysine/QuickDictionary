using System;
using System.Collections.Generic;
using System.IO;

namespace QuickDictionary.Models.Configs;

public class Config
{
    public List<string> SelectedDictionaries { get; set; } = new();

    public bool AlwaysOnTop { get; set; }

    public bool WordListManagerAlwaysOnTop { get; set; }

    public bool PauseClipboard { get; set; }

    public string LastWordListName { get; set; }

    public string WordListsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\Word Lists");

    public double CaptureBoxWidth { get; set; } = 200;

    public double CaptureBoxAspectRatio { get; set; } = 4;
}
