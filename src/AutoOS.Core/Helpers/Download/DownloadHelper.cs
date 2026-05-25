using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Logging;
using DevWinUI;
using Downloader;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AutoOS.Core.Helpers.Download;

public static partial class DownloadHelper
{
    private static readonly HttpClient httpClient = new()
    {
        DefaultRequestHeaders =
        {
            UserAgent =
            {
                new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
            }
        }
    };

    public static async Task Download(string url, string path, string file = null, IStatusReporter reporter = null)
    {
        if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            string destination = string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file);
            await File.WriteAllTextAsync(destination, await httpClient.GetStringAsync(url), Encoding.UTF8);
            reporter?.Report(progress: 100);
            return;
        }

        DownloadConfiguration config = new()
        {
            MaxTryAgainOnFailure = 5,
            EnableAutoResumeDownload = false,
            ParallelDownload = true,
            ChunkCount = 8,
            ParallelCount = 4,
            RequestConfiguration = new RequestConfiguration
            {
                UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 AutoOS/{ProcessInfoHelper.Version}"
            }
        };

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
            if (e.Error == null)
            {
                reporter?.Report($"{lastSpeedMB:F1} MB/s - {totalSizeMB:F2} MB of {totalSizeMB:F2} MB", 100, false);
            }
        };

        await download.StartAsync();

        string fileName = download.Package?.FileName ?? (!string.IsNullOrEmpty(file) ? Path.Combine(path, file) : null);
        if (!File.Exists(fileName))
        {
            HttpStatusCode? statusCode = null;
            try
            {
                using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead);
                statusCode = response.StatusCode;
            }
            catch { }

            await LogHelper.LogError(new FileNotFoundException(statusCode.HasValue ? $"Downloaded file not found. HTTP Status Code: {(int)statusCode.Value} ({statusCode.Value})" : "Downloaded file not found."));

            if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
            {
                try
                {
                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        using var contentStream = await response.Content.ReadAsStreamAsync();
                        using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                        
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        double clientTotalSizeMB = totalBytes / (1024.0 * 1024.0);
                        var buffer = new byte[8192];
                        int bytesRead;
                        long totalRead = 0;
                        var startTime = DateTime.Now;
                        var clientLastLoggedTime = DateTime.MinValue;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            totalRead += bytesRead;
                            
                            if ((DateTime.Now - clientLastLoggedTime).TotalMilliseconds >= 100)
                            {
                                clientLastLoggedTime = DateTime.Now;
                                double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                                double speedMB = elapsedSeconds > 0 ? (totalRead / (1024.0 * 1024.0)) / elapsedSeconds : 0;
                                double receivedMB = totalRead / (1024.0 * 1024.0);
                                double percentage = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0;
                                reporter?.Report(totalBytes > 0 ? $"{speedMB:F1} MB/s - {receivedMB:F2} MB of {clientTotalSizeMB:F2} MB" : $"{speedMB:F1} MB/s - {receivedMB:F2} MB", percentage, false);
                            }
                        }
                    }
                }
                catch { }
            }

            if (!File.Exists(fileName))
            {
                await LogHelper.LogError(new FileNotFoundException(statusCode.HasValue ? $"Downloaded file not found (HttpClient). HTTP Status Code: {(int)statusCode.Value} ({statusCode.Value})" : "Downloaded file not found (HttpClient)."));
            }
        }

        reporter?.Report(progress: 100, isIndeterminate: true);
    }
}
