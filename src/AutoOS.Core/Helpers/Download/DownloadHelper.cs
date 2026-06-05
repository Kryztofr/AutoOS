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
			MaxTryAgainOnFailure = 8,
			EnableAutoResumeDownload = true,
			ParallelDownload = true,
			ChunkCount = 4,
			ParallelCount = 4,
			HttpClientTimeout = 300000,
			CheckDiskSizeBeforeDownload = true,
			MinimumChunkSize = 1024 * 1024,
			RequestConfiguration = new RequestConfiguration
			{
				Headers = new WebHeaderCollection
				{
					{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36" },
					{ "Accept", "*/*" },
					{ "Accept-Language", "en-US,en;q=0.9" },
					{ "Connection", "keep-alive" }
				}
			}
		};

		if (url.Contains("dropboxusercontent.com", StringComparison.OrdinalIgnoreCase))
		{
			config.ParallelDownload = false;
			config.ChunkCount = 1;
			config.ParallelCount = 1;
		}

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

			errorDetails.AppendLine($"Primary download failed for: {url}");
			errorDetails.AppendLine($"Package: Status={package?.Status}, SaveComplete={package?.IsSaveComplete}, FileName={package?.FileName}");
			if (downloaderError != null)
				errorDetails.AppendLine($"Downloader error: {downloaderError.GetType().Name}: {downloaderError.Message}");
			var files = Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly) : [];
			errorDetails.AppendLine($"Files in path: [{string.Join(", ", files.Select(Path.GetFileName))}]");

			HttpStatusCode? statusCode = null;
			string contentLength = "", acceptRanges = "", contentRange = "";
			try
			{
				using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
				if (config.RequestConfiguration?.Headers != null)
				{
					foreach (string headerName in config.RequestConfiguration.Headers.AllKeys)
						headRequest.Headers.TryAddWithoutValidation(headerName, config.RequestConfiguration.Headers[headerName]);
				}
				using var response = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
				statusCode = response.StatusCode;
				try { contentLength = response.Content.Headers.ContentLength?.ToString() ?? ""; } catch { }
				try { acceptRanges = response.Headers.AcceptRanges.FirstOrDefault() ?? ""; } catch { }
				try { contentRange = response.Content.Headers.ContentRange?.ToString() ?? ""; } catch { }
			}
			catch { }
			errorDetails.AppendLine(statusCode.HasValue ? $"HTTP Status Code: {(int)statusCode.Value} ({statusCode.Value})" : "HTTP status unknown");
			errorDetails.AppendLine($"Content-Length: {contentLength}, Accept-Ranges: {acceptRanges}, Content-Range: {contentRange}");

			Exception fallbackError = null;
			if (statusCode.HasValue && (int)statusCode.Value >= 200 && (int)statusCode.Value <= 299)
			{
				try
				{
					using var request = new HttpRequestMessage(HttpMethod.Get, url);
					if (config.RequestConfiguration?.Headers != null)
					{
						foreach (string headerName in config.RequestConfiguration.Headers.AllKeys)
							request.Headers.TryAddWithoutValidation(headerName, config.RequestConfiguration.Headers[headerName]);
					}
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
					if (response.IsSuccessStatusCode)
					{
						try { contentLength = response.Content.Headers.ContentLength?.ToString() ?? ""; } catch { }
						try { acceptRanges = response.Headers.AcceptRanges.FirstOrDefault() ?? ""; } catch { }
						try { contentRange = response.Content.Headers.ContentRange?.ToString() ?? ""; } catch { }

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
}