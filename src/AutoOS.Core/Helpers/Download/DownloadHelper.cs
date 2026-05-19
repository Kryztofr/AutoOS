using AutoOS.Core.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AutoOS.Core.Helpers.Download;

public static partial class DownloadHelper
{
    private static readonly HttpClient httpClient = new();

    public static async Task Download(string url, string path, string file = null, IStatusReporter reporter = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(path);
        }

        if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            string destination = string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file);
            await File.WriteAllTextAsync(destination, await httpClient.GetStringAsync(url), Encoding.UTF8);
            reporter?.Report(progress: 100);
            return;
        }

        string finalFileName = !string.IsNullOrEmpty(file) ? Path.Combine(path, file) : null;
        if (finalFileName == null)
        {
            throw new ArgumentNullException(nameof(file), "File name must be specified.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (url.Contains("www2.ati.com", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Referrer = new Uri("http://support.amd.com");
            request.Headers.Add("Accept", "*/*");
            request.Headers.UserAgent.ParseAdd("AMD Catalyst Install Manager/0.0");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Connection.Add("Keep-Alive");
        }
        else
        {
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(finalFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalReadBytes = 0;
        int readBytes;
        var stopwatch = Stopwatch.StartNew();
        DateTime lastLoggedTime = DateTime.MinValue;

        while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, readBytes);
            totalReadBytes += readBytes;

            if ((DateTime.Now - lastLoggedTime).TotalMilliseconds >= 100)
            {
                lastLoggedTime = DateTime.Now;
                if (totalBytes > 0)
                {
                    double percentage = (double)totalReadBytes / totalBytes * 100;
                    double speedMB = (totalReadBytes / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                    double receivedMB = totalReadBytes / (1024.0 * 1024.0);
                    double totalSizeMB = totalBytes / (1024.0 * 1024.0);

                    reporter?.Report($"{speedMB:F1} MB/s - {receivedMB:F2} MB of {totalSizeMB:F2} MB", percentage, false);
                }
                else
                {
                    double receivedMB = totalReadBytes / (1024.0 * 1024.0);
                    reporter?.Report($"{receivedMB:F2} MB downloaded", 0, true);
                }
            }
        }

        reporter?.Report(progress: 100, isIndeterminate: true);
    }
}
