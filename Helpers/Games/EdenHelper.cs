using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace AutoOS.Helpers.Games;

public static partial class EdenHelper
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    private static readonly HttpClient httpClient = new();

    public static async Task LoadGames()
    {
        // if paths defined
        if (localSettings.Values["EdenLocation"] is string exePath && localSettings.Values["EdenDataLocation"] is string dataPath && File.Exists(exePath) && Directory.Exists(dataPath))
        {
            // download switch game catalog
            string filePath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json");

            if (!File.Exists(filePath))
            {
                // to do: set switchpresenter text to downloading switch game catalog
                var content = await httpClient.GetStringAsync("https://raw.githubusercontent.com/blawar/titledb/refs/heads/master/US.en.json");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await File.WriteAllTextAsync(filePath, content);
            }

            // remove previous games
            foreach (var item in GamesPage.Instance.Games.Items.OfType<Views.Settings.Games.HeaderCarouselItem>().Where(item => item.Launcher == "Eden").ToList())
                GamesPage.Instance.Games.Items.Remove(item);
            
            // get game list
            using var stream = File.OpenRead(Path.Combine(localSettings.Values["EdenDataLocation"]?.ToString(), "cache", "game_list", "game_metadata_cache.json"));
            var config = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream);

            // read json database
            using var fs = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json"));
            using var doc = await JsonDocument.ParseAsync(fs);

            var jsonById = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in doc.RootElement.EnumerateObject())
            {
                if (kvp.Value.TryGetProperty("id", out var idElem))
                {
                    var key = idElem.GetString()?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(key))
                        jsonById.TryAdd(key, kvp.Value);
                }
            }

            if (config.TryGetValue("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)

            await Parallel.ForEachAsync(entries.EnumerateArray(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (edenEntry, _) =>
            {
                // check if game id exists in database
                string id = "0" + edenEntry.GetProperty("program_id").GetString();
                if (!jsonById.TryGetValue(id, out var entry)) 
                    return;

                // get name from database
                string name = entry.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

                // clean name for searching
                if (string.IsNullOrWhiteSpace(name)) return;
                string cleanName = CleanNameRegex().Replace(name.Replace('’', '\''), "");

                // get install location
                string installLocation = edenEntry.GetProperty("file_path").GetString();

                // get playtime
                string playTime = "0m";

                byte[] playtimeData = File.Exists(Path.Combine(localSettings.Values["EdenDataLocation"]?.ToString() ?? "", "play_time", "playtime.bin")) ? File.ReadAllBytes(Path.Combine(localSettings.Values["EdenDataLocation"]?.ToString() ?? "", "play_time", "playtime.bin")) : Directory.Exists(Path.Combine(localSettings.Values["EdenDataLocation"]?.ToString() ?? "", "play_time")) ? Directory.GetFiles(Path.Combine(localSettings.Values["EdenDataLocation"]?.ToString() ?? "", "play_time"), "*.bin").FirstOrDefault() is string f ? File.ReadAllBytes(f) : [] : [];

                for (int offset = 0; offset + 16 <= playtimeData.Length; offset += 16)
                {
                    if (BitConverter.ToUInt64(playtimeData, offset).ToString("x16").Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ulong seconds = BitConverter.ToUInt64(playtimeData, offset + 8);
                        playTime = (seconds / 3600) > 0 ? $"{seconds / 3600}h {(seconds % 3600) / 60}m" : $"{(seconds % 3600) / 60}m";
                        break;
                    }
                }

                // get size
                long sizeBytes = edenEntry.TryGetProperty("file_size", out var sizeElem) ? sizeElem.GetInt64() : 0;

                // search on igdb
                var result = await SearchCovers(cleanName);
                if (result == null) return;

                using var docData = JsonDocument.Parse(await httpClient.GetStringAsync(result["game_url"], _));
                var data = docData.RootElement.Clone();

                GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                {
                    GamesPage.Instance.Games.Items.Add(new Views.Settings.Games.HeaderCarouselItem
                    {
                        Launcher = "Eden",
                        LauncherLocation = localSettings.Values["EdenLocation"]?.ToString(),
                        DataLocation = localSettings.Values["EdenDataLocation"]?.ToString(),
                        GameLocation = installLocation,
                        InstallLocation = Path.GetDirectoryName(installLocation),
                        Title = name,
                        ImageUrl = result["cover_url"],
                        BackgroundImageUrl = entry.GetProperty("bannerUrl").GetString(),
                        Developers = result["developers"],
                        Genres = [.. data.GetProperty("genres").EnumerateArray().Select(g => g.GetProperty("name").GetString())],
                        Features = [.. data.GetProperty("game_modes").EnumerateArray().Select(m => m.GetProperty("name").GetString())],
                        Rating = Math.Round(data.GetProperty("aggregated_rating").GetDouble() / 20.0, 2),
                        PlayTime = playTime,
                        AgeRatingUrl = result["age_rating_url"],
                        AgeRatingTitle = result["age_rating_title"],
                        AgeRatingDescription = result["age_rating_title"] is not null
                                                ? entry.GetProperty("ratingContent")[0].GetString()
                                                : null,
                        Description = data.GetProperty("summary").GetString(),
                        Screenshots = [.. entry.GetProperty("screenshots").EnumerateArray().Select(x => x.GetString())],
                        ReleaseDate = result["release_date"],
                        Size = sizeBytes >= 1024 * 1024 * 1024
                                ? $"{sizeBytes / (1024d * 1024d * 1024d):F1} GB"
                                : $"{sizeBytes / (1024d * 1024d):F2} MB",
                        //Videos = [.. data.GetProperty("videos")
                        //                .EnumerateArray()
                        //                .Select(g => g.TryGetProperty("url", out var url) && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri)
                        //                    ? MediaSource.CreateFromUri(uri)
                        //                    : null)
                        //                .Where(source => source is not null)],
                        Width = 240,
                        Height = 320,
                    });
                });

                //doc.Dispose();
            });
        }
    }

    private static async Task<Dictionary<string, string>> SearchCovers(string name)
    {
        string Clean(string input) => Regex.Replace(input.ToLowerInvariant(), @"\s+", ".");
        string GetSearchBucket(string input)
        {
            string cleaned = Regex.Replace(input.Length >= 2 ? input[..2] : input.ToLowerInvariant(), @"[^a-z\d]", "");
            return string.IsNullOrEmpty(cleaned) ? "@" : cleaned;
        }

        try
        {
            var bucketJson = await httpClient.GetStringAsync($"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/buckets/{GetSearchBucket(Clean(name))}.json");

            var bucketRoot = JsonDocument.Parse(bucketJson).RootElement;

            var matchingIds = new List<string>();
            string cleanName = Clean(name);

            if (bucketRoot.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in bucketRoot.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    if (!property.Value.TryGetProperty("name", out var nameProp))
                        continue;

                    string itemName = nameProp.GetString() ?? "";

                    if (Clean(itemName) == cleanName)
                        matchingIds.Add(property.Name);
                }
            }

            JsonElement? maxGame = null;
            int maxFields = 0;

            foreach (var id in matchingIds)
            {
                using var response = await httpClient.GetAsync($"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/games/{id}.json");

                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync();

                var root = JsonDocument.Parse(json).RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    continue;

                int count = 0;
                foreach (var prop in root.EnumerateObject())
                {
                    var val = prop.Value;
                    if (val.ValueKind == JsonValueKind.Object)
                        continue;

                    if (val.ValueKind == JsonValueKind.Null)
                        continue;

                    if (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString()))
                        continue;

                    count++;
                }

                if (count > maxFields)
                {
                    maxFields = count;
                    maxGame = root.Clone();
                }
            }

            if (maxGame.HasValue && maxGame.Value.TryGetProperty("cover", out var cover) && cover.ValueKind == JsonValueKind.Object && cover.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
            {
                string thumb = url.GetString() ?? "";
                int dot = thumb.LastIndexOf('.');
                int slash = thumb.LastIndexOf('/');

                if (dot >= 0 && slash >= 0)
                {
                    string slug = thumb.Substring(slash + 1, dot - slash - 1);

                    var developers = new List<string>();

                    if (maxGame.HasValue && maxGame.Value.TryGetProperty("involved_companies", out var companies))
                    {
                        foreach (var company in companies.EnumerateArray())
                        {
                            if (company.GetProperty("developer").GetBoolean() &&
                                company.GetProperty("company").TryGetProperty("name", out var nameProp))
                            {
                                var devName = nameProp.GetString();
                                if (!string.IsNullOrWhiteSpace(devName))
                                    developers.Add(devName);
                            }
                        }
                    }

                    string developerNames = developers != null && developers.Any() ? string.Join(", ", developers) : "Unknown";

                    string gameUrl = maxGame is JsonElement { ValueKind: JsonValueKind.Object } game &&
                                        game.TryGetProperty("id", out var id) &&
                                        id.ValueKind == JsonValueKind.Number &&
                                        id.TryGetInt32(out var gameId)
                        ? $"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/games/{gameId}.json"
                        : "";

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

                    string baseUrl = ratingKey.ToLowerInvariant() switch
                    {
                        "pegI" => "https://www.igdb.com/icons/rating_icons/pegi/pegi_",
                        "esrb" => "https://www.igdb.com/icons/rating_icons/esrb/esrb_",
                        "cero" => "https://www.igdb.com/icons/rating_icons/cero/cero_",
                        "usk" => "https://www.igdb.com/icons/rating_icons/usk/usk_",
                        "grac" => "https://www.igdb.com/icons/rating_icons/grac/grac_",
                        "class_ind" => "https://www.igdb.com/icons/rating_icons/class_ind/class_ind_",
                        "acb" => "https://www.igdb.com/icons/rating_icons/acb/acb_",
                        _ => ""
                    };

                    var ratingTitles = ratingKey switch
                    {
                        "PEGI" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            {"3", "PEGI 3"}, {"7", "PEGI 7"}, {"12", "PEGI 12"}, {"16", "PEGI 16"}, {"18", "PEGI 18"},
                        },
                                                "USK" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            {"0", "USK ab 0"}, {"6", "USK ab 6"}, {"12", "USK ab 12"}, {"16", "USK ab 16"}, {"18", "USK ab 18"},
                        },
                                                "ESRB" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            {"ec", "Early Childhood"}, {"e", "Everyone"}, {"e10", "Everyone 10+"}, {"e10+", "Everyone 10+"},
                            {"t", "Teen"}, {"m", "Mature 17+"}, {"ao", "Adults Only 18+"},
                        },
                        _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    };

                    JsonElement? ratingEntry = null;

                    if (maxGame.HasValue && maxGame.Value.TryGetProperty("age_ratings", out var ageRatings))
                    {
                        foreach (var rating in ageRatings.EnumerateArray())
                        {
                            if (string.Equals(
                                rating.GetProperty("organization").GetProperty("name").GetString(),
                                ratingKey,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                ratingEntry = rating;
                                break;
                            }
                        }
                    }

                    // If no matching rating for this region
                    if (ratingEntry is null ||
                        !ratingEntry.Value.TryGetProperty("rating_category", out var ratingCategory) ||
                        !ratingCategory.TryGetProperty("rating", out var ratingValue))
                    {
                        return new Dictionary<string, string>
                        {
                            { "name", maxGame?.GetProperty("name").GetString() },
                            { "game_url", gameUrl },
                            { "cover_url", $"https://images.igdb.com/igdb/image/upload/t_cover_big_2x/{slug}.jpg" },
                            { "developers", developerNames },
                            { "age_rating_url", null },
                            { "age_rating_title", null }
                        };
                    }

                    string ratingCode = ratingValue.GetString();

                    string ratingKeyForUrl = ratingKey == "ESRB" &&
                                                ratingCode.StartsWith("e10+", StringComparison.OrdinalIgnoreCase) ? "e10" : ratingCode;

                    string ratingTitle = ratingTitles.TryGetValue(ratingCode, out var title) ? title : ratingCode;

                    string ratingUrl = $"{baseUrl}{ratingKeyForUrl.ToLowerInvariant()}.png";

                    DateTimeOffset? releaseDate = null;

                    if (maxGame.HasValue && maxGame.Value.TryGetProperty("release_dates", out var releaseDates))
                    {
                        var firstRelease = releaseDates.EnumerateArray().FirstOrDefault();
                        if (firstRelease.ValueKind != JsonValueKind.Undefined &&
                            firstRelease.TryGetProperty("date", out var dateProp) &&
                            dateProp.ValueKind == JsonValueKind.Number)
                        {
                            long unixTime = dateProp.GetInt64();
                            releaseDate = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                        }
                    }

                    return new Dictionary<string, string>
                    {
                        { "name", maxGame?.GetProperty("name").GetString() ?? "Unknown" },
                        { "game_url", gameUrl },
                        { "cover_url", $"https://images.igdb.com/igdb/image/upload/t_cover_big_2x/{slug}.jpg" },
                        { "developers", developerNames },
                        { "age_rating_url", ratingUrl },
                        { "age_rating_title", ratingTitle },
                        { "release_date", releaseDate?.ToString("d") }
                    };
                }
            }
        }
        catch 
        {
            return null;
        }

        return null;
    }

    [GeneratedRegex(@"[^\u0000-\u007F'’]+", RegexOptions.Compiled)]
    private static partial Regex CleanNameRegex();

    [GeneratedRegex(@"[^a-z0-9]", RegexOptions.Compiled)]
    private static partial Regex SimpleCleanNameRegex();
}