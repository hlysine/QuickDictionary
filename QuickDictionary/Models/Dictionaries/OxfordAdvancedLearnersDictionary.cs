using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class OxfordAdvancedLearnersDictionary : Dictionary
{
    public override string Url => "https://www.oxfordlearnersdictionaries.com/search/english/?q=%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("oxfordlearnersdictionaries.com");

    public override async Task<bool> ValidateQueryAsync(string url, string word)
        => !(await WebUtils.GetFinalRedirectAsync(url)).Contains("spellcheck");

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        var headword = await browser.GetInnerTextByXPath(@"//h1[@class='headword']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = await browser.GetInnerTextByXPath(@"//h2[@class='h']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = Regex.Match(browser.Address, @"www\.oxfordlearnersdictionaries\.com\/definition\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
        return headword;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//span[@class='def']");

    public override PackIconKind Icon => PackIconKind.LetterOBox;

    public override string Name => "Oxford Advanced Learner's Dictionary";
}