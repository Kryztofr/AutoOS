using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ValveKeyValue;
using AutoOS.Views.Installer;
using AutoOS.Views.Settings;

namespace AutoOS.Helpers.Games;

public static class SteamHelper
{
    public static readonly string SteamDir = (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam")?.GetValue("InstallPath") as string ?? @"C:\Program Files (x86)\Steam").Replace('/', '\\');
    public static readonly string SteamPath = Path.Combine(SteamDir, "steam.exe");
    public static readonly string SteamLibraryPath = Path.Combine(SteamDir, @"steamapps\libraryfolders.vdf");
    public static readonly string SteamLibraryCacheDir = Path.Combine(SteamDir, @"appcache\librarycache");
    public static readonly string SteamLoginUsersPath = Path.Combine(SteamDir, "config", "loginusers.vdf");

    private static readonly HttpClient httpClient = new();

    public class SteamAccountInfo
    {
        public string AccountName { get; set; }
        public bool MostRecent { get; set; }
        public bool AllowAutoLogin { get; set; }
    }

    public static List<SteamAccountInfo> GetSteamAccounts()
    {
        if (!File.Exists(SteamLoginUsersPath))
            return [];

        string content = File.ReadAllText(SteamLoginUsersPath);
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var options = new KVSerializerOptions
        {
            HasEscapeSequences = true,
        };

        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), options);

        return [.. kv.Root.Children
            .Select(c =>
            {
                string steam64Id = c.Key;
                string accountName = c.Value["AccountName"]?.ToString();
                bool mostRecent = c.Value["MostRecent"]?.ToString() == "1";
                bool allowAutoLogin = c.Value["AllowAutoLogin"]?.ToString() == "1";

                if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(steam64Id))
                    return null;

                return new SteamAccountInfo
                {
                    AccountName = accountName,
                    MostRecent = mostRecent,
                    AllowAutoLogin = allowAutoLogin
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x.AccountName, StringComparer.OrdinalIgnoreCase)];
    }

    public static string GetSteam64ID()
    {
        if (!File.Exists(SteamLoginUsersPath))
            return null;

        var options = new KVSerializerOptions
        {
            HasEscapeSequences = true,
        };

        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamLoginUsersPath))), options);
        return kv.Root.Children.FirstOrDefault(c => c.Value["MostRecent"]?.ToString() == "1" && c.Value["AllowAutoLogin"]?.ToString() == "1").Key;
    }

    public static void CloseSteam()
    {
        foreach (var name in new[] { "steam", "steamwebhelper" })
        {
            Process.GetProcessesByName(name).ToList().ForEach(p =>
            {
                p.Kill();
                p.WaitForExit();
            });
        }
    }

    public static async Task SteamLogin()
    {
        // launch steam
        Process.Start(SteamPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(SteamLoginUsersPath))
            {
                var options = new KVSerializerOptions
                {
                    HasEscapeSequences = true,
                };

                if (KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamLoginUsersPath))), options).Root.Children.Any())
                {
                    // close steam
                    CloseSteam();

                    var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamLoginUsersPath))), options);

                    InstallPage.Info.Title = $"Successfully logged in as {kv.Root.Children.Select(c => c.Value["AccountName"]?.ToString()).FirstOrDefault(name => !string.IsNullOrEmpty(name))}...";
                    break;
                }

            }

            if (Process.GetProcessesByName("steam").Length == 0)
            {
                break;
            }

            await Task.Delay(500);
        }

        await Task.Delay(1000);
    }

    public static async Task RunImportSteamGames()
    {
        var options = new KVSerializerOptions { HasEscapeSequences = true };

        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != Path.GetPathRoot(SteamDir))
            .Select(d => Path.Combine(d.Name, "Program Files (x86)", "Steam", "steamapps", "libraryfolders.vdf"))
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        if (foundFiles.Count == 0)
            return;

        var newestFile = foundFiles.First();
        string sourceDrive = Path.GetPathRoot(newestFile.FullName)?.TrimEnd('\\') ?? "";

        string sourceCacheDir = SteamLibraryCacheDir.Replace(Path.GetPathRoot(SteamLibraryCacheDir) ?? "", sourceDrive + @"\");
        string targetCacheDir = SteamLibraryCacheDir;

        Directory.CreateDirectory(targetCacheDir);

        if (Directory.Exists(sourceCacheDir))
        {
            foreach (var dir in Directory.GetDirectories(sourceCacheDir, "*", SearchOption.AllDirectories))
            {
                var targetDir = dir.Replace(sourceCacheDir, targetCacheDir);
                Directory.CreateDirectory(targetDir);
            }

            foreach (var file in Directory.GetFiles(sourceCacheDir, "*.*", SearchOption.AllDirectories))
            {
                var targetFile = file.Replace(sourceCacheDir, targetCacheDir);
                File.Copy(file, targetFile, true);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SteamLibraryPath));

        KVDocument libraryFolderData;
        using (var stream = File.OpenRead(newestFile.FullName))
        {
            libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }

        var sourceEntries = new List<KVObject>();
        foreach (var folder in libraryFolderData.Root.Children)
        {
            var children = folder.Value.Children.ToList();
            var pathNode = children.FirstOrDefault(c => c.Key == "path");
            string pathValue = pathNode.Value?.ToString() ?? "";

            string resolvedPath = pathValue;
            if (pathValue.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = string.Concat(sourceDrive, pathValue.AsSpan(2));
            }

            var entry = new KVObject();
            entry["path"] = new KVObject(resolvedPath);

            foreach (var child in children)
            {
                if (child.Key == "path") continue;
                entry[child.Key] = child.Value;
            }

            sourceEntries.Add(entry);
        }

        var currentDriveEntry = new KVObject();
        currentDriveEntry["path"] = new KVObject(SteamDir);
        currentDriveEntry["label"] = new KVObject("");
        currentDriveEntry["totalsize"] = new KVObject("0");
        currentDriveEntry["update_clean_bytes_tally"] = new KVObject("0");
        currentDriveEntry["time_last_update_verified"] = new KVObject("0");
        currentDriveEntry["apps"] = new KVObject();

        var rootObj = new KVObject();
        rootObj["0"] = currentDriveEntry;

        int nextIndex = 1;
        foreach (var entry in sourceEntries)
        {
            if (string.Equals(entry["path"]?.ToString(), SteamDir, StringComparison.OrdinalIgnoreCase))
                continue;

            rootObj[nextIndex.ToString()] = entry;
            nextIndex++;
        }

        using (var msOut = new MemoryStream())
        {
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, new KVDocument(null, libraryFolderData.Name, rootObj), options);
            msOut.Position = 0;
            File.WriteAllText(SteamLibraryPath, new StreamReader(msOut).ReadToEnd());
        }

        await Task.Delay(1000);
    }

    public static async Task LoadGames()
    {
        // return if either steam or no games are installed
        if (!File.Exists(SteamPath) || !File.Exists(SteamLibraryPath)) return;

        // remove previous games
        foreach (var item in GamesPage.Instance.Games.Items.OfType<Views.Settings.Games.HeaderCarouselItem>().Where(item => item.Launcher == "Steam").ToList())
            GamesPage.Instance.Games.Items.Remove(item);

        string region = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToUpper();

        string ratingKey = region switch
        {
            "AU" => "ACB",
            "BR" => "DEJUS",
            "KR" => "GRAC",
            "DE" => "USK",
            "US" or "CA" => "ESRB",
            _ => "PEGI"
        };

        string ratingBaseUrl = ratingKey switch
        {
            "ACB" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/ACB/",
            "DEJUS" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/DEJUS/",
            "GRAC" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/GRAC/",
            "USK" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/USK/",
            "ESRB" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/ESRB/",
            "PEGI" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/PEGI/",
            _ => ""
        };

        Dictionary<string, string> ratingTitles = ratingKey switch
        {
            "PEGI" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"3", "PEGI 3"},
                {"7", "PEGI 7"},
                {"12", "PEGI 12"},
                {"16", "PEGI 16"},
                {"18", "PEGI 18"},
            },
            "USK" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"0", "USK 0"},
                {"6", "USK 6"},
                {"12", "USK 12"},
                {"16", "USK 16"},
                {"18", "USK 18"},
            },
            "ESRB" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"ec", "Early Childhood"},
                {"e", "Everyone"},
                {"e10", "Everyone 10+" },
                {"e10+", "Everyone 10+"},
                {"t", "Teen"},
                {"m", "Mature 17+"},
                {"ao", "Adults Only 18+"},
            },
            _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        // read libraryfolders.vdf
        var libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(SteamLibraryPath));

        // for each steam install path
        await Parallel.ForEachAsync(libraryFolderData.Root.Children, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (folder, _) =>
        {
            string steamAppsDir = Path.Combine(folder.Value["path"]?.ToString().Replace(@"\\", @"\"), "steamapps");

            // skip if no steamapps directory
            if (!Directory.Exists(steamAppsDir)) return;

            // get installed apps dictionary
            var appsNode = folder.Value.Children.FirstOrDefault(c => c.Key == "apps");
            if (appsNode.Value == null) return;

            foreach (var app in appsNode.Value.Children.ToDictionary(x => int.Parse(x.Key), x => x.Value))
            {
                string gameId = app.Key.ToString();

                // skip steam tools
                if (gameId == "228980") continue;

                try
                {
                    // read game manifest
                    string manifestPath = Path.Combine(steamAppsDir, $"appmanifest_{gameId}.acf");
                    if (!File.Exists(manifestPath)) continue;

                    var appManifestData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(manifestPath));

                    var gameData = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={gameId}&l=english", _)).RootElement.GetProperty(gameId);
                    if (!gameData.TryGetProperty("success", out var success) || !success.GetBoolean()) continue;

                    // get playtime data
                    //var playTimeData = XDocument.Parse(await httpClient.GetStringAsync($"https://steamcommunity.com/profiles/{GetSteam64ID()}/?tab=all&xml=1", _));

                    //string playTime = playTimeData.Descendants("game")
                    //    .Where(game => (string)game.Element("appID") == gameId)
                    //    .Select(game =>
                    //    {
                    //        var hoursStr = (string)game.Element("hoursOnRecord");
                    //        return double.TryParse(hoursStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
                    //            ? $"{(int)h}h {(int)((h - (int)h) * 60)}min"
                    //            : null;
                    //    })
                    //    .FirstOrDefault() ?? "0m";

                    //// get review data
                    //var reviewData = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/appreviews/{gameId}?json=1", _)).RootElement.GetProperty("query_summary");
                    //int totalPositive = reviewData.GetProperty("total_positive").GetInt32();
                    //int totalNegative = reviewData.GetProperty("total_negative").GetInt32();

                    // skip if coming soon
                    bool comingSoon = gameData.GetProperty("data").GetProperty("release_date").GetProperty("coming_soon").GetBoolean();
                    if (comingSoon) continue;

                    // get age rating
                    string rating = null;
                    string descriptors = null;

                    var data = gameData.GetProperty("data");

                    if (data.TryGetProperty("ratings", out var ratings) && ratings.ValueKind == JsonValueKind.Object && ratings.TryGetProperty(ratingKey.ToLowerInvariant(), out var ratingData))
                    {
                        if (ratingData.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind == JsonValueKind.String)
                        {
                            rating = ratingElement.GetString();
                        }

                        if (ratingData.TryGetProperty("descriptors", out var descElement) && descElement.ValueKind == JsonValueKind.String)
                        {
                            descriptors = descElement.GetString()?
                                .Replace("\r\n", ", ")
                                .Replace("\n", ", ")
                                .Replace("\r", ", ");
                        }
                    }

                    string dateStr = gameData.GetProperty("data").GetProperty("release_date").GetProperty("date").GetString();
                    DateTimeOffset.TryParse(dateStr, out var releaseDate);

                    long? sizeBytes = long.TryParse(appManifestData["SizeOnDisk"]?.ToString(), out var result) ? result : null;

                    GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                    {
                        GamesPage.Instance.Games.Items.Add(new Views.Settings.Games.HeaderCarouselItem
                        {
                            Launcher = "Steam",
                            ImageUrl = $"https://cdn.steamstatic.com/steam/apps/{gameId}/library_600x900.jpg",
                            BackgroundImageUrl = $"https://cdn.steamstatic.com/steam/apps/{gameId}/library_hero.jpg",
                            Title = appManifestData["name"]?.ToString(),
                            Developers = string.Join(", ", gameData.GetProperty("data").GetProperty("developers")
                                                       .EnumerateArray().Select(d => d.GetString()).Where(s => !string.IsNullOrWhiteSpace(s))),
                            Genres = [.. gameData.GetProperty("data").GetProperty("genres")
                                            .EnumerateArray()
                                            .Select(g => g.GetProperty("description").GetString())
                                            .Where(s => !string.IsNullOrWhiteSpace(s))],
                            Features = [.. gameData.GetProperty("data").GetProperty("categories")
                                            .EnumerateArray()
                                            .Select(c => c.GetProperty("description").GetString())
                                            .Where(s => !string.IsNullOrWhiteSpace(s))],
                            //Rating = totalPositive + totalNegative > 0
                            //            ? Math.Round(5.0 * totalPositive / (totalPositive + totalNegative), 1)
                            //            : 0.0,
                            //PlayTime = playTime,
                            PlayTime = "0m",
                            AgeRatingUrl = !string.IsNullOrEmpty(rating) ? $"{ratingBaseUrl}{rating.ToLowerInvariant()}.png" : null,
                            AgeRatingTitle = !string.IsNullOrEmpty(rating) ? (ratingTitles.TryGetValue(rating.ToLowerInvariant(), out var title) ? title : rating) : null,
                            AgeRatingDescription = !string.IsNullOrEmpty(descriptors) ? descriptors : null,
                            Description = gameData.GetProperty("data").GetProperty("short_description").GetString(),
                            Screenshots = gameData.GetProperty("data").TryGetProperty("screenshots", out var screenshots)
                                ? [.. screenshots.EnumerateArray()
                                    .Select(s => s.GetProperty("path_thumbnail").GetString())
                                    .Where(s => !string.IsNullOrWhiteSpace(s))]
                                : [],
                            InstallLocation = Path.Combine(steamAppsDir, "common", appManifestData["installdir"]?.ToString()).Replace("/", "\\"),
                            ReleaseDate = releaseDate != default ? releaseDate.ToString("d") : "Unknown",
                            Size = sizeBytes >= 1024 * 1024 * 1024
                                ? $"{sizeBytes.Value / (1024d * 1024d * 1024d):F1} GB"
                                : $"{sizeBytes.Value / (1024d * 1024d):F2} MB",
                            Version = appManifestData["buildid"]?.ToString(),
                            GameID = gameId,
                            Width = 240,
                            Height = 320,
                        });
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load Steam game {gameId}: {ex.Message}");
                }
            }
        });
    }
}