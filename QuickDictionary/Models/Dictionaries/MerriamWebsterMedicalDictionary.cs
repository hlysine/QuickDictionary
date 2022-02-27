using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class MerriamWebsterMedicalDictionary : Dictionary
{
    protected override string Url => "https://www.merriam-webster.com/medical/%s";

    public override PackIconKind Icon => PackIconKind.MedicalBag;

    public override string Name => "Merriam-Webster Medical Dictionary";

    public override bool ValidateUrl(string url)
    {
        return new Uri(url).Host.Trim().ToLower().Contains("merriam-webster.com");
    }

    public override async Task<string> ExecuteQueryAsync(string word)
    {
        using HttpWebResponse response = await WebUtils.GetResponseAfterRedirect(GetUrl(word));

        return response.StatusCode == HttpStatusCode.OK ? response.ResponseUri.AbsoluteUri : null;

        //var web = new HtmlWeb();
        //var doc = await web.LoadFromWebAsync(GetUrl(word));
        //if (web.StatusCode != System.Net.HttpStatusCode.OK)
        //{
        //    return false;
        //}
        //var failNode1 = doc.DocumentNode.SelectSingleNode(@"//p[contains(@class,""missing-query"")]");
        //if (failNode1 != null) return false;
        //var failNode2 = doc.DocumentNode.SelectSingleNode(@"//div[contains(@class,""no-spelling-suggestions"")]");
        //if (failNode2 != null) return false;
        //return true;
    }

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        string headword = await browser.GetInnerTextByXPath(@"//h1[contains(@class,'hword')]");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = Regex.Match(browser.Address, @"www\.merriam-webster\.com\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
        return headword;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
    {
        return await browser.GetInnerTextByXPath(@"//span[@class='dtText']");
    }
}
