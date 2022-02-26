using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class DictionaryDotCom : Dictionary
{
    protected override string Url => "https://www.dictionary.com/browse/%s";

    public override PackIconKind Icon => PackIconKind.LetterDBox;

    public override string Name => "Dictionary.com";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("dictionary.com");
    }

    public override async Task<string> ExecuteQueryAsync(string word)
    {
        HttpWebResponse response = await WebUtils.GetResponseAfterRedirect(GetUrl(word));

        return response.StatusCode == HttpStatusCode.OK ? response.ResponseUri.AbsoluteUri : null;
    }

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        string headword = await browser.GetInnerTextByXPath(@"//*[@data-first-headword='true']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        Match match = Regex.Match(browser.Address, @"www\.dictionary\.com\/browse\/([^?]+)");

        if (match.Success)
        {
            headword = WebUtility.UrlDecode(match.Groups[1].Value);
            return headword;
        }

        return null;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//*[contains(@class, 'one-click-content')]");
    }
}
