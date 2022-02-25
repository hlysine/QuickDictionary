﻿using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using QuickDictionary.Utils;

namespace QuickDictionary.Models.Dictionaries;

public class DictionaryDotCom : Dictionary
{
    public override string Url => "https://www.dictionary.com/browse/%s";

    public override bool ValidateUrl(string url)
        => new Uri(url).Host.Trim().ToLower().Contains("dictionary.com");

    public override async Task<bool> ValidateQueryAsync(string url, string word)
        => await WebUtils.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;

    public override async Task<string> GetWordAsync(ChromiumWebBrowser browser)
    {
        var headword = await browser.GetInnerTextByXPath(@"//h1[@class=""css-1jzk4d9 e1rg2mtf8""]");
        if (!string.IsNullOrWhiteSpace(headword))
            return headword;
        var match = Regex.Match(browser.Address, @"www\.dictionary\.com\/definition\/[\w-_]+\/([^?]+)");
        if (match.Success)
        {
            headword = WebUtility.UrlDecode(match.Groups[1].Value);
            return headword;
        }

        return null;
    }

    public override async Task<string> GetDescriptionAsync(ChromiumWebBrowser browser)
        => await browser.GetInnerTextByXPath(@"//div[@class=""css-1ghs5zt e1q3nk1v3""]");

    public override PackIconKind Icon => PackIconKind.LetterDBox;

    public override string Name => "Dictionary.com";
}