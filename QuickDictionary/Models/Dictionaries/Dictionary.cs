using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;

namespace QuickDictionary.Models.Dictionaries;

public abstract class Dictionary : NotifyPropertyChanged
{
    // Url to the dictionary with %s in place of the query
    public abstract string Url { get; }

    // Function to check if a URL belongs to this dictionary
    public abstract bool ValidateUrl(string url);

    // Function to validate a query given the query url and query text
    public abstract Task<bool> ValidateQueryAsync(string url, string word);

    public abstract Task<string> GetWordAsync(ChromiumWebBrowser browser);

    public abstract Task<string> GetDescriptionAsync(ChromiumWebBrowser browser);

    // Pack icon in toolbar
    public abstract PackIconKind Icon { get; }

    public abstract string Name { get; }

    public override string ToString()
    {
        return Name;
    }

    public string PrecedenceString => precedence > 0 ? precedence.ToString() : "";

    private int precedence;

    public int Precedence
    {
        get => precedence;
        set => SetAndNotify(ref precedence, value, new[] { nameof(PrecedenceString) });
    }
}