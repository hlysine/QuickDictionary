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
    protected override string Url => "https://www.wikipedia.org/wiki/%s";

    public override PackIconKind Icon => PackIconKind.Wikipedia;

    public override string Name => "Wikipedia";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("wikipedia.org");
    }

    public override async Task<string> ExecuteQueryAsync(string word)
    {
        using HttpWebResponse response = await WebUtils.GetResponseAfterRedirect(GetUrl(word));

        return response.StatusCode == HttpStatusCode.OK ? response.ResponseUri.AbsoluteUri : null;
    }

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        string headword = await browser.GetInnerTextByXPath(@"//div[@class='page-heading']");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        Match match = Regex.Match(browser.Address, @"wikipedia\.org\/w\/index\.php\?title=([^&]+)");

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
        string res = await browser.GetInnerTextByXPath(@"(//div[@id='bodyContent']//p[not(@class)])[1]");
        if (res == null)
            return null;
        return Regex.Replace(res, @"\[\d+\]", "");
    }
}
