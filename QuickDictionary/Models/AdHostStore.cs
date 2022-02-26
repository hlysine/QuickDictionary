using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickDictionary.Models;

public class AdHostStore
{
    private static AdHostStore instance;

    public string[] AdHosts { get; private set; } = Array.Empty<string>();

    public static AdHostStore Instance => instance ??= new AdHostStore();

    public async Task DownloadHostListAsync()
    {
        var client = new WebClient();
        Stream stream = await client.OpenReadTaskAsync("https://raw.githubusercontent.com/anudeepND/blacklist/master/adservers.txt");
        var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();
        MatchCollection matches = Regex.Matches(content, @"0\.0\.0\.0 (.+)");
        string[] hosts = new string[matches.Count];

        for (int i = 0; i < matches.Count; i++)
            hosts[i] = matches[i].Groups[1].Value;

        AdHosts = hosts.OrderBy(x => x).ToArray();
    }

    public bool IsAdUrl(string url)
    {
        return Array.BinarySearch(AdHosts, new Uri(url).Host) >= 0;
    }
}
