using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Windows.Media.Core;

namespace AutoOS.Helpers.Games;

public static class EpicGamesHelper
{
    public static readonly string EpicGamesPath = File.Exists(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe") ? @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe" : @"C:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe";

    public static readonly string ActiveEpicGamesAccountPath = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", "GameUserSettings.ini");

    public static readonly string EpicGamesAccountDir = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows");

    public const string EpicGamesInstalledGamesPath = @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";

    public const string EpicGamesManifestDir = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

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

        if (!File.Exists(EpicGamesPath))
            return accounts;

        // get all configs
        foreach (var file in Directory.GetFiles(EpicGamesAccountDir, "GameUserSettings.ini", SearchOption.AllDirectories))
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

    public static void CloseEpicGames()
    {
        foreach (var name in new[] { "EpicGamesLauncher", "EpicWebHelper" })
        {
            Process.GetProcessesByName(name).ToList().ForEach(p =>
            {
                p.Kill();
                p.WaitForExit();
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

            var response = await loginClient.GetAsync("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/exchange");

            if (!response.IsSuccessStatusCode)
                return null;

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return responseJson.RootElement.GetProperty("code").GetString();
        }
        catch
        {
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
        string decryptedJson = decryptedFull.Contains('\0') ? decryptedFull.Substring(0, decryptedFull.IndexOf('\0')) : decryptedFull;
        string trailingData = decryptedFull.Contains('\0') ? decryptedFull.Substring(decryptedFull.IndexOf('\0')) : "";
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

        var response = await httpClient.PostAsync("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token", content);

        if (!response.IsSuccessStatusCode)
            return null;

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

    public static void AddPlaytime(string artifactId, DateTime startTime)
    {
        var url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}";
        var endTime = DateTime.UtcNow;

        string startTimeStr = startTime.ToString("o");
        string endTimeStr = endTime.ToString("o");

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

        using var response = loginClient.PutAsync(url, content).GetAwaiter().GetResult();

        if (response.IsSuccessStatusCode)
        {
            var duration = endTime - startTime;

            var playTimeResponse = loginClient
                .GetAsync($"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/artifact/{artifactId}")
                .GetAwaiter().GetResult();

            if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return;
            }

            var playTimeJson = JsonNode.Parse(playTimeResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var newTotalTime = playTimeJson?["totalTime"]?.GetValue<int>() ?? 0;

            GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                var existingItem = GamesPage.Instance.Games.Items
                    .OfType<Views.Settings.Games.HeaderCarouselItem>()
                    .FirstOrDefault(item => item.ArtifactId == artifactId);

                if (existingItem != null)
                {
                    var ts = TimeSpan.FromSeconds(newTotalTime);
                    var formattedTime = ts.TotalHours >= 1
                        ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                        : $"{ts.Minutes}m";

                    existingItem.PlayTime = formattedTime;

                    if (GamesPage.Instance.Games.ArtifactId == artifactId)
                    {
                        GamesPage.Instance.Games.PlayTime = formattedTime;
                    }
                }
            });
        }
        else
        {
            var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
    }

    public static async Task LoadGames()
    {
        if (File.Exists(EpicGamesPath) && Directory.Exists(EpicGamesManifestDir))
        {
            // remove previous games
            foreach (var item in GamesPage.Instance.Games.Items.OfType<Views.Settings.Games.HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games").ToList())
                GamesPage.Instance.Games.Items.Remove(item);

            // get access token
            string AccessToken = await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

            if (AccessToken == null)
                return;

            loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            // get library data
            var libraryData = new List<JsonNode>();
            string nextCursor = null;

            do
            {
                var url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/items?includeMetadata=true&platform=Windows";
                if (nextCursor != null)
                    url += $"&cursor={nextCursor}";

                var json = JsonNode.Parse(await loginClient.GetStringAsync(url));

                var records = json?["records"]?.AsArray();
                if (records != null)
                    libraryData.AddRange(records);

                nextCursor = json?["responseMetadata"]?["nextCursor"]?.GetValue<string>();

            } while (!string.IsNullOrEmpty(nextCursor));

            // get build data
            var buildResponse = await loginClient.GetAsync("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live");

            if (buildResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return;
            }

            var buildData = JsonNode.Parse(await buildResponse.Content.ReadAsStringAsync()) as JsonArray;

            // get playtime data
            var playTimeResponse = await loginClient.GetAsync($"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/all");

            if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return;
            }

            var playTimeData = (JsonNode.Parse(await playTimeResponse.Content.ReadAsStringAsync()) as JsonArray)?.ToDictionary(
                p => p["artifactId"]?.GetValue<string>(),
                p => p["totalTime"]?.GetValue<int>() ?? 0
            );

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

            var manifestFiles = Directory.GetFiles(EpicGamesManifestDir, "*.item", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).ToList();

            var latestManifests = new Dictionary<string, FileInfo>();

            foreach (var file in manifestFiles)
            {
                var json = JsonNode.Parse(File.ReadAllText(file.FullName));
                var manifestHash = json?["ManifestHash"]?.GetValue<string>();
                if (string.IsNullOrEmpty(manifestHash))
                    continue;

                if (!latestManifests.ContainsKey(manifestHash))
                {
                    latestManifests[manifestHash] = file;
                }
                else
                {
                    if (file.LastWriteTime > latestManifests[manifestHash].LastWriteTime)
                    {
                        File.Delete(latestManifests[manifestHash].FullName);
                        latestManifests[manifestHash] = file;
                    }
                    else
                    {
                        File.Delete(file.FullName);
                    }
                }
            }

            // for each manifest
            await Parallel.ForEachAsync(latestManifests.Values, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (file, _) =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var token = cts.Token;

                    // read manifest
                    var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file.FullName).ConfigureAwait(false));

                    // return if not a game
                    if (itemJson?["bIsApplication"]?.GetValue<bool>() != true) return;
                    string catalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>();
                    string catalogNamespace = itemJson["CatalogNamespace"]?.GetValue<string>();

                    // return if not in library
                    if (!libraryData.Any(x => x?["catalogItemId"]?.ToString() == catalogItemId))
                        return;

                    string installLocation = itemJson["InstallLocation"]?.GetValue<string>()?.Replace("/", "\\");
                    if (!Directory.Exists(installLocation))
                        return;

                    // get offer id
                    var itemOfferData = JsonNode.Parse(await httpClient.GetStringAsync($"https://api.egdata.app/items/{catalogItemId}/offer", token).ConfigureAwait(false));
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
                    var manifestTask = loginClient.GetStringAsync($"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/namespace/{catalogNamespace}/bulk/items?id={catalogItemId}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US", token);
                    var offerTask = loginClient.GetStringAsync($"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US", token);
                    var ratingTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/polls", token);
                    var genresTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/genres", token);
                    var featuresTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/features", token);
                    var ageRatingTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/age-rating", token);
                    var mediaTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/media", token);

                    await Task.WhenAll(manifestTask, offerTask, ratingTask, genresTask, featuresTask, ageRatingTask, mediaTask).ConfigureAwait(false);

                    //var itemData = JsonNode.Parse(await itemTask.ConfigureAwait(false));
                    //var offerData = JsonNode.Parse(await offerTask.ConfigureAwait(false));
                    var manifestData = JsonNode.Parse(await manifestTask.ConfigureAwait(false));
                    var offerData = JsonNode.Parse(await offerTask.ConfigureAwait(false));
                    var ratingData = JsonNode.Parse(await ratingTask.ConfigureAwait(false));
                    var genresData = JsonNode.Parse(await genresTask.ConfigureAwait(false));
                    var featuresData = JsonNode.Parse(await featuresTask.ConfigureAwait(false));
                    var ageRatingData = JsonNode.Parse(await ageRatingTask.ConfigureAwait(false));
                    var mediaData = JsonNode.Parse(await mediaTask.ConfigureAwait(false));

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

                    DateTimeOffset releaseDate = DateTimeOffset.Parse(offerData[offerId]!["releaseDate"]!.GetValue<string>()!);

                    long? sizeBytes = itemJson["InstallSize"]?.GetValue<long>();

                    GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                    {
                        GamesPage.Instance.Games.Items.Add(new Views.Settings.Games.HeaderCarouselItem
                        {
                            Launcher = "Epic Games",
                            CatalogNamespace = catalogNamespace,
                            CatalogItemId = catalogItemId,
                            AppName = itemJson["MainGameAppName"]?.GetValue<string>(),
                            InstallLocation = installLocation,
                            LaunchCommand = itemJson["LaunchCommand"]?.GetValue<string>(),
                            LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>()?.Replace("/", "\\"),
                            ProcessNames = itemJson["ProcessNames"]?.AsArray().Select(p => p.GetValue<string>()).ToList(),
                            ArtifactId = artifactId,
                            UpdateIsAvailable = latestVersion != null && latestVersion != currentVersion,
                            //ImageUrl = imageTallUrl,
                            //BackgroundImageUrl = imageWideUrl,
                            ImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBoxTall")?["url"]?.GetValue<string>(),
                            BackgroundImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBox")?["url"]?.GetValue<string>(),
                            //Title = offerData["title"]?.GetValue<string>(),
                            //Developers = offerData["seller"]?["name"]?.GetValue<string>(),
                            Title = offerData[offerId]?["title"]?.GetValue<string>(),
                            Developers = offerData[offerId]?["seller"]?["name"]?.GetValue<string>(),
                            Genres = genresData?.AsArray()?.Select(g => g?["name"]?.GetValue<string>())
                                                .Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [],
                            Features = featuresData?["features"]?.AsArray()?.Select(f => f?.GetValue<string>())
                                                  .Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? [],
                            Rating = ratingData["averageRating"]?.GetValue<double?>() ?? 0.0,
                            PlayTime = playTime,
                            AgeRatingUrl = ageRatingData[ratingKey]?["ratingImage"]?.ToString(),
                            AgeRatingTitle = ageRatingData[ratingKey]?["title"]?.ToString(),
                            AgeRatingDescription = ageRatingData[ratingKey]?["descriptor"]?.ToString()?.Replace(",", ", "),
                            Elements = ageRatingData[ratingKey]?["element"]?.ToString()?.Replace(",", ", "),
                            //Description = offerData["description"]?.GetValue<string>(),
                            Description = description,
                            Screenshots = [.. mediaData["images"]
                                    .AsArray()
                                    .Select(img => img["src"]?.ToString())
                                    .Where(src => !string.IsNullOrWhiteSpace(src))],
                            //Videos = [.. mediaData["videos"]
                            //            .AsArray()
                            //            .SelectMany(video => video["outputs"].AsArray())
                            //            .Where(output =>
                            //                output["contentType"]?.ToString() == "video/webm" &&
                            //                output["key"]?.ToString() == "low" &&
                            //                !string.IsNullOrWhiteSpace(output["url"]?.ToString()))
                            //            .Select(output => MediaSource.CreateFromUri(new Uri(output["url"].ToString())))],
                            ReleaseDate = releaseDate.ToString("d"),
                            Size = sizeBytes >= 1_000_000_000
                                ? $"{sizeBytes.Value / 1_000_000_000d:F1} GB"
                                : $"{sizeBytes.Value / 1_000_000d:F2} MB",
                            Version = currentVersion,
                            Width = 240,
                            Height = 320,
                        });
                    });
                }
                catch (Exception ex)
                {
                    await App.ShowErrorMessage(new Exception($"Failed to load game: {JsonNode.Parse(await File.ReadAllTextAsync(file.FullName).ConfigureAwait(false))["DisplayName"]?.ToString()}", ex));
                }
            });
        }
    }
}

internal record PlaytimePayload(
    string machineId,
    string artifactId,
    string startTime,
    string endTime,
    bool startSegment,
    bool endSegment
);

[JsonSerializable(typeof(PlaytimePayload))]
internal partial class PlaytimeJsonContext : JsonSerializerContext { }