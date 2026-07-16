using PhantomClientCore;

namespace AutoOS.Core.Helpers.Games.Clients;

internal sealed class TlsClient
{
	private static bool _initialized;
	private static readonly Lock InitGate = new();

	private readonly PhantomClient _client;

	public TlsClient(TlsClientOptions options = null)
	{
		options ??= new TlsClientOptions();
		EnsureInitialized();
		_client = new PhantomClient(new PhantomClientOptions
		{
			ClientIdentifier = options.ClientIdentifier,
			Timeout = options.Timeout,
			Proxy = options.Proxy ?? "",
			DefaultHeaders = options.DefaultHeaders,
			HeaderOrder = options.HeaderOrder,
			ForceHttp1 = options.ForceHttp1,
			InsecureSkipVerify = options.InsecureSkipVerify,
			RandomTlsExtensionOrder = options.RandomTlsExtensionOrder,
			CatchPanics = true
		});
	}

	public Task<TlsResponse> GetAsync(string url, IReadOnlyDictionary<string, string> headers = null)
	{
		var opts = headers != null ? new RequestOptions { Headers = new Dictionary<string, string>(headers) } : null;
		return MapResponseAsync(_client.GetAsync(url, opts));
	}

	public Task<TlsResponse> PostAsync(string url, string body, IReadOnlyDictionary<string, string> headers = null)
	{
		var opts = new PostRequestOptions { Body = body };
		if (headers != null)
			opts.Headers = new Dictionary<string, string>(headers);
		return MapResponseAsync(_client.PostAsync(url, opts));
	}

	private static async Task<TlsResponse> MapResponseAsync(Task<PhantomClientCore.TlsResponse> task)
	{
		var r = await task;
		return new TlsResponse
		{
			Status = r.Status,
			Body = r.Body,
			Url = r.Url
		};
	}

	private static void EnsureInitialized()
	{
		if (_initialized)
			return;

		lock (InitGate)
		{
			if (_initialized)
				return;

			PhantomTLS.InitializeAsync().GetAwaiter().GetResult();
			_initialized = true;
		}
	}
}

internal sealed class TlsClientOptions
{
	public string ClientIdentifier { get; set; } = "chrome_120";
	public int Timeout { get; set; } = 30000;
	public string Proxy { get; set; }
	public Dictionary<string, string> DefaultHeaders { get; set; } = new()
	{
		{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
		{ "Accept", "application/json" },
		{ "Accept-Language", "en-US,en;q=0.9" }
	};
	public List<string> HeaderOrder { get; set; }
	public bool RandomTlsExtensionOrder { get; set; } = true;
	public bool InsecureSkipVerify { get; set; }
	public bool ForceHttp1 { get; set; }
}

internal sealed class TlsResponse
{
	public int Status { get; init; }
	public string Body { get; init; }
	public string Url { get; init; }
	public bool IsSuccess => Status is >= 200 and <= 299;
}
