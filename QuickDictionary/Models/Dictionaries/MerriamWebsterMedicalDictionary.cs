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
    public override string Url => "https://www.merriam-webster.com/medical/%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("merriam-webster.com");

    public override async Task<bool> ValidateQueryAsync(string url, string word)
    {
        return await WebUtils.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;
        //var web = new HtmlWeb();
        //var doc = await web.LoadFromWebAsync(url);
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
        var headword = await browser.GetInnerTextByXPath(@"//h1[contains(@class,""hword"")]");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        headword = Regex.Match(browser.Address, @"www\.merriam-webster\.com\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
        return headword;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//span[@class=""dtText""]");

    public override PackIconKind Icon => PackIconKind.MedicalBag;

    public override string Name => "Merriam-Webster Medical Dictionary";
}