using Microsoft.Win32;
using System.Diagnostics;
using AutoOS.Helpers.Registry;

namespace AutoOS.Views.Installer
{
    public sealed partial class HomeLandingPage : Page
    {
        private static readonly HttpClient httpClient = new();
        public HomeLandingPage()
        {
            InitializeComponent();
            Loaded += HomeLandingPage_Loaded;
        }

        private async void HomeLandingPage_Loaded(object sender, RoutedEventArgs e)
        {
            #if !DEBUG
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                if (key.GetValue("InstallDate") is int unixSeconds)
                {
                    var installDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
                    if ((DateTime.Now - installDate).TotalDays > 2)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Fresh Windows Required",
                            Content = "AutoOS currently only on fresh installations of Windows.\nPlease follow the Getting Started guide in the README on GitHub.",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = XamlRoot
                        };
                        await dialog.ShowAsync();
                        Application.Current.Exit();
                    }
                }

                string buildStr = key.GetValue("CurrentBuild")?.ToString() ?? "";
                string ubrStr = key.GetValue("UBR")?.ToString() ?? "";
                if (int.TryParse(buildStr, out int build) && int.TryParse(ubrStr, out int ubr))
                {
                    if (build != 26200 || (build == 26200 && ubr < 7922))
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Unsupported Windows Version",
                            Content = $"AutoOS is currently only supported on new versions of Windows 11 25H2. \nPlease download it from the Getting Started guide in the README on GitHub.",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = XamlRoot
                        };
                        await dialog.ShowAsync();
                        Application.Current.Exit();
                    }
                }
            #endif

            // enable app access to location
            await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo { FileName = @"C:\Windows\system32\SystemSettingsAdminFlows.exe", Arguments = "SetCamSystemGlobal location 1", CreateNoWindow = true });
            RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation", 1, RegistryValueKind.DWord);

            // download pci ids
            string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");

            if (!File.Exists(pciPath))
                await File.WriteAllBytesAsync(pciPath, await httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids"));
        }
    }
}