using System;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class GoogleSearchDictionary : Dictionary
{
    public override string Url => "https://www.google.com/search?q=define+%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("www.google.com");

    public override Task<bool> ValidateQueryAsync(string url, string word) => Task.FromResult(true);

    // todo: try web-scraping google search
    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//*[@data-dobid='hdw']");

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//*[@data-dobid='dfn']");

    public override PackIconKind Icon => PackIconKind.Google;

    public override string Name => "Google Dictionary";
}