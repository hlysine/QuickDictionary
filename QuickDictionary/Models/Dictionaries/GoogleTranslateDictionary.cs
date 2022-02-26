using System;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class GoogleTranslateDictionary : Dictionary
{
    protected override string Url => "https://translate.google.com/#view=home&op=translate&sl=en&tl=zh-TW&text=%s";

    public override PackIconKind Icon => PackIconKind.GoogleTranslate;

    public override string Name => "Google Translate";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("translate.google.com");
    }

    public override Task<string> ExecuteQueryAsync(string word)
    {
        return Task.FromResult(GetUrl(word));
    }

    public override Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        return Task.FromResult<string>(null);
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//div[contains(@class,'J0lOec')]");
    }
}
