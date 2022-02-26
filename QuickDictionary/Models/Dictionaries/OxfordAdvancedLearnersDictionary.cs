using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class OxfordAdvancedLearnersDictionary : Dictionary
{
    protected override string Url => "https://www.oxfordlearnersdictionaries.com/search/english/?q=%s";

    public override PackIconKind Icon => PackIconKind.LetterOBox;

    public override string Name => "Oxford Advanced Learner's Dictionary";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("oxfordlearnersdictionaries.com");
    }

    public override async Task<string> ExecuteQueryAsync(string word)
    {
        string url = await WebUtils.GetUrlAfterRedirect(GetUrl(word));

        return !url.Contains("spellcheck") ? url : null;
    }

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        string headword = await browser.GetInnerTextByXPath(@"//h1[@class='headword']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = await browser.GetInnerTextByXPath(@"//h2[@class='h']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = Regex.Match(browser.Address, @"www\.oxfordlearnersdictionaries\.com\/definition\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
        return headword;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//span[@class='def']");
    }
}
