using AutoOS.Core.Common;
using System.Text.Json.Nodes;

namespace AutoOS.Core.Helpers.Database;

public static partial class DiscordHelper
{
    public static readonly string DiscordRoamingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord");

    public static async Task ImportAccount(IStatusReporter reporter = null)
    {
        // get all leveldb folders from other drives
        var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        var foundFolders = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
            .SelectMany(d =>
            {
                string usersPath = Path.Combine(d.Name, "Users");
                if (!Directory.Exists(usersPath)) return [];

                return Directory.GetDirectories(usersPath)
                    .Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
                    .Where(Directory.Exists);
            })
            .Select(path => new DirectoryInfo(path))
            .ToList();

        DirectoryInfo newestFolder = null;

        // check if folders contain valid accounts
        foreach (var folder in foundFolders)
        {
            string localStoragePath = Path.GetDirectoryName(folder.FullName);
            string discordPath = Path.GetDirectoryName(localStoragePath);
            string databasePath = Path.Combine(discordPath, "Local Storage", "leveldb");

            var accounts = GetAccountData(databasePath);

            if (accounts != null && accounts.Count > 0)
            {
                // use the latest one
                if (newestFolder == null || folder.LastWriteTime > newestFolder.LastWriteTime)
                {
                    newestFolder = folder;

                    // create destination directory
                    Directory.CreateDirectory(DiscordRoamingPath);

                    // copy Local Storage folder
                    string sourceLocalStoragePath = Path.Combine(discordPath, "Local Storage");
                    string destLocalStoragePath = Path.Combine(DiscordRoamingPath, "Local Storage");

                    if (Directory.Exists(sourceLocalStoragePath))
                    {
                        Directory.CreateDirectory(destLocalStoragePath);

                        foreach (var directory in Directory.GetDirectories(sourceLocalStoragePath, "*", SearchOption.AllDirectories))
                        {
                            string subDirPath = directory.Replace(sourceLocalStoragePath, destLocalStoragePath);
                            Directory.CreateDirectory(subDirPath);
                        }

                        foreach (var file in Directory.GetFiles(sourceLocalStoragePath, "*.*", SearchOption.AllDirectories))
                        {
                            string destFilePath = file.Replace(sourceLocalStoragePath, destLocalStoragePath);
                            File.Copy(file, destFilePath, true);
                        }
                    }

                    // copy Local State file
                    string sourceLocalStatePath = Path.Combine(discordPath, "Local State");
                    string destLocalStatePath = Path.Combine(DiscordRoamingPath, "Local State");

                    if (File.Exists(sourceLocalStatePath))
                    {
                        File.Copy(sourceLocalStatePath, destLocalStatePath, true);
                    }

                    var accountNames = accounts.Select(a => a.Username).ToList();
                    string accountsString = accountNames.Count switch
                    {
                        1 => accountNames[0],
                        2 => $"{accountNames[0]} and {accountNames[1]}",
                        _ => $"{string.Join(", ", accountNames.Take(accountNames.Count - 1))}, and {accountNames.Last()}"
                    };

                    reporter?.SetTitle($"Successfully logged in as {accountsString}...");

                    await Task.Delay(1000);

                    return;
                }
            }
        }
    }

    public class DiscordAccountInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public bool IsActive { get; set; }
    }

	public static List<DiscordAccountInfo> GetAccountData(string databasePath)
	{
		JsonNode themeNode;
		JsonNode userIdCacheNode;

		try
		{
			themeNode = DatabaseHelper.Read(databasePath, "_https://discord.com", "MultiAccountStore");
			userIdCacheNode = DatabaseHelper.Read(databasePath, "_https://discord.com", "user_id_cache");
		}
		catch (IOException)
		{
			string tempDatabasePath = databasePath + " - Copy";
			Directory.CreateDirectory(tempDatabasePath);

			foreach (var file in Directory.GetFiles(databasePath))
			{
				File.Copy(file, Path.Combine(tempDatabasePath, Path.GetFileName(file)), true);
			}

			themeNode = DatabaseHelper.Read(tempDatabasePath, "_https://discord.com", "MultiAccountStore");
			userIdCacheNode = DatabaseHelper.Read(tempDatabasePath, "_https://discord.com", "user_id_cache");

			Directory.Delete(tempDatabasePath, true);
		}

		string activeUserId = userIdCacheNode?.ToString();

		if (themeNode != null)
		{
			var accounts = new List<DiscordAccountInfo>();
			JsonNode usersNode = themeNode["_state"]?["users"];

			if (usersNode != null && usersNode is JsonArray usersArray)
			{
				foreach (JsonNode userNode in usersArray)
				{
					string id = userNode?["id"]?.ToString();
					string username = userNode?["username"]?.ToString();
					string avatar = userNode?["avatar"]?.ToString();
					bool isActive = id == activeUserId;

					accounts.Add(new DiscordAccountInfo { UserId = id, Username = username, Avatar = avatar, IsActive = isActive });
				}
			}

			return accounts;
		}
		return null;
	}

	public static void SetSystemAppearance(string databasePath)
	{
		JsonNode jsonContent = new JsonObject
		{
			["_state"] = new JsonObject
			{
				["darkSidebar"] = false,
				["hdrDynamicRange"] = "no-limit",
				["useSystemTheme"] = 2
			},
			["_version"] = 2
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "UnsyncedUserSettingsStore", jsonContent);
	}

	public static void DisableGameOverlay(string databasePath)
	{
		JsonNode jsonContent = new JsonObject
		{
			["legacyEnabled"] = false,
			["oopEnabled"] = false
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "OverlayStore6", jsonContent);
	}

	public static void DisableClips(string databasePath)
	{
		JsonNode clipsNode = DatabaseHelper.Read(databasePath, "https://discordapp.com", "ClipsStore");

		if (clipsNode != null)
		{
			clipsNode["_state"]?["clipsSettings"]?["clipsEnabled"] = false;
			DatabaseHelper.Write(databasePath, "https://discord.com", "ClipsStore", clipsNode);
		}
	}
}