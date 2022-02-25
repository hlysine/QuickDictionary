using System;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class GoogleTranslateDictionary : Dictionary
{
    public override string Url => "https://translate.google.com/#view=home&op=translate&sl=en&tl=zh-TW&text=%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("translate.google.com");

    public override Task<bool> ValidateQueryAsync(string url, string word)
        => Task.FromResult(true);

    public override Task<string> GetWordAsync(ChromiumWebBrowser browser)
        => Task.FromResult<string>(null);

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//div[contains(@class,""J0lOec"")]");

    public override PackIconKind Icon => PackIconKind.GoogleTranslate;

    public override string Name => "Google Translate";
}