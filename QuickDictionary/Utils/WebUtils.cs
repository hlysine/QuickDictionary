using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace QuickDictionary.Utils;

public static class WebUtils
{
    public static async Task<HttpWebResponse> GetResponseAfterRedirect(string url)
    {
        HttpWebRequest req = WebRequest.CreateHttp(url);

        try
        {
            req.Method = "HEAD";
            req.AllowAutoRedirect = true;
            return (HttpWebResponse)await req.GetResponseAsync();
        }
        catch (WebException ex)
        {
            return ex.Response as HttpWebResponse ?? throw ex;
        }
    }

    public static async Task<string> GetUrlAfterRedirect(string url)
    {
        return (await GetResponseAfterRedirect(url)).ResponseUri.AbsoluteUri;
    }

    public static async Task<HttpStatusCode> GetStatusCodeAfterRedirect(string url)
    {
        return (await GetResponseAfterRedirect(url)).StatusCode;
    }

    public static string ToJSLiteral(this string input)
    {
        using var writer = new StringWriter();
        using var provider = CodeDomProvider.CreateProvider("JScript");
        provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
        return writer.ToString();
    }

    public static async Task<string> GetInnerTextByXPath(this ChromiumWebBrowser browser, params string[] xpath)
    {
        var sb = new StringBuilder();
        sb.Append(@"(function() { return ");

        for (int i = 0; i < xpath.Length; i++)
        {
            sb.Append(@"document.evaluate(" + xpath[i].ToJSLiteral() + @", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.innerText");

            if (i != xpath.Length - 1)
                sb.Append(@" + '\r\n' + ");
        }

        sb.Append(@";})();");
        JavascriptResponse result = await browser.EvaluateScriptAsync(sb.ToString());

        if (result.Success)
            return result.Result as string;

        return null;
    }
}
