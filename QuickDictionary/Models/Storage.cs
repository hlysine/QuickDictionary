using System;
using System.IO;

namespace QuickDictionary.Models;

public static class Storage
{
    static Storage()
    {
        if (!Directory.Exists(ApplicationStoragePath))
            Directory.CreateDirectory(ApplicationStoragePath);
    }

    public static string ApplicationStoragePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary");

    public static string ToAbsolutePath(string relativePath)
    {
        return Path.Combine(ApplicationStoragePath, relativePath);
    }
}
