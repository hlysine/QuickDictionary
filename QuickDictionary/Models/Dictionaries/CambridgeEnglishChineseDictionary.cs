using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class CambridgeEnglishChineseDictionary : Dictionary
{
    public override string Url => "https://dictionary.cambridge.org/search/english-chinese-traditional/direct/?source=gadgets&q=%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("cambridge.org");

    public override async Task<bool> ValidateQueryAsync(string url, string word)
    {
        string finalUrl = await WebUtils.GetFinalRedirectAsync(url);
        return !finalUrl.Contains("spellcheck") && !finalUrl.EndsWith("english-chinese-traditional/");
    }

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        var headword = await browser.GetInnerTextByXPath(@"//span[contains(@class,'headword')]");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = Regex.Match(browser.Address, @"dictionary\.cambridge\.org\/dictionary\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
        return headword;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//div[contains(@class,'def ddef_d')]", @"//span[contains(@class,'trans dtrans')]");

    public override PackIconKind Icon => PackIconKind.LetterCBox;

    public override string Name => "Cambridge English-Chinese Dictionary";
}