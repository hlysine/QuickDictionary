using System.Net;
using System.Threading.Tasks;
using CefSharp.Wpf;
using JetBrains.Annotations;
using MaterialDesignThemes.Wpf;

namespace QuickDictionary.Models.Dictionaries;

public abstract class Dictionary : NotifyPropertyChanged
{
    private int precedence;

    /// <summary>
    /// Url to the dictionary with %s in place of the query
    /// </summary>
    protected abstract string Url { get; }

    /// <summary>
    /// Pack icon shown in the toolbar
    /// </summary>
    public abstract PackIconKind Icon { get; }

    public abstract string Name { get; }

    public string PrecedenceString => precedence > 0 ? precedence.ToString() : "";

    public int Precedence
    {
        get => precedence;
        set => SetAndNotify(ref precedence, value, new[] { nameof(PrecedenceString) });
    }

    public string GetUrl(string word)
    {
        return Url.Replace("%s", WebUtility.UrlEncode(word));
    }

    /// <summary>
    /// Check if a given link belongs to this dictionary.
    /// </summary>
    public abstract bool ValidateUrl(string url);

    /// <summary>
    /// Query the provided word in the dictionary and return a link to the word entry if the word is found.
    /// Return null otherwise.
    /// </summary>
    /// <param name="word">The word to look up.</param>
    /// <returns>A task containing a link to the word entry, or null.</returns>
    [ItemCanBeNull]
    public abstract Task<string> ExecuteQueryAsync(string word);

    public abstract Task<string> GetWordAsync(ChromiumWebBrowser browser);

    public abstract Task<string> GetDescriptionAsync(ChromiumWebBrowser browser);

    public override string ToString()
    {
        return Name;
    }
}
