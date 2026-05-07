using AutoOS.Core.Common;
using Downloader;
using System.Net;
using System.Text;

namespace AutoOS.Core.Helpers.Download;

public static class DownloadHelper
{
    private static readonly HttpClient httpClient = new();

    public static async Task Download(string url, string path, string file = null, IStatusReporter reporter = null)
    {
        if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            reporter?.Report("Downloading file from GitHub...", isIndeterminate: true);
            string destination = string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file);
            await File.WriteAllTextAsync(destination, await httpClient.GetStringAsync(url), Encoding.UTF8);
            reporter?.Report("Download complete.", progress: 100);
            return;
        }

        DownloadConfiguration config = new();
        if (url.Contains("www2.ati.com", StringComparison.OrdinalIgnoreCase))
        {
            config.RequestConfiguration = new RequestConfiguration
            {
                Headers = new WebHeaderCollection
                {
                    { "Referer", "http://support.amd.com" },
                    { "Accept", "*/*" },
                    { "User-Agent", "AMD Catalyst Install Manager/0.0" },
                    { "Cache-Control", "no-cache" },
                    { "Connection", "Keep-Alive" }
                }
            };
        }

        var downloadBuilder = DownloadBuilder.New()
            .WithUrl(url)
            .WithDirectory(path)
            .WithFileName(file)
            .WithConfiguration(config);

        var download = downloadBuilder.Build();

        DateTime lastLoggedTime = DateTime.MinValue;
        double lastSpeedMB = 0;
        double totalSizeMB = 0;

        download.DownloadProgressChanged += (sender, e) =>
        {
            if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 100) return;
            lastLoggedTime = DateTime.Now;

            lastSpeedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
            double receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
            totalSizeMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
            double percentage = e.ProgressPercentage;

            reporter?.Report($"{lastSpeedMB:F1} MB/s - {receivedMB:F2} MB of {totalSizeMB:F2} MB", percentage, false);
        };

        download.DownloadFileCompleted += (sender, e) =>
        {
            reporter?.Report($"{lastSpeedMB:F1} MB/s - {totalSizeMB:F2} MB of {totalSizeMB:F2} MB", 100, true);
        };

        await download.StartAsync();

        string finalFileName = download.Package?.FileName ?? (!string.IsNullOrEmpty(file) ? Path.Combine(path, file) : null);
        string downloadFile = finalFileName + ".download";
        
        int retries = 0;
        while (!File.Exists(finalFileName) && retries < 50)
        {
            if (File.Exists(downloadFile))
            {
                try 
				{ 
					File.Move(downloadFile, finalFileName); 
					break; 
				} catch { }
            }
            await Task.Delay(100);
            retries++;
        }
    }
}
