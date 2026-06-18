using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Logging;
using System.Collections.Concurrent;
using DevWinUI;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Windows.Media.Core;

namespace AutoOS.Core.Helpers.Games;

public static partial class EpicGamesHelper
{
	public static readonly string EpicGamesPath = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe");
	public static readonly string ActiveEpicGamesAccountPath = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", "GameUserSettings.ini");
	public static readonly string EpicGamesAccountDir = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows");
	public static readonly string EpicGamesInstalledGamesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat");
	public static readonly string EpicGamesManifestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
	public static readonly string EpicGamesThirdPartyManifestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "ThirPartyManagedApps");
	public static readonly string EpicGamesInstalledItemsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicOnlineServicesShared", "InstallHelper", "InstalledItems");

	private static readonly HttpClient httpClient = new();
	private static readonly HttpClient loginClient = new();

	private const string ClientId = "34a02cf8f4414e29b15921876da36f9a";

	private const string ClientSecret = "daafbccc737745039dffe53d94fc76cf";

	private const string AesKey = "A09C853C9E95409BB94D707EADEFA52E";

	private const string itemOfferQuery = @"
		query searchStoreQuery(
		$allowCountries: String, $category: String, $comingSoon: Boolean, $count: Int, $country: String!,
		$keywords: String, $locale: String, $namespace: String, $itemNs: String, $sortBy: String,
		$sortDir: String, $start: Int, $tag: String, $releaseDate: String,
		$priceRange: String, $freeGame: Boolean, $onSale: Boolean,
		$effectiveDate: String
		) {
		Catalog {
			searchStore(
			allowCountries: $allowCountries, category: $category, comingSoon: $comingSoon, count: $count,
			country: $country, keywords: $keywords, locale: $locale, namespace: $namespace,
			itemNs: $itemNs, sortBy: $sortBy, sortDir: $sortDir, releaseDate: $releaseDate,
			start: $start, tag: $tag, priceRange: $priceRange, freeGame: $freeGame, onSale: $onSale,
			effectiveDate: $effectiveDate
			) {
			elements {
				id
			}
			}
			}
		}
		";

	private const string ratingQuery = @"
		query getProductResult($sandboxId: String!, $locale: String!) {
			RatingsPolls {
				getProductResult(sandboxId: $sandboxId, locale: $locale) {
					averageRating
				}
			}
		}";


	private const string tagQuery = @"
		query getCatalogOffer($sandboxId: String!, $offerId: String!) {
			Catalog {
				catalogOffer(namespace: $sandboxId, id: $offerId) {
					tags {
						id
						name
						groupName
					}
				}
			}
		}";

	public class EpicAccountInfo
	{
		public string DisplayName { get; set; }
		public string AccountId { get; set; }
		public bool IsActive { get; set; }
	}

	public static List<EpicAccountInfo> GetEpicGamesAccounts()
	{
		List<EpicAccountInfo> accounts = [];

		if (!File.Exists(EpicGamesPath) || !Directory.Exists(EpicGamesAccountDir))
			return accounts;

		// get all configs
		foreach (var file in Directory.GetFiles(EpicGamesAccountDir, "GameUserSettings.ini", System.IO.SearchOption.AllDirectories))
		{
			// check if data is valid
			if (!ValidateData(file))
				continue;

			var (accountId, displayName, _, _) = GetAccountData(file);

			// update config if accountids match
			string accountDir = Path.Combine(EpicGamesAccountDir, accountId);
			if (Directory.Exists(accountDir))
			{
				if (File.Exists(ActiveEpicGamesAccountPath) && file != ActiveEpicGamesAccountPath)
				{
					if (GetAccountData(ActiveEpicGamesAccountPath).AccountId == accountId)
					{
						File.Copy(ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
					}
				}
			}
			// backup config if not already
			else
			{
				// create folder
				Directory.CreateDirectory(accountDir);

				// copy config
				File.Copy(file, Path.Combine(accountDir, "GameUserSettings.ini"), true);

				// create reg file
				File.WriteAllText(Path.Combine(accountDir, "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");
			}

			if (!accounts.Any(x => x.AccountId == accountId))
			{
				accounts.Add(new EpicAccountInfo
				{
					DisplayName = displayName,
					AccountId = accountId,
					IsActive = file == ActiveEpicGamesAccountPath
				});
			}
		}

		return [.. accounts.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)];
	}

	public static bool ValidateData(string file)
	{
		var (_, _, token, _) = GetAccountData(file);

		return !string.IsNullOrWhiteSpace(token);
	}

	public static string Decrypt(string base64)
	{
		byte[] keyBytes = Encoding.ASCII.GetBytes(AesKey);
		using var aes = Aes.Create();
		aes.KeySize = keyBytes.Length * 8;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.PKCS7;

		aes.Key = keyBytes;

		byte[] cipher = Convert.FromBase64String(base64);
		using var ms = new MemoryStream(cipher);
		using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
		using var sr = new StreamReader(cs, Encoding.UTF8);
		//using var sr = new StreamReader(cs, Encoding.GetEncoding("windows-1252"));
		string result = sr.ReadToEnd();
		return result;
	}

	public static string Encrypt(string plainText)
	{
		byte[] keyBytes = Encoding.ASCII.GetBytes(AesKey);
		using var aes = Aes.Create();
		aes.KeySize = keyBytes.Length * 8;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.PKCS7;

		aes.Key = keyBytes;

		byte[] plain = Encoding.UTF8.GetBytes(plainText);
		//byte[] plain = Encoding.GetEncoding("windows-1252").GetBytes(plainText);

		using var ms = new MemoryStream();
		using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
		{
			cs.Write(plain, 0, plain.Length);
			cs.FlushFinalBlock();
		}
		return Convert.ToBase64String(ms.ToArray());
	}

	public static (string AccountId, string DisplayName, string Token, int TokenUseCount) GetAccountData(string file)
	{
		try
		{
			var iniHelper = new InIHelper(file);
			string decryptedOffline = Decrypt(iniHelper.ReadValue("Data", "Offline", 2048));
			string decryptedRememberMe = Decrypt(iniHelper.ReadValue("Data", "RememberMe", 2048));

			var rememberMeRoot = JsonDocument.Parse(decryptedRememberMe.TrimEnd('\0')).RootElement[0];
			string displayName = rememberMeRoot.GetProperty("DisplayName").GetString();
			string token = rememberMeRoot.GetProperty("Token").GetString();
			int tokenUseCount = rememberMeRoot.GetProperty("TokenUseCount").GetInt32();

			var offlineArray = JsonDocument.Parse(decryptedOffline.TrimEnd('\0')).RootElement;
			string accountId = null;

			foreach (var account in offlineArray.EnumerateArray())
			{
				if (account.TryGetProperty("Email", out var emailProp) && emailProp.GetString() == rememberMeRoot.GetProperty("Email").GetString())
				{
					accountId = account.GetProperty("UserID").GetString();
					break;
				}
			}

			return (accountId, displayName, token, tokenUseCount);
		}
		catch
		{
			return (null, null, null, 0);
		}
	}

	public static async Task<string> Exchange()
	{
		try
		{
			string AccessToken = await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

			if (AccessToken == null)
				return null;

			loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

			var exchangeUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/exchange";
			var exchangeFallbackUrl = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/exchange";
			HttpResponseMessage response;
			try
			{
				response = await loginClient.GetAsync(exchangeUrl);
			}
			catch (Exception)
			{
				try
				{
					response = await loginClient.GetAsync(exchangeFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					await LogHelper.LogError(fallbackEx, null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null;
				}
			}

			if (!response.IsSuccessStatusCode)
			{
				try
				{
					response = await loginClient.GetAsync(exchangeFallbackUrl);
				}
				catch (Exception)
				{
					await LogHelper.LogError(new HttpRequestException($"Exchange request failed with status {response.StatusCode}"), null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null;
				}

				if (!response.IsSuccessStatusCode)
				{
					await LogHelper.LogError(new HttpRequestException($"Exchange request failed with status {response.StatusCode}"), null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null;
				}
			}

			var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			return responseJson.RootElement.GetProperty("code").GetString();
		}
		catch (Exception ex)
		{
			await LogHelper.LogError(ex, null, "Failed to exchange Epic Games token");
			return null;
		}
	}

	public static async Task<string> UpdateEpicGamesToken(string file)
	{
		// close epic games launcher
		CloseEpicGames();

		// add needed encoding options
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		// read old data
		var iniHelper = new InIHelper(file);
		string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

		// decrypt it
		string decryptedFull = Decrypt(rememberMeData);
		string decryptedJson = decryptedFull.Contains('\0') ? decryptedFull[..decryptedFull.IndexOf('\0')] : decryptedFull;
		string trailingData = decryptedFull.Contains('\0') ? decryptedFull[decryptedFull.IndexOf('\0')..] : "";
		JsonArray jsonArray = JsonNode.Parse(decryptedJson).AsArray();

		// get old refresh token
		string oldRefreshToken = jsonArray[0]["Token"].GetValue<string>();

		// authenticate
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

		var content = new FormUrlEncodedContent(
		[
			new KeyValuePair<string, string>("grant_type", "refresh_token"),
			new KeyValuePair<string, string>("refresh_token", oldRefreshToken),
			new KeyValuePair<string, string>("token_type", "eg1"),
		]);

		var authUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token";
		var authFallbackUrl = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
		HttpResponseMessage response;
		try
		{
			response = await httpClient.PostAsync(authUrl, content);
		}
		catch (Exception)
		{
			try
			{
				response = await httpClient.PostAsync(authFallbackUrl, content);
			}
			catch (Exception fallbackEx)
			{
				await LogHelper.LogError(fallbackEx, null, $"Failed to update Epic Games token from both {authUrl} and {authFallbackUrl}");
				return null;
			}
		}

		if (!response.IsSuccessStatusCode)
		{
			try
			{
				response = await httpClient.PostAsync(authFallbackUrl, content);
			}
			catch (Exception)
			{
				await LogHelper.LogError(new HttpRequestException($"Auth request failed with status {response.StatusCode}"), null, $"Failed to update Epic Games token from both {authUrl} and {authFallbackUrl}");
				return null;
			}

			if (!response.IsSuccessStatusCode)
			{
				await LogHelper.LogError(new HttpRequestException($"Auth request failed with status {response.StatusCode}"), null, $"Failed to update Epic Games token from both {authUrl} and {authFallbackUrl}");
				return null;
			}
		}

		var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		string newDisplayName = responseJson.RootElement.GetProperty("displayName").GetString();
		string newAccessToken = responseJson.RootElement.GetProperty("access_token").GetString();
		string newRefreshToken = responseJson.RootElement.GetProperty("refresh_token").GetString();

		// replace old display name and refresh token with new data
		jsonArray[0]["DisplayName"] = newDisplayName;
		jsonArray[0]["Token"] = newRefreshToken;

		// write changes
		var options = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		string updatedJson = jsonArray.ToJsonString(options);
		string reencrypted = Encrypt(updatedJson + trailingData);
		reencrypted = rememberMeData.StartsWith("\"") && rememberMeData.EndsWith("\"") ? $"\"{reencrypted}\"" : reencrypted;

		iniHelper.AddValue("Data", $"\"{reencrypted}\"", "RememberMe");
		new InIHelper(Path.Combine(EpicGamesAccountDir, GetAccountData(ActiveEpicGamesAccountPath).AccountId, "GameUserSettings.ini")).AddValue("Data", $"\"{reencrypted}\"", "RememberMe");

		return newAccessToken;
	}

	public static void CloseEpicGames()
	{
		foreach (var name in new[] { "EpicGamesLauncher", "EpicWebHelper" })
		{
			Process.GetProcessesByName(name).ToList().ForEach(process =>
			{
				try
				{
					process.Kill();
					process.WaitForExit(2000);
				}
				catch { }
			});
		}
	}

	public static void DisableMinimizeToTray(string file)
	{
		var (accountId, _, _, _) = GetAccountData(file);

		var iniHelper = new InIHelper(file);

		iniHelper.AddValue("MinimiseToSystemTray", "False", accountId + "_General");
	}

	public static void DisableNotifications(string file)
	{
		var (accountId, _, _, _) = GetAccountData(file);

		var iniHelper = new InIHelper(file);

		iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", accountId + "_General");
		iniHelper.AddValue("NotificationsEnabled_Adverts", "False", accountId + "_General");
	}

	public static void AddPlaytime(string artifactId, DateTime startTime, Action<string, string> onPlayTimeUpdated = null)
	{
		var url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}";
		var endTime = DateTime.UtcNow;
		var startTimeUtc = startTime.ToUniversalTime();

		string startTimeStr = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
		string endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

		var payload = new PlaytimePayload(
			Guid.NewGuid().ToString(),
			artifactId,
			startTimeStr,
			endTimeStr,
			true,
			true
		);

		var json = JsonSerializer.Serialize(payload, PlaytimeJsonContext.Default.PlaytimePayload);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		HttpResponseMessage response;
		try
		{
			response = loginClient.PutAsync(url, content).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			LogHelper.LogError(ex, null, $"Failed to submit playtime to {url}").GetAwaiter().GetResult();
			return;
		}

		if (response.IsSuccessStatusCode)
		{
			var duration = endTime - startTimeUtc;

			var playTimeUrl = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/artifact/{artifactId}";
			HttpResponseMessage playTimeResponse;
			try
			{
				playTimeResponse = loginClient.GetAsync(playTimeUrl).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				LogHelper.LogError(ex, null, $"Failed to get playtime from {playTimeUrl}").GetAwaiter().GetResult();
				return;
			}

			if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
			{
				return;
			}

			var playTimeJson = JsonNode.Parse(playTimeResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
			var newTotalTime = playTimeJson?["totalTime"]?.GetValue<int>() ?? 0;
			var ts = TimeSpan.FromSeconds(newTotalTime);
			var formattedTime = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
			onPlayTimeUpdated?.Invoke(artifactId, formattedTime);
		}
	}

	public static async Task ImportAccount(IStatusReporter reporter = null)
	{
		// get all configs from other drives
		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFiles = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.SelectMany(d =>
			{
				string usersPath = Path.Combine(d.Name, "Users");
				if (!Directory.Exists(usersPath)) return [];

				return Directory.GetDirectories(usersPath)
					.Select(userDir =>
						File.Exists(Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini"))
						? Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini")
						: Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini")
					)
					.Where(File.Exists);
			})
			.Select(path => new FileInfo(path))
			.ToList();

		string newestFilePath = null;

		// check if files are valid
		foreach (var file in foundFiles)
		{
			string configContent = await File.ReadAllTextAsync(file.FullName);
			Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

			if (ValidateData(file.FullName))
			{
				// use the latest one
				if (newestFilePath == null || file.LastWriteTime > new FileInfo(newestFilePath).LastWriteTime)
				{
					// copy the file
					Directory.CreateDirectory(Path.GetDirectoryName(ActiveEpicGamesAccountPath)!);
					File.Copy(file.FullName, ActiveEpicGamesAccountPath, true);

					// disable tray and notifications
					DisableMinimizeToTray(ActiveEpicGamesAccountPath);
					DisableNotifications(ActiveEpicGamesAccountPath);

					// get accountId
					string accountId = GetAccountData(file.FullName).AccountId;

					// create backup folder
					Directory.CreateDirectory(Path.Combine(EpicGamesAccountDir, accountId));

					// copy config
					File.Copy(ActiveEpicGamesAccountPath, Path.Combine(EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

					// create reg file
					File.WriteAllText(Path.Combine(Path.Combine(EpicGamesAccountDir, accountId), "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");

					// update refresh token
					await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

					// update the backed up config
					File.Copy(file.FullName, Path.Combine(EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

					reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");

					await Task.Delay(1000);

					return;
				}
			}
		}
	}

	public static async Task ImportGames()
	{
		// get the newest install list from other drives
		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFiles = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
			.Where(File.Exists)
			.Select(path => new FileInfo(path))
			.OrderByDescending(f => f.LastWriteTime)
			.ToList();

		if (foundFiles.Count == 0)
			return;

		FileInfo newestFile = foundFiles.First();
		string oldDrive = Path.GetPathRoot(newestFile.FullName);

		var jsonContent = await File.ReadAllTextAsync(newestFile.FullName);

		if (string.IsNullOrWhiteSpace(jsonContent))
			return;

		var jsonObject = JsonNode.Parse(jsonContent);

		// return if install list is empty
		if (jsonObject?["InstallationList"] is not JsonArray installationList || installationList.Count == 0)
			return;

		// check and set new game paths in LauncherInstalled.dat
		foreach (var game in installationList)
		{
			if (game is JsonObject gameObj && gameObj.ContainsKey("InstallLocation"))
			{
				string originalPath = gameObj["InstallLocation"]!.ToString();
				string relativePath = originalPath[Path.GetPathRoot(originalPath).Length..];

				foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
				{
					if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
					{
						gameObj["InstallLocation"] = drive.Name[0] + originalPath[1..];
						break;
					}
				}
			}
		}

		// write updated install list to new drive
		Directory.CreateDirectory(Path.GetDirectoryName(EpicGamesInstalledGamesPath)!);
		await File.WriteAllTextAsync(EpicGamesInstalledGamesPath, jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

		// copy manifests folder to new drive
		string srcThirdParty = Path.Combine(oldDrive, EpicGamesThirdPartyManifestDir[Path.GetPathRoot(EpicGamesThirdPartyManifestDir).Length..]);
		if (Directory.Exists(srcThirdParty))
			FileSystem.CopyDirectory(srcThirdParty, EpicGamesThirdPartyManifestDir);

		string srcManifest = Path.Combine(oldDrive, EpicGamesManifestDir[Path.GetPathRoot(EpicGamesManifestDir).Length..]);
		if (Directory.Exists(srcManifest))
		{
			// set new game paths in manifests
			FileSystem.CopyDirectory(srcManifest, EpicGamesManifestDir);
			foreach (var file in Directory.GetFiles(EpicGamesManifestDir, "*.item", System.IO.SearchOption.AllDirectories))
			{
				var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file));

				if (itemJson is JsonObject itemObj && itemObj.ContainsKey("InstallLocation"))
				{
					string originalPath = itemObj["InstallLocation"]!.ToString();
					string relativePath = originalPath[Path.GetPathRoot(originalPath)!.Length..];

					foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
					{
						if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
						{
							// store found drive
							char newDrive = drive.Name[0];

							// update install location
							itemObj["InstallLocation"] = newDrive + originalPath[1..];

							// update other paths
							foreach (var prop in new[] { "ManifestLocation", "StagingLocation", "CompleteManifestPath", "PendingManifestPath" })
							{
								if (itemObj.ContainsKey(prop) && itemObj[prop]!.ToString() is string val && val.Length >= 2 && val[1] == ':')
									itemObj[prop] = newDrive + val[1..];
							}

							// write updated manifest files
							await File.WriteAllTextAsync(file, itemObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
							break;
						}
					}
				}
			}
		}

		// copy install dir to new drive
		string srcInstalled = Path.Combine(oldDrive, EpicGamesInstalledItemsDir[Path.GetPathRoot(EpicGamesInstalledItemsDir).Length..]);
		if (Directory.Exists(srcInstalled))
		{
			// set new game paths in installed items manifests
			FileSystem.CopyDirectory(srcInstalled, EpicGamesInstalledItemsDir, true);
			foreach (var file in Directory.GetFiles(EpicGamesInstalledItemsDir, "*.egi", System.IO.SearchOption.AllDirectories))
			{
				var egiJson = JsonNode.Parse(await File.ReadAllTextAsync(file));

				if (egiJson is JsonObject egiObj && egiObj["v4"] is JsonObject v4Obj)
				{
					string originalPath = v4Obj["dir"]!.ToString();
					string relativePath = originalPath[Path.GetPathRoot(originalPath)!.Length..];

					foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
					{
						if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
						{
							// store found drive
							char newDrive = drive.Name[0];

							// update dir
							v4Obj["dir"] = newDrive + originalPath[1..];

							// update other paths
							foreach (var prop in new[] { "metaDir", "manifestPath", "pendingManifestPath" })
							{
								if (v4Obj.ContainsKey(prop) && v4Obj[prop]!.ToString() is string val && val.Length >= 2 && val[1] == ':')
									v4Obj[prop] = newDrive + val[1..];
							}

							// write updated egi files as a single line
							await File.WriteAllTextAsync(file, egiObj.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
							break;
						}
					}
				}
			}
		}
	}

	public static async Task EpicGamesLogin(IStatusReporter reporter = null)
	{
		// launch epic games launcher
		Process.Start(EpicGamesPath);

		// check when logged in
		while (true)
		{
			if (File.Exists(ActiveEpicGamesAccountPath))
			{
				if (ValidateData(ActiveEpicGamesAccountPath))
				{
					await Task.Delay(1000);

					// close epic games launcher
					CloseEpicGames();

					// disable tray and notifications
					DisableMinimizeToTray(ActiveEpicGamesAccountPath);
					DisableNotifications(ActiveEpicGamesAccountPath);

					reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");
					break;
				}
			}

			if (Process.GetProcessesByName("EpicGamesLauncher").Length == 0)
			{
				// disable tray and notifications
				DisableMinimizeToTray(ActiveEpicGamesAccountPath);
				DisableNotifications(ActiveEpicGamesAccountPath);
				break;
			}

			await Task.Delay(500);
		}

		await Task.Delay(1000);
	}

	public static async Task UpdateInvalidEpicGamesToken(IStatusReporter reporter = null)
	{
		reporter?.SetTitle("The refresh token is no longer valid. Please enter your password again...");

		// close epic games launcher
		CloseEpicGames();

		// delay
		await Task.Delay(500);

		// launch epic games launcher
		Process.Start(EpicGamesPath);

		// check when logged in
		while (true)
		{
			if (File.Exists(ActiveEpicGamesAccountPath))
			{
				if (ValidateData(ActiveEpicGamesAccountPath))
				{
					break;
				}
			}

			await Task.Delay(500);
		}

		// close epic games launcher
		CloseEpicGames();

		// disable tray and notifications
		DisableMinimizeToTray(ActiveEpicGamesAccountPath);
		DisableNotifications(ActiveEpicGamesAccountPath);

		reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");

		await Task.Delay(1000);
	}

	public static async Task<List<GameModel>> GetGames()
	{
		var games = new ConcurrentBag<GameModel>();

		if (File.Exists(EpicGamesPath) && (Directory.Exists(EpicGamesManifestDir) || Directory.Exists(EpicGamesThirdPartyManifestDir)))
		{
			// get access token
			string AccessToken = await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

			if (AccessToken == null)
			{
				await LogHelper.LogError(new UnauthorizedAccessException("Failed to retrieve the Epic Games access token."));
				return [.. games];
			}

			loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

			// get library data
			var libraryData = new List<JsonNode>();
			string nextCursor = null;

			do
			{
				var url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/items?includeMetadata=true&platform=Windows";
				if (nextCursor != null)
					url += $"&cursor={nextCursor}";

				JsonNode json;
				try
				{
					json = JsonNode.Parse(await loginClient.GetStringAsync(url));
				}
				catch (Exception ex)
				{
					await LogHelper.LogError(ex, null, $"Failed to load library data from {url}");
					break;
				}

				var records = json?["records"]?.AsArray();
				if (records != null)
					libraryData.AddRange(records);

				nextCursor = json?["responseMetadata"]?["nextCursor"]?.GetValue<string>();

			} while (!string.IsNullOrEmpty(nextCursor));

			// get build data
			JsonArray buildData = null;
			var buildUrl = "https://launcher-public-service-prod.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live";
			var buildFallbackUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live";
			HttpResponseMessage buildResponse;
			try
			{
				buildResponse = await loginClient.GetAsync(buildUrl);
			}
			catch (Exception)
			{
				try
				{
					buildResponse = await loginClient.GetAsync(buildFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					await LogHelper.LogError(fallbackEx, null, $"Failed to load build data from both {buildUrl} and {buildFallbackUrl}");
					buildResponse = null;
				}
			}

			if (buildResponse != null && !buildResponse.IsSuccessStatusCode)
			{
				try
				{
					buildResponse = await loginClient.GetAsync(buildFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					await LogHelper.LogError(fallbackEx, null, $"Failed to load build data from both {buildUrl} and {buildFallbackUrl}");
					buildResponse = null;
				}
			}

			if (buildResponse != null && buildResponse.IsSuccessStatusCode)
				buildData = JsonNode.Parse(await buildResponse.Content.ReadAsStringAsync()) as JsonArray;

			// get playtime data
			Dictionary<string, int> playTimeData = null;
			var playTimeUrl = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/all";
			HttpResponseMessage playTimeResponse;
			try
			{
				playTimeResponse = await loginClient.GetAsync(playTimeUrl);
			}
			catch (Exception ex)
			{
				await LogHelper.LogError(ex, null, $"Failed to load playtime data from {playTimeUrl}");
				playTimeResponse = null;
			}

			if (playTimeResponse != null && playTimeResponse.IsSuccessStatusCode)
			{
				playTimeData = (JsonNode.Parse(await playTimeResponse.Content.ReadAsStringAsync()) as JsonArray)?.ToDictionary(
					p => p["artifactId"]?.GetValue<string>(),
					p => p["totalTime"]?.GetValue<int>() ?? 0
				);
			}

			string region = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToUpper();
			string ratingKey = region switch
			{
				"AU" => "ACB",
				"BR" => "ClassInd",
				"KR" => "GRAC",
				"DE" => "USK",
				"US" or "CA" => "ESRB",
				_ => "PEGI"
			};

			var manifestFiles = Directory.Exists(EpicGamesManifestDir) ? Directory.GetFiles(EpicGamesManifestDir, "*.item", System.IO.SearchOption.TopDirectoryOnly).ToList() : [];

			var allManifests = new List<JsonNode>();
			foreach (var file in manifestFiles)
			{
				var node = JsonNode.Parse(File.ReadAllText(file));
				allManifests.Add(node);
			}

			if (Directory.Exists(EpicGamesThirdPartyManifestDir))
			{
				foreach (var file in Directory.GetFiles(EpicGamesThirdPartyManifestDir, "*.json"))
				{
					var json = JsonNode.Parse(File.ReadAllText(file));

					string installLocation = null;

					using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(json["RegistryPath"]?.GetValue<string>()))
					{
						if (key != null)
						{
							installLocation = key.GetValue(json["RegistryKey"]?.GetValue<string>())?.ToString()?.TrimEnd('\\', '/');
						}
					}

					if (Directory.Exists(installLocation))
					{
						string provider = json["Provider"]?.GetValue<string>();

						string gameId = null;
						if (provider == "UbisoftConnect")
						{
							gameId = json["GameID"]?.GetValue<string>();
							provider = "Ubisoft Connect";
						}

						allManifests.Add(new JsonObject
						{
							["Provider"] = provider,
							["bIsApplication"] = true,
							["CatalogItemId"] = json["CatalogID"]?.GetValue<string>(),
							["CatalogNamespace"] = json["Namespace"]?.GetValue<string>(),
							["AppName"] = json["AppName"]?.GetValue<string>(),
							["DisplayName"] = json["Title"]?.GetValue<string>(),
							["InstallLocation"] = installLocation,
							["GameID"] = gameId,
							["LaunchExecutable"] = json["MainWindowProcessName"]?.GetValue<string>(),
							["ProcessNames"] = json["ProcessNames"]?.AsArray().DeepClone()
						});
					}
				}
			}

			// for each manifest
			await Parallel.ForEachAsync(allManifests, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (itemJson, _) =>
			{
				try
				{
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					var token = cts.Token;

					// return if not a game
					if (itemJson?["bIsApplication"]?.GetValue<bool>() != true) return;
					var appCategories = itemJson?["AppCategories"]?.AsArray();
					if (appCategories == null || !appCategories.Any(children => children?.GetValue<string>()?.Equals("games", StringComparison.OrdinalIgnoreCase) == true)) return;
					string catalogItemId = itemJson["CatalogItemId"]?.GetValue<string>();
					string catalogNamespace = itemJson["CatalogNamespace"]?.GetValue<string>();
					string appName = itemJson["AppName"]?.GetValue<string>();

					if (catalogItemId == "1e8bda5cfbb641b9a9aea8bd62285f73")
						appName = itemJson["MainGameAppName"]?.GetValue<string>();

					// return if not in library
					if (!libraryData.Any(x => x?["catalogItemId"]?.ToString() == catalogItemId))
						return;

					string installLocation = itemJson["InstallLocation"]?.GetValue<string>()?.Replace("/", "\\");
					if (!Directory.Exists(installLocation))
						return;

					// get offer id
					JsonNode itemOfferData = null;
					try
					{
						itemOfferData = JsonNode.Parse(await httpClient.GetStringAsync($"https://api.egdata.app/items/{catalogItemId}/offer", token).ConfigureAwait(false));
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load offer data for game {itemJson?["DisplayName"]?.ToString()}, {catalogItemId}, https://api.egdata.app/items/{catalogItemId}/offer");
						return;
					}

					var offerId = itemOfferData?["id"]?.GetValue<string>();

					if (catalogItemId == "4fe75bbc5a674f4f9b356b5c90567da5")
					{
						offerId = "09176f4ff7564bbbb499bbe20bd6348f";
					}
					else if (catalogItemId == "1e8bda5cfbb641b9a9aea8bd62285f73")
					{
						offerId = "38a7aa439bad42fa8c708bf80e47ed8b";
					}
					else if (catalogItemId == "d398f3033c5e4b90b09dcbb4b962be80")
					{
						offerId = "e880a70ecac84bc185fea9d354a157cc";
					}

					// get offer id
					//var itemOfferTask = loginClient.PostAsync("https://graphql.unrealengine.com/ue/graphql", new StringContent(JsonSerializer.Serialize(new { query = itemOfferQuery, variables = new { allowCountries = "US", country = "US", locale = "en-US", count = 1, withPrice = true, withPromotions = true, sortBy = "releaseDate", sortDir = "DESC", @namespace = itemJson["CatalogNamespace"]?.GetValue<string>(), category = "games/edition/base" } }), Encoding.UTF8, "application/json"), token);

					//var itemOfferData = JsonNode.Parse(await (await itemOfferTask.ConfigureAwait(false)).Content.ReadAsStringAsync(token).ConfigureAwait(false));

					//string offerId;

					//if (itemOfferData?["data"]?["Catalog"]?["searchStore"]?["elements"] is JsonArray { Count: > 0 })
					//{
					//    offerId = itemOfferData?["data"]?["Catalog"]?["searchStore"]?["elements"]?[0]?["id"]?.GetValue<string>();
					//}
					//else
					//{
					//    itemOfferData = JsonNode.Parse(await httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}/offer", token).ConfigureAwait(false));
					//    offerId = itemOfferData?["id"]?.GetValue<string>();
					//}

					// get metadata
					//var itemTask = httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}", token);
					//var offerTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}", token);
					string manifestUrl = $"https://catalog-public-service-prod.ol.epicgames.com/catalog/api/shared/namespace/{catalogNamespace}/bulk/items?id={catalogItemId}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US";
					string manifestFallbackUrl = $"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/namespace/{catalogNamespace}/bulk/items?id={catalogItemId}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US";
					string offerUrl = $"https://catalog-public-service-prod.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US";
					string offerFallbackUrl = $"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US";
					string ratingUrl = $"https://api.egdata.app/offers/{offerId}/polls";
					string genresUrl = $"https://api.egdata.app/offers/{(offerId == "6e02cab6e82243858462ba7f93c82e9d" ? "d546d9a3e9fe4ba093d3a3fdae020760" : offerId)}/genres";
					string featuresUrl = $"https://api.egdata.app/offers/{(offerId == "6e02cab6e82243858462ba7f93c82e9d" ? "d546d9a3e9fe4ba093d3a3fdae020760" : offerId)}/features";
					string ageRatingUrl = $"https://api.egdata.app/offers/{offerId}/age-rating";
					string mediaUrl = $"https://api.egdata.app/offers/{offerId}/media";

					JsonNode manifestData = null;
					JsonNode offerData = null;
					JsonNode ratingData = null;
					JsonNode genresData = null;
					JsonNode featuresData = null;
					JsonNode ageRatingData = null;
					JsonNode mediaData = null;

					try
					{
						manifestData = JsonNode.Parse(await loginClient.GetStringAsync(manifestUrl, token).ConfigureAwait(false));
					}
					catch (Exception)
					{
						try
						{
							manifestData = JsonNode.Parse(await loginClient.GetStringAsync(manifestFallbackUrl, token).ConfigureAwait(false));
						}
						catch (Exception fallbackEx)
						{
							await LogHelper.LogError(fallbackEx, null, $"Failed to load manifest data for game {itemJson?["DisplayName"]?.ToString()}, {catalogItemId}, both {manifestUrl} and {manifestFallbackUrl}");
							return;
						}
					}

					try
					{
						offerData = JsonNode.Parse(await loginClient.GetStringAsync(offerUrl, token).ConfigureAwait(false));
					}
					catch (Exception)
					{
						try
						{
							offerData = JsonNode.Parse(await loginClient.GetStringAsync(offerFallbackUrl, token).ConfigureAwait(false));
						}
						catch (Exception fallbackEx)
						{
							await LogHelper.LogError(fallbackEx, null, $"Failed to load offer data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, both {offerUrl} and {offerFallbackUrl}");
							return;
						}
					}

					try
					{
						ratingData = JsonNode.Parse(await httpClient.GetStringAsync(ratingUrl, token).ConfigureAwait(false));
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load rating data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, {ratingUrl}");
						ratingData = JsonNode.Parse("{}");
					}

					try
					{
						genresData = JsonNode.Parse(await httpClient.GetStringAsync(genresUrl, token).ConfigureAwait(false));
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load genres data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, {genresUrl}");
						genresData = JsonNode.Parse("[]");
					}

					try
					{
						featuresData = JsonNode.Parse(await httpClient.GetStringAsync(featuresUrl, token).ConfigureAwait(false));
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load features data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, {featuresUrl}");
						featuresData = JsonNode.Parse("{}");
					}

					try
					{
						ageRatingData = JsonNode.Parse(await httpClient.GetStringAsync(ageRatingUrl, token).ConfigureAwait(false));
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load age rating data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, {ageRatingUrl}");
						ageRatingData = JsonNode.Parse("{}");
					}

					try
					{
						var mediaResponse = await httpClient.GetAsync(mediaUrl, token).ConfigureAwait(false);
						mediaData = mediaResponse.IsSuccessStatusCode ? JsonNode.Parse(await mediaResponse.Content.ReadAsStringAsync(token).ConfigureAwait(false)) : JsonNode.Parse("{}");
					}
					catch (Exception ex)
					{
						await LogHelper.LogError(ex, null, $"Failed to load media data for game {itemJson?["DisplayName"]?.ToString()}, {offerId}, {mediaUrl}");
						mediaData = JsonNode.Parse("{}");
					}

					// get images
					//var itemModified = DateTime.TryParse(itemData["lastModifiedDate"]?.GetValue<string>(), out var itemDate) ? itemDate : DateTime.MinValue;
					//var offerModified = DateTime.TryParse(offerData["lastModifiedDate"]?.GetValue<string>(), out var offerDate) ? offerDate : DateTime.MinValue;

					//string imageTallUrl, imageWideUrl;

					//if (itemModified > offerModified)
					//{
					//    var itemImages = itemData["keyImages"]?.AsArray() ?? [];
					//    imageTallUrl = itemImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBoxTall")?["url"]?.GetValue<string>();
					//    imageWideUrl = itemImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBox")?["url"]?.GetValue<string>();
					//}
					//else
					//{
					//    var offerImages = offerData["keyImages"]?.AsArray() ?? [];
					//    imageTallUrl = offerImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "OfferImageTall")?["url"]?.GetValue<string>();
					//    imageWideUrl = offerImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "OfferImageWide")?["url"]?.GetValue<string>();
					//}

					// get description
					string description = offerData[offerId]?["description"]?.GetValue<string>();

					if (offerData[offerId]?["offerType"]?.GetValue<string>() != "BASE_GAME")
					{
						description = manifestData[catalogItemId]?["description"]?.GetValue<string>();
					}

					// get key images
					var keyImages = manifestData[catalogItemId]?["keyImages"]?.AsArray() ?? [];

					// get artifactid
					//string artifactId = itemData?["releaseInfo"]?[0]?["appId"]?.ToString();
					string artifactId = manifestData[catalogItemId]?["releaseInfo"]?[0]?["appId"]?.ToString();

					// read playtime json data
					var totalSeconds = playTimeData?.GetValueOrDefault(artifactId) ?? 0;

					var ts = TimeSpan.FromSeconds(totalSeconds);
					string playTime = ts.TotalHours >= 1
						? $"{(int)ts.TotalHours}h {ts.Minutes}m"
						: $"{ts.Minutes}m";

					// get latest version
					string currentVersion = itemJson["AppVersionString"]?.GetValue<string>();
					string latestVersion = buildData?.FirstOrDefault(x => x?["appName"]?.ToString() == itemJson["AppName"]?.GetValue<string>())?["buildVersion"]?.ToString();

					if (string.IsNullOrEmpty(currentVersion))
						currentVersion = latestVersion;

					DateTimeOffset releaseDate = DateTimeOffset.Parse(offerData[offerId]!["releaseDate"]!.GetValue<string>()!);

					long? sizeBytes = itemJson["InstallSize"]?.GetValue<long>();

					if (!sizeBytes.HasValue)
						sizeBytes = new DirectoryInfo(installLocation).EnumerateFiles("*", System.IO.SearchOption.AllDirectories).Sum(fi => fi.Length);

					games.Add(new GameModel
					{
						Launcher = itemJson["Provider"]?.GetValue<string>() ?? "Epic Games",
						CatalogNamespace = catalogNamespace,
						CatalogItemId = catalogItemId,
						AppName = appName,
						InstallLocation = installLocation,
						LaunchCommand = itemJson["LaunchCommand"]?.GetValue<string>(),
						LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>()?.Replace("/", "\\"),
						GameID = itemJson["GameID"]?.GetValue<string>(),
						ProcessNames = itemJson["ProcessNames"]?.AsArray().Select(p => p.GetValue<string>()).ToList(),
						ArtifactId = artifactId,
						UpdateIsAvailable = latestVersion != null && latestVersion != currentVersion,
						ImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBoxTall")?["url"]?.GetValue<string>(),
						BackgroundImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBox")?["url"]?.GetValue<string>(),
						Title = offerData[offerId]?["title"]?.GetValue<string>(),
						Developers = offerData[offerId]?["seller"]?["name"]?.GetValue<string>(),
						Genres = genresData?.AsArray()?.Select(g => g?["name"]?.GetValue<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [],
						Features = featuresData?["features"]?.AsArray()?.Select(f => f?.GetValue<string>()).Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? [],
						Rating = ratingData["averageRating"]?.GetValue<double?>() ?? 0.0,
						PlayTime = playTime,
						AgeRatingUrl = ageRatingData[ratingKey]?["ratingImage"]?.ToString(),
						AgeRatingTitle = ageRatingData[ratingKey]?["title"]?.ToString(),
						AgeRatingDescription = ageRatingData[ratingKey]?["descriptor"]?.ToString()?.Replace(",", ", "),
						Elements = ageRatingData[ratingKey]?["element"]?.ToString()?.Replace(",", ", "),
						Description = description,
						Screenshots = [.. (mediaData["images"]?.AsArray().Select(img => img["src"]?.ToString()).Where(src => !string.IsNullOrWhiteSpace(src)) ?? [])],
						ReleaseDate = releaseDate.ToString("d"),
						Size = sizeBytes >= 1_000_000_000 ? $"{sizeBytes.Value / 1_000_000_000d:F1} GB" : $"{sizeBytes.Value / 1_000_000d:F2} MB",
						Version = currentVersion
					});
				}
				catch (Exception ex)
				{
					await LogHelper.LogError(ex, null);
				}
			});
		}
		return [.. games];
	}
}

internal record PlaytimePayload(
	[property: JsonPropertyName("machineId")] string MachineId,
	[property: JsonPropertyName("artifactId")] string ArtifactId,
	[property: JsonPropertyName("startTime")] string StartTime,
	[property: JsonPropertyName("endTime")] string EndTime,
	[property: JsonPropertyName("startSegment")] bool StartSegment,
	[property: JsonPropertyName("endSegment")] bool EndSegment
);

[JsonSerializable(typeof(PlaytimePayload))]
internal partial class PlaytimeJsonContext : JsonSerializerContext { }
