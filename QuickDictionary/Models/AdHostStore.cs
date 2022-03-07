using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickDictionary.Models;

public class AdHostStore
{
    private const string ad_hosts_link = @"https://raw.githubusercontent.com/anudeepND/blacklist/master/adservers.txt";

    private static readonly string ad_hosts_path = Storage.ToAbsolutePath("ad_hosts.txt");

    private static AdHostStore instance;

    public string[] AdHosts { get; private set; } = Array.Empty<string>();

    public static AdHostStore Instance => instance ??= new AdHostStore();

    public async Task LoadAdHostsAsync()
    {
        // only re-download ad hosts once per day
        bool fileIsValid = false;
        if (File.Exists(ad_hosts_path))
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(ad_hosts_path) < TimeSpan.FromDays(1))
                fileIsValid = true;

        if (!fileIsValid)
            await downloadAdHostsAsync();

        readAdHosts();
    }

    private async Task downloadAdHostsAsync()
    {
        using var client = new WebClient();
        await client.DownloadFileTaskAsync(ad_hosts_link, ad_hosts_path);
    }

    private void readAdHosts()
    {
        string content = File.ReadAllText(ad_hosts_path);
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
