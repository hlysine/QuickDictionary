using System;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class GoogleSearchDictionary : Dictionary
{
    protected override string Url => "https://www.google.com/search?q=define+%s";

    public override PackIconKind Icon => PackIconKind.Google;

    public override string Name => "Google Search";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("www.google.com");
    }

    public override Task<string> ExecuteQueryAsync(string word)
    {
        return Task.FromResult(GetUrl(word));
    }

    // todo: try web-scraping google search
    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//*[@data-dobid='hdw']");
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//*[@data-dobid='dfn']");
    }
}
