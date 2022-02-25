using System;
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
    public static async Task<string> GetFinalRedirectAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var maxRedirCount = 8;  // prevent infinite loops
        var newUrl = url;
        do
        {
            HttpWebRequest req;
            HttpWebResponse resp = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                resp = (HttpWebResponse)await req.GetResponseAsync();
                switch (resp.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return newUrl;
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.RedirectKeepVerb:
                    case HttpStatusCode.RedirectMethod:
                        newUrl = resp.Headers["Location"];
                        if (newUrl == null)
                            return url;

                        if (newUrl.IndexOf("://", StringComparison.Ordinal) == -1)
                        {
                            // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                            var u = new Uri(new Uri(url), newUrl);
                            newUrl = u.ToString();
                        }
                        break;
                    default:
                        return newUrl;
                }
                url = newUrl;
            }
            catch (WebException)
            {
                // Return the last known good URL
                return newUrl;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }
        } while (maxRedirCount-- > 0);

        return newUrl;
    }

    public static async Task<HttpStatusCode> GetFinalStatusCodeAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return HttpStatusCode.NotFound;

        var maxRedirectCount = 8;  // prevent infinite loops
        var statusCode = HttpStatusCode.NotFound;
        do
        {
            HttpWebRequest req;
            HttpWebResponse resp = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                resp = (HttpWebResponse)await req.GetResponseAsync();
                statusCode = resp.StatusCode;
                string newUrl;

                switch (resp.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return statusCode;
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.RedirectKeepVerb:
                    case HttpStatusCode.RedirectMethod:
                        newUrl = resp.Headers["Location"];
                        if (newUrl == null)
                            return statusCode;

                        if (newUrl.IndexOf("://", StringComparison.Ordinal) == -1)
                        {
                            // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                            var u = new Uri(new Uri(url), newUrl);
                            newUrl = u.ToString();
                        }
                        break;
                    default:
                        return statusCode;
                }
                url = newUrl;
            }
            catch (WebException)
            {
                // Return the last known good URL
                return statusCode;
            }
            catch (Exception)
            {
                return statusCode;
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }
        } while (maxRedirectCount-- > 0);

        return statusCode;
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
        for (var i = 0; i < xpath.Length; i++)
        {
            sb.Append(@"document.evaluate(" + xpath[i].ToJSLiteral() + @", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.innerText");
            if (i != xpath.Length - 1)
            {
                sb.Append(@" + '\r\n' + ");
            }
        }
        sb.Append(@";})();");
        var result = await browser.EvaluateScriptAsync(sb.ToString());
        if (result.Success)
        {
            return result.Result as string;
        }

        return null;
    }
}