using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class WikipediaDictionary : Dictionary
{
    public override string Url => "https://www.wikipedia.org/wiki/%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("wikipedia.org");

    public override async Task<bool> ValidateQueryAsync(string url, string word)
        => await WebUtils.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        var headword = await browser.GetInnerTextByXPath(@"//div[@class=""page-heading""]");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        var match = Regex.Match(browser.Address, @"wikipedia\.org\/w\/index\.php\?title=([^&]+)");
        if (match.Success)
        {
            headword = WebUtility.UrlDecode(match.Groups[1].Value);
            return headword;
        }

        match = Regex.Match(browser.Address, @"wikipedia\.org\/wiki\/([^?]+)");
        if (match.Success)
        {
            headword = WebUtility.UrlDecode(match.Groups[1].Value);
            return headword;
        }

        return null;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        var res = await browser.GetInnerTextByXPath(@"(//div[@id=""bodyContent""]//p[not(@class)])[1]");
        if (res == null)
            return null;
        return Regex.Replace(res, @"\[\d+\]", "");
    }

    public override PackIconKind Icon => PackIconKind.Wikipedia;

    public override string Name => "Wikipedia";
}