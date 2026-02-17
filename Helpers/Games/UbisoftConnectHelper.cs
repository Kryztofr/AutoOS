using Microsoft.Win32;
using System.Diagnostics;
using System.Text;

namespace AutoOS.Helpers.Games
{
    public static class UbisoftConnectHelper
    {
        public static readonly string UbisoftConnecDir = @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher";
        public static readonly string UbisoftConnectPath = Path.Combine(UbisoftConnecDir, "upc.exe");
        public static readonly string UbisoftConnectLauncherPath = Path.Combine(UbisoftConnecDir, "UbisoftGameLauncher.exe");
        public static readonly string UbisoftConnectCachePath = Path.Combine(UbisoftConnecDir, "cache", "configuration", "configurations");
        public static readonly string UbisoftConnectApi = @"https://ubistatic3-a.akamaihd.net/orbit/uplay_launcher_3_0/assets/";

        private static readonly HttpClient httpClient = new();

        public static async Task LoadGames()
        {
            // return if either steam or no games are installed
            if (!File.Exists(UbisoftConnectPath)) return;

            // remove previous games
            foreach (var item in GamesPage.Instance.Games.Items.OfType<Views.Settings.Games.HeaderCarouselItem>().Where(item => item.Launcher == "Ubisoft Connect").ToList())
                GamesPage.Instance.Games.Items.Remove(item);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parsedGames = new List<(string Name, string Publisher, string AppId, string ThumbImage, string BackgroundImage)>();
            string currentName = null, publisher = null, thumbImage = null, backgroundImage = null, appId = null;

            foreach (var line in File.ReadAllLines(UbisoftConnectCachePath, Encoding.GetEncoding("iso-8859-15")))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("name:"))
                {
                    if (!string.IsNullOrEmpty(currentName) && !string.IsNullOrEmpty(publisher) && !string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(thumbImage) && !string.IsNullOrEmpty(backgroundImage))
                    {
                        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Ubisoft\Launcher\Installs\{appId}");
                        if (key != null && key.GetValue("InstallState")?.ToString() == "1")
                        {
                            parsedGames.Add((currentName, publisher, appId, thumbImage, backgroundImage));
                        }
                    }

                    currentName = trimmed.Replace("name:", "").Trim();
                    publisher = null;
                    thumbImage = null;
                    backgroundImage = null;
                    appId = null;
                }
                else if (trimmed.StartsWith("publisher:"))
                {
                    publisher = trimmed.Replace("publisher:", "").Trim();
                }
                else if (trimmed.StartsWith("thumb_image:"))
                {
                    thumbImage = trimmed.Replace("thumb_image:", "").Trim();
                }
                else if (trimmed.StartsWith("background_image:"))
                {
                    backgroundImage = trimmed.Replace("background_image:", "").Trim();
                }
                else if (trimmed.StartsWith("register: HKEY_LOCAL_MACHINE\\SOFTWARE\\Ubisoft\\Launcher\\Installs"))
                {
                    appId = trimmed.Replace("register: HKEY_LOCAL_MACHINE\\SOFTWARE\\Ubisoft\\Launcher\\Installs\\", "")
                                   .Replace("\\InstallDir", "").Trim();
                }
            }

            foreach (var game in parsedGames)
            {
                GamesPage.Instance.Games.Items.Add(new Views.Settings.Games.HeaderCarouselItem
                {
                    Launcher = "Ubisoft Connect",
                    Title = game.Name,
                    Developers = game.Publisher,
                    ImageUrl = $"{UbisoftConnectApi}{game.ThumbImage}",
                    BackgroundImageUrl = $"{UbisoftConnectApi}{game.BackgroundImage}",
                    LauncherLocation = UbisoftConnectLauncherPath,
                    GameID = game.AppId,
                    Width = 240,
                    Height = 320
                });
            }
        }

        public static void CloseUbisoftConnect()
        {
            foreach (var name in new[] { "upc", "UplayWebCore" })
            {
                Process.GetProcessesByName(name).ToList().ForEach(p =>
                {
                    p.Kill();
                    p.WaitForExit();
                });
            }
        }
    }
}