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
        Directory.CreateDirectory(path);

        if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            string destination = string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file);
            await File.WriteAllTextAsync(destination, await httpClient.GetStringAsync(url), Encoding.UTF8);
            reporter?.Report(progress: 100);
            return;
        }

        DownloadConfiguration config = new()
        {
            MaxTryAgainOnFailure = 10,
            EnableAutoResumeDownload = true,
            ParallelDownload = false,
            ChunkCount = 1,
            ParallelCount = 1,
            HttpClientTimeout = 300000,
            CheckDiskSizeBeforeDownload = true,
            MinimumChunkSize = 1024 * 1024,
            RequestConfiguration = new RequestConfiguration
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
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
        Exception downloaderError = null;

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
            else
            {
                downloaderError = e.Error;
            }
        };

        await download.StartAsync();

        string fileName = download.Package?.FileName ?? (!string.IsNullOrEmpty(file) ? Path.Combine(path, file) : null);
        if (!File.Exists(fileName))
        {
            var errorDetails = new StringBuilder();
            var package = download.Package;

            errorDetails.AppendLine($"Download URL: {url}");
            errorDetails.AppendLine(
                $"Package: Status={package?.Status}, SaveComplete={package?.IsSaveComplete}, FileName={package?.FileName}, " +
                $"TotalFileSize={package?.TotalFileSize}, ReceivedBytes={package?.ReceivedBytesSize}, SupportsRange={package?.IsSupportDownloadInRange}");
            if (downloaderError != null)
                errorDetails.AppendLine($"Downloader error: {downloaderError.GetType().Name}: {downloaderError.Message}");
            var files = Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly) : [];
            errorDetails.AppendLine($"Files in path: [{string.Join(", ", files.Select(Path.GetFileName))}]");

            HttpStatusCode? statusCode = null;
            try
            {
                using var headRequest = CreateProbeRequest(HttpMethod.Head, url, config);
                using var response = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
                statusCode = response.StatusCode;
                AppendResponseHeaders(errorDetails, "HEAD", response);
            }
            catch (Exception ex)
            {
                errorDetails.AppendLine($"HEAD probe failed: {ex.GetType().Name}: {ex.Message}");
            }

            if (!statusCode.HasValue)
                errorDetails.AppendLine("HTTP status unknown");
            else
                errorDetails.AppendLine($"HTTP Status Code: {(int)statusCode.Value} ({statusCode.Value})");

            try
            {
                using var rangeRequest = CreateProbeRequest(HttpMethod.Get, url, config);
                rangeRequest.Headers.Range = new RangeHeaderValue(0, 0);
                using var rangeResponse = await httpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead);
                AppendResponseHeaders(errorDetails, "GET Range bytes=0-0", rangeResponse);
            }
            catch (Exception ex)
            {
                errorDetails.AppendLine($"Range probe failed: {ex.GetType().Name}: {ex.Message}");
            }

            Exception fallbackError = null;
            if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
            {
                try
                {
                    using var request = CreateProbeRequest(HttpMethod.Get, url, config);
                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
                        using var contentStream = await response.Content.ReadAsStreamAsync();
                        using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                        
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        double clientTotalSizeMB = totalBytes / (1024.0 * 1024.0);
                        var buffer = new byte[81920];
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
                        
                        await fileStream.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    fallbackError = ex;
                }
            }

            if (File.Exists(fileName))
            {
                errorDetails.AppendLine("Fallback download succeeded");
                await LogHelper.LogError(new Exception(errorDetails.ToString(), downloaderError));
            }
            else
            {
                if (fallbackError != null)
                    errorDetails.AppendLine($"Fallback download error: {fallbackError.GetType().Name}: {fallbackError.Message}");
                else if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
                    errorDetails.AppendLine("Fallback download completed but file still not found");
                else
                    errorDetails.AppendLine("Fallback download not attempted (non-success HTTP status or unknown)");

                await LogHelper.LogError(new FileNotFoundException(errorDetails.ToString(), fileName!, downloaderError));
            }
        }

        reporter?.Report(progress: 100, isIndeterminate: true);
    }

    private static HttpRequestMessage CreateProbeRequest(HttpMethod method, string url, DownloadConfiguration config)
    {
        var request = new HttpRequestMessage(method, url);
        var requestConfig = config.RequestConfiguration;
        if (!string.IsNullOrWhiteSpace(requestConfig?.UserAgent))
            request.Headers.TryAddWithoutValidation("User-Agent", requestConfig.UserAgent);

        if (requestConfig?.Headers == null)
            return request;

        foreach (string headerName in requestConfig.Headers.AllKeys)
            request.Headers.TryAddWithoutValidation(headerName, requestConfig.Headers[headerName]);

        return request;
    }

    private static void AppendResponseHeaders(StringBuilder errorDetails, string probeName, HttpResponseMessage response)
    {
        errorDetails.AppendLine($"{probeName} response: {(int)response.StatusCode} ({response.StatusCode})");
        errorDetails.AppendLine($"  Content-Length: {FormatHeaderValue(response.Content.Headers.ContentLength)}");
        errorDetails.AppendLine($"  Accept-Ranges: {FormatHeaderValue(response.Headers.AcceptRanges)}");
        errorDetails.AppendLine($"  Content-Range: {FormatContentRange(response.Content.Headers.ContentRange)}");
    }

    private static string FormatHeaderValue(object value) =>
        value switch
        {
            null => "(not present)",
            long l => l.ToString(),
            _ => value.ToString() ?? "(not present)"
        };

    private static string FormatContentRange(ContentRangeHeaderValue contentRange)
    {
        if (contentRange == null)
            return "(not present)";

        if (contentRange.HasLength && contentRange.HasRange)
            return $"bytes {contentRange.From}-{contentRange.To}/{contentRange.Length}";

        if (contentRange.HasRange)
            return $"bytes {contentRange.From}-{contentRange.To}/*";

        return contentRange.ToString() ?? "(not present)";
    }
}
